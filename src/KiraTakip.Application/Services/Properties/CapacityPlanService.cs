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

namespace KiraTakip.Services.Properties;

public class CapacityPlanService(
    IUnitRepository unitRepository,
    ILeaseRepository leaseRepository,
    IUnitOfWork unitOfWork,
    IOperationalPolicyProvider operationalPolicyProvider,
    IAllocationSuggestionStrategy suggestionStrategy,
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
            "Fiziksel m² yalnızca açık (aktif, taslak veya revizyon istenen) sözleşmelerde düzenlenebilir.",
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
            $"Fiziksel m² {minimum:0.##} ile {rented:0.##} m² arasında olmalıdır.",
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
                    $"Fiziksel alanlar toplamı birim alanını aşıyor: {peak - unit.Area:0.##} m² taşma (toplam {peak:0.##} m², birim {unit.Area:0.##} m²).",
                    CapacityPlanCodes.AreaOverflow);
            }

            if (hard.Contains(CapacityHardViolation.CapacityExceeded))
                throw new BusinessValidationException(
                    CapacityPlanCodes.AllocatedAreaField,
                    "Açık sözleşme sayısı birim kontenjanını aşıyor; fiziksel alan değişikliği bunu çözmez.",
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

        var suggestions = suggestionStrategy
            .Suggest(
                norm,
                context.Leases
                    .Select(lease => new AllocationSuggestionRow(
                        lease.LeaseId, lease.RentedArea, lease.AllocatedArea, lease.Mode))
                    .ToList(),
                capacityValid ? overrides : new Dictionary<int, decimal>())
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
            errors.Add(new(CapacityPlanCodes.Invalid, "Kontenjan 0'dan büyük olmalıdır."));
        }
        else
        {
            if (newCapacity > maximum)
                errors.Add(new(
                    CapacityPlanCodes.AboveMaximum,
                    $"Kontenjan en fazla {maximum} olabilir (alan {unit.Area:0.##} m², kişi başı en az {minimum:0.##} m²)."));

            var hard = UnitCapacityPolicy.GetHardViolations(newCapacity, unit.Area, occupants, today, rangeEnd);
            if (hard.Contains(CapacityHardViolation.CapacityExceeded))
                errors.Add(new(
                    CapacityPlanCodes.CountExceeded,
                    $"Açık sözleşme sayısı en yüksek {peakCount} olduğu için kontenjan {newCapacity} olamaz. Fiziksel alanı azaltmak bunu çözmez; sözleşme sayısı kontenjanın altına inmelidir."));
            if (hard.Contains(CapacityHardViolation.AreaExceeded))
                errors.Add(new(
                    CapacityPlanCodes.AreaOverflow,
                    $"Fiziksel alanlar toplamı birim alanını aşıyor: {peak.Area - unit.Area:0.##} m² taşma (toplam {peak.Area:0.##} m², birim {unit.Area:0.##} m²). Sözleşme alanlarını düzeltin."));

            var invalidRows = rows.Count(row => row.Error != null);
            if (invalidRows > 0)
                errors.Add(new(
                    CapacityPlanCodes.AreaOutOfRange,
                    $"{invalidRows} sözleşmenin fiziksel alanı izin verilen aralığın dışında."));

            var snapshot = UnitCapacityPolicy.GetSnapshot(newCapacity, unit.Area, minimum, occupants, today);
            if (snapshot.NormTargetShort)
                warnings.Add(new(
                    CapacityPlanCodes.NormTargetShort,
                    $"Kalan alan ({snapshot.RemainingArea:0.##} m²), {snapshot.FreeSlots} boş kontenjan için norm hedefinin ({snapshot.NormTarget:0.##} m²) altında.",
                    snapshot.FreeSlots, snapshot.RemainingArea, snapshot.NormTarget, snapshot.MinimumTarget));
            if (snapshot.MinimumTargetShort)
                warnings.Add(new(
                    CapacityPlanCodes.MinimumTargetShort,
                    $"Kalan alan ({snapshot.RemainingArea:0.##} m²), {snapshot.FreeSlots} boş kontenjan için asgari hedefin ({snapshot.MinimumTarget:0.##} m²) altında.",
                    snapshot.FreeSlots, snapshot.RemainingArea, snapshot.NormTarget, snapshot.MinimumTarget));
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
            ComputeHash(unit.Capacity, newCapacity, unit.Area, minimum, context.Leases, overrides));
    }

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
