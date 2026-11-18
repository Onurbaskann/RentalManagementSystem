using System.Globalization;
using KiraTakip.Data;
using KiraTakip.Domain.Leases;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Settings;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Repositories.Interfaces.Settings;
using KiraTakip.Services.Interfaces.Payments;
using KiraTakip.Services.Interfaces.Reservations;
using KiraTakip.Services.Interfaces.Settings;

namespace KiraTakip.Services.Settings;

public class SystemSettingService(
    ISystemSettingRepository systemSettingRepository,
    IReservationPolicyProvider reservationPolicyProvider,
    IOperationalPolicyProvider operationalPolicyProvider,
    IUnitRepository unitRepository,
    TimeProvider timeProvider,
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

    public async Task<SystemSettingUpdateResult> UpdateAsync(
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

        var previousValue = setting.Value;
        var warnings = await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var result = new List<string>();
            if (setting.Key == SystemSettingDefinitions.Lease.MinimumUsageArea
                && TryParseDecimal(previousValue, out var oldMinimum)
                && TryParseDecimal(normalizedValue, out var newMinimum)
                && newMinimum > oldMinimum)
            {
                result.AddRange(await CheckMinimumUsageAreaIncreaseAsync(newMinimum));
            }

            setting.Value = normalizedValue;
            await unitOfWork.SaveChangesAsync(ct);
            return result;
        }, cancellationToken);

        await reservationPolicyProvider.RefreshAsync(cancellationToken);
        await operationalPolicyProvider.RefreshAsync(cancellationToken);
        return new SystemSettingUpdateResult(warnings);
    }

    private async Task<List<string>> CheckMinimumUsageAreaIncreaseAsync(decimal minimum)
    {
        await unitRepository.AcquireGlobalCapacityLockExclusiveAsync();
        var today = timeProvider.GetLocalNow().Date;
        var units = await unitRepository.GetCapacityUnitsWithOpenLeasesAsync(today);

        var unitConflicts = new List<string>();
        var leaseConflicts = new List<string>();
        var warnings = new List<string>();

        foreach (var unit in units)
        {
            var unitLabel = $"{unit.PropertyName} / {unit.UnitName}";
            if (unit.Capacity is { } capacity
                && capacity > UnitCapacityPolicy.GetCapacityLimit(unit.Area, minimum))
            {
                unitConflicts.Add(
                    $"- {unitLabel}: kontenjan {capacity}, en fazla {UnitCapacityPolicy.GetCapacityLimit(unit.Area, minimum)}");
            }

            foreach (var lease in unit.Leases)
            {
                if (lease.RentedArea >= minimum && lease.AllocatedArea >= minimum)
                    continue;

                var line = $"{lease.TenantName} ({unitLabel}): kiralanan {FormatArea(lease.RentedArea)} m², fiziksel {FormatArea(lease.AllocatedArea)} m²";
                if (lease.Status == LeaseStatus.Active)
                    leaseConflicts.Add($"- {line}");
                else
                    warnings.Add(line);
            }
        }

        var conflicts = unitConflicts.Concat(leaseConflicts).ToList();
        if (conflicts.Count > 0)
        {
            throw new BusinessValidationException(
                nameof(UpdateSystemSettingInput.Value),
                $"Min. kullanım alanı {FormatArea(minimum)} m²'ye çıkarılamaz. Çakışanlar:\n{string.Join('\n', conflicts)}",
                SystemSettingCodes.MinimumUsageAreaConflict);
        }

        return warnings
            .ToList();
    }

    private static bool TryParseDecimal(string value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

    private static string FormatArea(decimal value)
        => value.ToString("0.##", CultureInfo.GetCultureInfo("tr-TR"));

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
