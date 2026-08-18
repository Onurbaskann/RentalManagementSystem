using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Common;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class SystemSettingService(
    ISystemSettingRepository systemSettingRepository,
    IReservationPolicyProvider reservationPolicyProvider,
    IOperationalPolicyProvider operationalPolicyProvider,
    IUnitOfWork unitOfWork) : ISystemSettingService
{
    public async Task<PagedResult<SystemSettingListItemDto>> GetPagedAsync(
        TableQuery query,
        CancellationToken cancellationToken = default)
    {
        var settings = await systemSettingRepository.GetActiveListAsync(cancellationToken);
        var orderedItems = settings
            .Select(ToListItem)
            .Where(item => item != null)
            .Select(item => item!)
            .OrderBy(item => item.GroupDisplayName)
            .ThenBy(item => item.DisplayName)
            .ToList();
        var size = query.SafeSize;
        var total = orderedItems.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)size));
        var page = Math.Clamp(query.Page, 1, totalPages);

        return new PagedResult<SystemSettingListItemDto>
        {
            Items = orderedItems.Skip((page - 1) * size).Take(size).ToList(),
            Total = total,
            Page = page,
            Size = size
        };
    }

    public async Task<SystemSettingListItemDto?> GetByIdAsync(
        GetSystemSettingInput input,
        CancellationToken cancellationToken = default)
    {
        var setting = await systemSettingRepository.GetByIdAsync(input.Id);
        return setting == null || !setting.IsActive ? null : ToListItem(setting);
    }

    public async Task UpdateAsync(
        UpdateSystemSettingInput input,
        CancellationToken cancellationToken = default)
    {
        var setting = Guard.NotFound(
            await systemSettingRepository.GetByIdAsync(input.Id),
            "Sistem ayarı bulunamadı.");
        var definition = Guard.NotFound(
            SystemSettingDefinitions.Find(setting.Key),
            "Sistem ayarı tanımı bulunamadı.");

        Guard.Forbidden(
            !definition.IsEditable,
            "Bu sistem ayarı kullanıcı tarafından değiştirilemez.");

        var isValid = SystemSettingDefinitions.TryNormalizeValue(
            definition,
            input.Value,
            out var normalizedValue,
            out var error);
        Guard.InvalidField(!isValid, nameof(input.Value), error ?? "Geçersiz değer.");

        var activeSettings = await systemSettingRepository.GetActiveListAsync(cancellationToken);
        var candidateValues = activeSettings.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.OrdinalIgnoreCase);
        candidateValues[setting.Key] = normalizedValue;

        try
        {
            _ = SystemSettingDefinitions.CreateReservationPolicy(candidateValues);
            _ = SystemSettingDefinitions.CreateOperationalPolicy(candidateValues);
        }
        catch (InvalidOperationException exception)
        {
            throw new BusinessValidationException(
                nameof(input.Value),
                exception.Message,
                "SYSTEM_SETTING_INVALID_VALUE");
        }

        setting.Value = normalizedValue;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await reservationPolicyProvider.RefreshAsync(cancellationToken);
        await operationalPolicyProvider.RefreshAsync(cancellationToken);
    }

    private static SystemSettingListItemDto? ToListItem(SystemSetting setting)
    {
        var definition = SystemSettingDefinitions.Find(setting.Key);
        return definition == null
            ? null
            : new SystemSettingListItemDto(
                setting.Id,
                setting.Key,
                definition.DisplayName,
                definition.GroupDisplayName,
                definition.Description,
                setting.Value,
                definition.InputKind,
                definition.IsEditable,
                definition.MinimumValue,
                definition.MaximumValue);
    }
}
