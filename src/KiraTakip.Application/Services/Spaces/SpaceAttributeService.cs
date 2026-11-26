using System.Globalization;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos.Space;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Spaces;
using KiraTakip.Services.Interfaces.Spaces;

namespace KiraTakip.Services.Spaces;

public class SpaceAttributeService(
    ISpaceAttributeRepository attributeRepository,
    ISpaceRepository spaceRepository,
    IUnitOfWork uow) : ISpaceAttributeService
{
    private const char OptionSeparator = '\n';
    private const int ValueMaxLength = 1000;
    private const string CodeField = "Code";
    private const string NameField = "Name";
    private const string OptionsField = "Options";
    private const string ValuesField = "Values";

    public async Task<List<SpaceAttributeDto>> GetListAsync()
        => (await attributeRepository.GetAllOrderedAsync()).Select(ToDto).ToList();

    public async Task<List<SpaceAttributeDto>> GetTypeSuggestionsAsync(int spaceTypeId)
        => (await attributeRepository.GetTypeDefaultsAsync(spaceTypeId)).Select(ToDto).ToList();

    public async Task<int> CreateAsync(SpaceAttributeInput input)
    {
        var (code, name, options) = await ValidateAsync(input, null);
        var entity = new SpaceAttributeDefinition { Code = code, Name = name, DataType = input.DataType, Options = options };
        await attributeRepository.AddAsync(entity);
        await uow.SaveChangesAsync();
        return entity.Id;
    }

    public async Task UpdateAsync(int id, SpaceAttributeInput input)
    {
        var entity = Guard.NotFound(await attributeRepository.GetByIdAsync(id), "Özellik bulunamadı.");
        var (code, name, options) = await ValidateAsync(input, id);
        Guard.Conflict(
            entity.DataType != input.DataType && await attributeRepository.IsUsedAsync(id),
            "Kullanımdaki özelliğin veri tipi değiştirilemez.",
            "SpaceAttribute.DataTypeLocked");
        entity.Code = code;
        entity.Name = name;
        entity.DataType = input.DataType;
        entity.Options = options;
        await uow.SaveChangesAsync();
    }

    public async Task<bool> ToggleStatusAsync(int id)
    {
        var entity = Guard.NotFound(await attributeRepository.GetByIdAsync(id), "Özellik bulunamadı.");
        entity.IsActive = !entity.IsActive;
        await uow.SaveChangesAsync();
        return entity.IsActive;
    }

    public async Task DeleteAsync(int id)
    {
        var entity = Guard.NotFound(await attributeRepository.GetByIdAsync(id), "Özellik bulunamadı.");
        Guard.Conflict(
            await attributeRepository.IsUsedAsync(id),
            "Kullanımdaki özellik silinemez; pasifleştirin.",
            "SpaceAttribute.InUse");
        entity.IsDeleted = true;
        entity.IsActive = false;
        await uow.SaveChangesAsync();
    }

    public async Task<List<SpaceAttributeValueDto>> GetValuesAsync(int spaceId)
        => (await attributeRepository.GetValuesForUpdateAsync(spaceId))
            .Select(v => new SpaceAttributeValueDto(
                v.AttributeDefinitionId, v.AttributeDefinition.Name, v.AttributeDefinition.DataType, v.Value))
            .OrderBy(v => v.Name)
            .ToList();

    public Task SaveValuesAsync(int spaceId, IReadOnlyList<SpaceAttributeValueInput> values)
        => uow.ExecuteInTransactionAsync(async _ =>
        {
            Guard.NotFound(await spaceRepository.GetNoTrackingAsync(spaceId), "Alan bulunamadı.");
            Guard.InvalidField(
                values.Select(v => v.AttributeDefinitionId).Distinct().Count() != values.Count,
                ValuesField,
                "Aynı özellik birden fazla girilemez.");

            var definitions = (await attributeRepository.GetByIdsAsync(values.Select(v => v.AttributeDefinitionId).ToList()))
                .ToDictionary(d => d.Id);
            var existing = (await attributeRepository.GetValuesForUpdateAsync(spaceId))
                .ToDictionary(v => v.AttributeDefinitionId);
            var kept = new HashSet<int>();

            foreach (var item in values)
            {
                var definition = Guard.NotFound(
                    definitions.GetValueOrDefault(item.AttributeDefinitionId), "Özellik bulunamadı.");
                var raw = item.Value?.Trim();
                if (string.IsNullOrEmpty(raw)) continue;

                var value = Normalize(definition, raw);
                kept.Add(definition.Id);
                if (existing.TryGetValue(definition.Id, out var current))
                    current.Value = value;
                else
                {
                    Guard.InvalidField(!definition.IsActive, ValuesField, $"\"{definition.Name}\" özelliği pasif.");
                    attributeRepository.AddValue(new SpaceAttributeValue
                    {
                        SpaceId = spaceId,
                        AttributeDefinitionId = definition.Id,
                        Value = value
                    });
                }
            }

            foreach (var stale in existing.Values.Where(v => !kept.Contains(v.AttributeDefinitionId)))
            {
                stale.IsDeleted = true;
                stale.IsActive = false;
            }
            await uow.SaveChangesAsync();
        });

    private static string Normalize(SpaceAttributeDefinition definition, string raw)
    {
        Guard.InvalidField(raw.Length > ValueMaxLength, ValuesField, $"\"{definition.Name}\" değeri çok uzun.");
        switch (definition.DataType)
        {
            case AttributeDataType.Number:
                Guard.InvalidField(
                    !decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var number),
                    ValuesField,
                    $"\"{definition.Name}\" için geçerli bir sayı girin.");
                return decimal.Parse(raw, NumberStyles.Number, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture);
            case AttributeDataType.Boolean:
                Guard.InvalidField(
                    !bool.TryParse(raw, out var flag),
                    ValuesField,
                    $"\"{definition.Name}\" için Evet/Hayır değeri geçersiz.");
                return bool.Parse(raw) ? bool.TrueString : bool.FalseString;
            case AttributeDataType.List:
                Guard.InvalidField(
                    !ParseOptions(definition.Options).Contains(raw),
                    ValuesField,
                    $"\"{definition.Name}\" için seçenek listesinde olmayan bir değer girildi.");
                return raw;
            default:
                return raw;
        }
    }

    private async Task<(string Code, string Name, string? Options)> ValidateAsync(SpaceAttributeInput input, int? excludeId)
    {
        var code = input.Code?.Trim() ?? string.Empty;
        var name = input.Name?.Trim() ?? string.Empty;
        Guard.InvalidField(code.Length is 0 or > 50, CodeField, "Kod zorunludur ve en fazla 50 karakter olabilir.");
        Guard.InvalidField(name.Length is 0 or > 150, NameField, "Ad zorunludur ve en fazla 150 karakter olabilir.");
        Guard.InvalidField(!Enum.IsDefined(input.DataType), nameof(input.DataType), "Veri tipi geçersiz.");
        Guard.InvalidField(
            await attributeRepository.CodeExistsAsync(code, excludeId),
            CodeField,
            "Bu kod zaten kullanılıyor.",
            "SpaceAttribute.CodeExists");

        string? options = null;
        if (input.DataType == AttributeDataType.List)
        {
            var list = ParseOptions(input.Options);
            Guard.InvalidField(list.Count == 0, OptionsField, "Liste tipi için en az bir seçenek girilmelidir.");
            options = string.Join(OptionSeparator, list);
        }
        return (code, name, options);
    }

    private static List<string> ParseOptions(string? options)
        => (options ?? string.Empty)
            .Split(OptionSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

    private static SpaceAttributeDto ToDto(SpaceAttributeDefinition d)
        => new(d.Id, d.Code, d.Name, d.DataType, ParseOptions(d.Options), d.IsActive);
}
