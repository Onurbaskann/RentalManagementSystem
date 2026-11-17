using KiraTakip.Domain.Leases;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;

namespace KiraTakip.Services.Interfaces.Leases;

public sealed record ValidatedLeaseApplication(
    LeaseUnitContextDto Unit,
    (decimal Rented, decimal Allocated)? Area);

/// <summary>
/// Tekil sözleşme ve paylaşımlı grup akışlarının ortak başvuru/onay kuralları.
/// </summary>
public interface ILeaseApplicationRules
{
    Task<ValidatedLeaseApplication> ValidateApplicationDataAsync(
        int unitId,
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        int dueDay,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        LeaseAccessScopeInput accessScope,
        bool isRentFree,
        decimal? rentedArea,
        decimal? allocatedArea,
        int? excludeLeaseId = null,
        int? excludeLeaseGroupId = null,
        decimal? rateArea = null);

    void EnsureCapacityAvailable(
        int capacity,
        decimal unitArea,
        IReadOnlyCollection<CapacityOccupant> occupants,
        DateTime rangeStart,
        DateTime rangeEnd,
        string field);

    /// <summary>
    /// Uzatma doğrulamaları (durum, tarih, çakışma/kontenjan, tarife, KDV). Lease.Unit yüklü olmalıdır.
    /// Kapsam ve gruplu sözleşme kontrolü çağırana aittir.
    /// </summary>
    Task EnsureExtensionAllowedAsync(
        Lease lease,
        DateTime newEndDate,
        bool isVatApplied,
        decimal vatRate,
        bool updateRate,
        bool canOverrideRate,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        int? excludeLeaseGroupId = null);

    Task EnsureRateOverridesAsync(
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        decimal unitArea,
        bool isRentFree = false);

    /// <summary>
    /// Onay doğrulamalarını çalıştırır ve sözleşmeyi Active yapar; inceleme geçmişi ve
    /// işlem günlüğünü ekler. Kaydetme ve tahakkuk üretimi çağırana aittir.
    /// </summary>
    Task ActivateAsync(
        Lease lease,
        string actorUserId,
        string? explanation,
        LeaseAccessScopeInput accessScope);

    Task AddReviewAsync(
        Lease lease,
        LeaseReviewActionType actionType,
        LeaseStatus? fromStatus,
        LeaseStatus? toStatus,
        string actorUserId,
        string? explanation = null);
}
