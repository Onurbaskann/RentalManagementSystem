using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using KiraTakip.Data;
using KiraTakip.Domain.Leases;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Property;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Interfaces.Leases;
using KiraTakip.Repositories.Interfaces.Properties;
using KiraTakip.Services.Interfaces.Properties;
using KiraTakip.Services.Leases;
using KiraTakip.Services.Interfaces.Payments;
using Microsoft.Extensions.DependencyInjection;

namespace KiraTakip.Services.Properties;

public class CapacityPlanService(
    IUnitRepository unitRepository,
    ILeaseRepository leaseRepository,
    IUnitOfWork unitOfWork,
    IOperationalPolicyProvider operationalPolicyProvider,
    IAllocationSuggestionStrategy suggestionStrategy,
    [FromKeyedServices(AllocationStrategyKeys.ProportionalReduction)] IAllocationSuggestionStrategy optimizationStrategy,
    TimeProvider timeProvider) : ICapacityPlanService, ITransactionalService
{
    public async Task<CapacityPlanPreviewDto> PreviewAsync(CapacityPlanInput input)
    {
        var context = await LoadContextAsync(input);
        return BuildPreview(input, context);
    }

    public async Task ApplyAsync(CapacityPlanInput input, string expectedHash)
    {
        await LoadUnitAsync(input);
        await unitRepository.AcquireCapacityLocksAsync(input.UnitId);

        var context = await LoadContextAsync(input);
        var preview = BuildPreview(input, context);

        Guard.Conflict(
            !string.Equals(preview.Hash, expectedHash, StringComparison.Ordinal),
            "Önizlemeden sonra veriler değişti; önizlemeyi yenileyin.",
            CapacityPlanCodes.PreviewOutdated);

        if (preview.HasErrors)
            throw new BusinessValidationException(
                CapacityPlanCodes.CapacityField,
                string.Join(" ", preview.Errors.Select(error => error.Message)),
                preview.Errors[0].Code);

        var today = Today();
        var unit = Guard.NotFound(
            await unitRepository.GetByIdAsync(input.UnitId),
            "Birim bulunamadı.",
            CapacityPlanCodes.UnitNotFound);
        if (unit.Capacity != input.NewCapacity)
        {
            unit.Capacity = input.NewCapacity;
            unitRepository.AddCapacityHistory(unit, input.NewCapacity, today);
        }

        var changedRows = preview.Rows
            .Where(row => row.ProposedMode.HasValue
                && (row.ProposedAllocatedArea != row.CurrentAllocatedArea || row.ProposedMode != row.CurrentMode))
            .ToList();
        if (changedRows.Count > 0)
        {
            var areas = (await leaseRepository.GetTrackedLeaseAreasAsync(changedRows.Select(row => row.LeaseId).ToList()))
                .ToDictionary(area => area.LeaseId);
            foreach (var row in changedRows)
            {
                if (!areas.TryGetValue(row.LeaseId, out var area)) continue;
                area.AllocatedArea = row.ProposedAllocatedArea;
                area.AllocationMode = row.ProposedMode!.Value;
                if (row.Status == LeaseStatus.Active)
                    leaseRepository.AddLeaseAreaHistory(
                        area, row.ProposedMode.Value, today, LeaseAreaChangeReason.CapacityChange);
            }
        }

        await unitOfWork.SaveChangesAsync();
    }

    public async Task<LeaseAllocatedAreaInfoDto?> GetAllocatedAreaInfoAsync(int leaseId, LeaseAccessScopeInput scope)
    {
        var head = await LoadLeaseHeadAsync(leaseId);
        var unit = await unitRepository.GetLeaseContextAsync(head.UnitId);
        if (unit == null || unit.RentalMode != UnitRentalMode.CapacityBased) return null;
        await LoadUnitAsync(head.UnitId, scope);

        var area = await leaseRepository.GetLeaseAreaAsync(leaseId);
        if (area == null) return null;

        return new LeaseAllocatedAreaInfoDto(
            area.RentedArea,
            area.AllocatedArea,
            area.AllocationMode,
            operationalPolicyProvider.Current.LeaseMinimumUsageArea,
            CapacityOccupantStatuses.Contains(head.Status),
            await leaseRepository.GetLeaseAreaHistoryItemsAsync(leaseId));
    }

    public async Task UpdateAllocatedAreaAsync(UpdateAllocatedAreaInput input)
    {
        var head = await LoadLeaseHeadAsync(input.LeaseId);
        await LoadUnitAsync(head.UnitId, input.AccessScope);
        await unitRepository.AcquireCapacityLocksAsync(head.UnitId);

        head = await LoadLeaseHeadAsync(input.LeaseId);
        var unit = await LoadUnitAsync(head.UnitId, input.AccessScope);
        Guard.InvalidField(
            !CapacityOccupantStatuses.Contains(head.Status),
            CapacityPlanCodes.AllocatedAreaField,
            "Fiziksel alan yalnızca açık (aktif, taslak veya revizyon istenen) sözleşmelerde düzenlenebilir.",
            CapacityPlanCodes.AllocatedAreaNotEditable);

        var area = (await leaseRepository.GetTrackedLeaseAreasAsync([input.LeaseId])).FirstOrDefault();
        if (area == null)
            throw new BusinessValidationException(
                CapacityPlanCodes.AllocatedAreaField,
                "Sözleşmenin kiralanan alan kaydı bulunamadı.",
                CapacityPlanCodes.AllocatedAreaMissing);
        var newArea = Math.Round(input.AllocatedArea, 2, MidpointRounding.AwayFromZero);
        if (area.AllocatedArea == newArea) return;

        var minimum = operationalPolicyProvider.Current.LeaseMinimumUsageArea;
        var rented = area.RentedArea;
        Guard.InvalidField(
            UnitCapacityPolicy.ValidateArea(rented, newArea, minimum).Count > 0,
            CapacityPlanCodes.AllocatedAreaField,
            $"Fiziksel alan {minimum:0.##} m² ile {rented:0.##} m² arasında olmalıdır.",
            CapacityPlanCodes.AllocatedAreaOutOfRange);

        var today = Today();
        var rangeStart = head.StartDate.Date > today ? head.StartDate.Date : today;
        var rangeEnd = head.EndDate.Date;
        if (rangeStart <= rangeEnd)
        {
            var occupants = (await leaseRepository.GetOpenLeaseAreasForUnitAsync(
                    head.UnitId, rangeStart, rangeEnd, input.LeaseId))
                .Append(new CapacityOccupant(
                    head.StartDate, head.EndDate, newArea, input.LeaseId, rented, LeaseAreaMode.Manual))
                .ToList();
            var hard = UnitCapacityPolicy.GetHardViolations(
                unit.Capacity ?? 0, unit.Area, occupants, rangeStart, rangeEnd);
            if (hard.Contains(CapacityHardViolation.AreaExceeded))
            {
                var days = occupants.Select(item => item.StartDate.Date)
                    .Where(day => day >= rangeStart && day <= rangeEnd)
                    .Append(rangeStart)
                    .Distinct();
                var peak = days.Max(day => occupants
                    .Where(item => item.StartDate.Date <= day && day <= item.EndDate.Date)
                    .Sum(item => item.AllocatedArea));
                throw new BusinessValidationException(
                    CapacityPlanCodes.AllocatedAreaField,
                    $"Toplam fiziksel alan birim toplam alanını {peak - unit.Area:0.##} m² aşıyor. Sözleşmelerin fiziksel alanlarını küçültün.",
                    CapacityPlanCodes.AreaOverflow);
            }

            if (hard.Contains(CapacityHardViolation.CapacityExceeded))
                throw new BusinessValidationException(
                    CapacityPlanCodes.AllocatedAreaField,
                    "Açık sözleşme sayısı kontenjanı aşıyor. Fiziksel alanı değiştirmek bunu çözmez.",
                    CapacityPlanCodes.CountExceeded);
        }

        area.AllocatedArea = newArea;
        area.AllocationMode = LeaseAreaMode.Manual;
        if (head.Status == LeaseStatus.Active)
            leaseRepository.AddLeaseAreaHistory(
                area, LeaseAreaMode.Manual, rangeStart, LeaseAreaChangeReason.ManualEdit);

        await unitOfWork.SaveChangesAsync();
    }

    private static readonly LeaseStatus[] CapacityOccupantStatuses =
        [LeaseStatus.Active, LeaseStatus.Draft, LeaseStatus.RevisionRequested];

    private sealed record LeaseHead(int UnitId, DateTime StartDate, DateTime EndDate, LeaseStatus Status);

    private async Task<LeaseHead> LoadLeaseHeadAsync(int leaseId)
        => Guard.NotFound(
            await leaseRepository.GetByIdAsync(
                leaseId,
                lease => new LeaseHead(lease.UnitId, lease.StartDate, lease.EndDate, lease.Status)),
            "Sözleşme bulunamadı.",
            CapacityPlanCodes.LeaseNotFound);

    private sealed record PlanContext(
        LeaseUnitContextDto Unit,
        List<CapacityPlanLeaseDto> Leases,
        decimal MinimumUsageArea);

    private Task<LeaseUnitContextDto> LoadUnitAsync(CapacityPlanInput input)
        => LoadUnitAsync(input.UnitId, input.AccessScope);

    private async Task<LeaseUnitContextDto> LoadUnitAsync(int unitId, LeaseAccessScopeInput scope)
    {
        var unit = Guard.NotFound(
            await unitRepository.GetLeaseContextAsync(unitId),
            "Birim bulunamadı.",
            CapacityPlanCodes.UnitNotFound);
        if (LeaseApplicationRules.HasScopeRestriction(scope))
            Guard.Forbidden(
                scope.PropertyIds?.Contains(unit.PropertyId) != true && scope.UnitIds?.Contains(unit.UnitId) != true,
                "Bu birim üzerinde işlem yapma yetkiniz bulunmuyor.",
                CapacityPlanCodes.OutOfScope);
        Guard.Against(
            unit.RentalMode != UnitRentalMode.CapacityBased,
            "Birim kontenjan bazlı değil.",
            CapacityPlanCodes.UnitNotCapacityBased);
        return unit;
    }

    private async Task<PlanContext> LoadContextAsync(CapacityPlanInput input)
    {
        var unit = await LoadUnitAsync(input);
        var leases = await leaseRepository.GetOpenLeasesForCapacityPlanAsync(input.UnitId, Today());
        return new PlanContext(unit, leases, operationalPolicyProvider.Current.LeaseMinimumUsageArea);
    }

    private CapacityPlanPreviewDto BuildPreview(CapacityPlanInput input, PlanContext context)
    {
        var today = Today();
        var unit = context.Unit;
        var minimum = context.MinimumUsageArea;
        var newCapacity = input.NewCapacity;
        var overrides = input.Overrides ?? new Dictionary<int, decimal>();
        var errors = new List<CapacityPlanErrorDto>();
        var warnings = new List<CapacityPlanWarningDto>();
        var maximum = UnitCapacityPolicy.GetCapacityLimit(unit.Area, minimum);
        var capacityValid = newCapacity > 0;
        var norm = capacityValid ? UnitCapacityPolicy.CalculateEffectiveNorm(unit.Area, newCapacity) : 0m;

        var suggestionContext = new AllocationSuggestionContext(
            norm,
            unit.Area,
            newCapacity,
            minimum,
            today,
            context.Leases
                .Select(lease => new AllocationSuggestionRow(
                    lease.LeaseId, lease.RentedArea, lease.AllocatedArea, lease.Mode, lease.StartDate, lease.EndDate))
                .ToList(),
            capacityValid ? overrides : new Dictionary<int, decimal>());
        var suggestions = suggestionStrategy
            .Suggest(suggestionContext)
            .Suggestions
            .ToDictionary(item => item.LeaseId);

        var rows = context.Leases.Select(lease =>
        {
            var suggestion = suggestions[lease.LeaseId];
            string? rowError = null;
            if (lease.RentedArea.HasValue)
                rowError = DescribeAreaViolations(
                    UnitCapacityPolicy.ValidateArea(lease.RentedArea.Value, suggestion.AllocatedArea, minimum),
                    lease.RentedArea.Value,
                    minimum);
            return new CapacityPlanRowDto(
                lease.LeaseId,
                lease.LeaseNo,
                lease.TenantName,
                lease.StartDate,
                lease.EndDate,
                lease.Status,
                lease.RentedArea,
                lease.Mode,
                lease.AllocatedArea,
                suggestion.AllocatedArea,
                suggestion.Mode,
                lease.RentedArea.HasValue,
                rowError);
        }).ToList();

        var occupants = rows
            .Select(row => new CapacityOccupant(
                row.StartDate, row.EndDate, row.ProposedAllocatedArea, row.LeaseId, row.RentedArea, row.ProposedMode))
            .ToList();
        var rangeEnd = occupants.Count == 0 ? today : occupants.Max(item => item.EndDate.Date);
        if (rangeEnd < today) rangeEnd = today;

        var days = new HashSet<DateTime> { today };
        foreach (var occupant in occupants)
        {
            var start = occupant.StartDate.Date;
            if (start >= today && start <= rangeEnd) days.Add(start);
        }

        var measures = days.OrderBy(day => day)
            .Select(day =>
            {
                var active = occupants.Where(item => item.StartDate.Date <= day && day <= item.EndDate.Date).ToList();
                return (Day: day, Count: active.Count, Area: active.Sum(item => item.AllocatedArea));
            })
            .ToList();
        var todayMeasure = measures.First(item => item.Day == today);
        var peak = measures.OrderByDescending(item => item.Area).First();
        var peakCount = measures.Max(item => item.Count);

        if (!capacityValid)
        {
            errors.Add(new(CapacityPlanCodes.Invalid, "Geçerli bir kontenjan girin."));
        }
        else
        {
            if (newCapacity > maximum)
                errors.Add(new(
                    CapacityPlanCodes.AboveMaximum,
                    $"Kontenjan en fazla {maximum} olabilir. Birim toplam alanı {unit.Area:0.##} m² ve kişi başı asgari fiziksel alan {minimum:0.##} m²."));

            var hard = UnitCapacityPolicy.GetHardViolations(newCapacity, unit.Area, occupants, today, rangeEnd);
            if (hard.Contains(CapacityHardViolation.CapacityExceeded))
                errors.Add(new(
                    CapacityPlanCodes.CountExceeded,
                    $"Açık sözleşme sayısı {peakCount}; kontenjan bundan az olamaz. Fiziksel alanları küçültmek bunu çözmez."));
            if (hard.Contains(CapacityHardViolation.AreaExceeded))
                errors.Add(new(
                    CapacityPlanCodes.AreaOverflow,
                    $"Toplam fiziksel alan birim toplam alanını {peak.Area - unit.Area:0.##} m² aşıyor. Sözleşmelerin fiziksel alanlarını küçültün.",
                    DayOrNull(peak.Day, today)));

            var invalidRows = rows.Count(row => row.Error != null);
            if (invalidRows > 0)
                errors.Add(new(
                    CapacityPlanCodes.AreaOutOfRange,
                    $"{invalidRows} sözleşmenin fiziksel alanı izin verilen aralığın dışında."));

            var snapshots = measures
                .Select(measure => (measure.Day, Snapshot: UnitCapacityPolicy.GetSnapshot(
                    newCapacity, unit.Area, minimum, occupants, measure.Day)))
                .ToList();

            var normWorst = snapshots
                .Where(item => item.Snapshot.NormTargetShort)
                .OrderByDescending(item => item.Snapshot.NormTarget - item.Snapshot.RemainingArea)
                .ThenBy(item => item.Day)
                .Cast<(DateTime Day, CapacitySnapshot Snapshot)?>()
                .FirstOrDefault();
            if (normWorst is { } normItem)
                warnings.Add(new(
                    CapacityPlanCodes.NormTargetShort,
                    $"Boş kontenjanlar için {normItem.Snapshot.NormTarget - normItem.Snapshot.RemainingArea:0.##} m² yer eksik.",
                    normItem.Snapshot.FreeSlots, normItem.Snapshot.RemainingArea, normItem.Snapshot.NormTarget, normItem.Snapshot.MinimumTarget,
                    DayOrNull(normItem.Day, today)));

            var minimumWorst = snapshots
                .Where(item => item.Snapshot.MinimumTargetShort)
                .OrderByDescending(item => item.Snapshot.MinimumTarget - item.Snapshot.RemainingArea)
                .ThenBy(item => item.Day)
                .Cast<(DateTime Day, CapacitySnapshot Snapshot)?>()
                .FirstOrDefault();
            if (minimumWorst is { } minimumItem)
                warnings.Add(new(
                    CapacityPlanCodes.MinimumTargetShort,
                    $"Boş kontenjanların her birine en az {minimum:0.##} m² ayrılamıyor; {minimumItem.Snapshot.MinimumTarget - minimumItem.Snapshot.RemainingArea:0.##} m² yer eksik.",
                    minimumItem.Snapshot.FreeSlots, minimumItem.Snapshot.RemainingArea, minimumItem.Snapshot.NormTarget, minimumItem.Snapshot.MinimumTarget,
                    DayOrNull(minimumItem.Day, today)));
        }

        CapacityOptimizationDto? optimization = null;
        if (input.Optimize
            && capacityValid
            && !errors.Any(error => error.Code == CapacityPlanCodes.CountExceeded))
        {
            var optimized = optimizationStrategy.Suggest(suggestionContext);
            var changed = optimized.Suggestions
                .Where(item => item.AllocatedArea != suggestions[item.LeaseId].AllocatedArea)
                .Select(item => new CapacityOptimizationSuggestionDto(item.LeaseId, item.AllocatedArea))
                .ToList();
            var shortfalls = optimized.Shortfalls
                .Select(item => new CapacityOptimizationShortfallDto(item.Day, item.MissingArea))
                .ToList();
            optimization = new CapacityOptimizationDto(changed.Count > 0 || shortfalls.Count > 0, changed, shortfalls);
        }

        return new CapacityPlanPreviewDto(
            unit.UnitId,
            unit.Capacity ?? 0,
            newCapacity,
            unit.Area,
            minimum,
            norm,
            maximum,
            todayMeasure.Area,
            peak.Area,
            peak.Day,
            rows,
            errors,
            warnings,
            ComputeHash(unit.Capacity, newCapacity, unit.Area, minimum, context.Leases, overrides),
            optimization);
    }

    private static DateTime? DayOrNull(DateTime day, DateTime today)
        => day.Date == today.Date ? null : day.Date;

    private static string? DescribeAreaViolations(
        IReadOnlyList<LeaseAreaViolation> violations,
        decimal rented,
        decimal minimum)
    {
        if (violations.Count == 0) return null;
        var messages = violations.Select(violation => violation switch
        {
            LeaseAreaViolation.RentedBelowMinimum => $"Kiralanan alan en az {minimum:0.##} m² olmalıdır.",
            LeaseAreaViolation.AllocatedBelowMinimum => $"Fiziksel alan en az {minimum:0.##} m² olmalıdır.",
            _ => $"Fiziksel alan kiralanan alanı ({rented:0.##} m²) aşamaz."
        });
        return string.Join(" ", messages);
    }

    private static string ComputeHash(
        int? currentCapacity,
        int newCapacity,
        decimal unitArea,
        decimal minimum,
        IReadOnlyList<CapacityPlanLeaseDto> leases,
        IReadOnlyDictionary<int, decimal> overrides)
    {
        var builder = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        builder.Append(currentCapacity?.ToString(inv) ?? "-").Append('|')
            .Append(newCapacity.ToString(inv)).Append('|')
            .Append(unitArea.ToString(inv)).Append('|')
            .Append(minimum.ToString(inv)).Append('\n');
        foreach (var lease in leases.OrderBy(item => item.LeaseId))
            builder.Append(lease.LeaseId.ToString(inv)).Append('|')
                .Append(lease.StartDate.Ticks.ToString(inv)).Append('|')
                .Append(lease.EndDate.Ticks.ToString(inv)).Append('|')
                .Append(((int)lease.Status).ToString(inv)).Append('|')
                .Append(lease.RentedArea?.ToString(inv) ?? "-").Append('|')
                .Append(lease.AllocatedArea.ToString(inv)).Append('|')
                .Append(lease.Mode.HasValue ? ((int)lease.Mode.Value).ToString(inv) : "-").Append('\n');
        builder.Append('#');
        foreach (var item in overrides.OrderBy(pair => pair.Key))
            builder.Append(item.Key.ToString(inv)).Append('=').Append(item.Value.ToString(inv)).Append(';');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()))).ToLowerInvariant();
    }

    private DateTime Today() => timeProvider.GetLocalNow().DateTime.Date;
}
