using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;

namespace KiraTakip.Services.Interfaces.Leases;

public sealed record ValidatedLeaseApplication(LeaseSpaceContextDto Space, decimal RentedArea);

/// <summary>
/// Tekil sözleşme ve paylaşımlı grup akışlarının ortak başvuru/onay kuralları.
/// </summary>
public interface ILeaseApplicationRules
{
    /// <summary>
    /// Alan (aktif, kiralanabilir, kapsamda), kiracı, tarih ve tarife doğrulaması; çakışma yalnız aynı alan için.
    /// isGroupFlow: true = alan paylaşımlı olmalı, false = paylaşımlı alan reddedilir (K27).
    /// rentedArea boşsa alanın m²'si kullanılır.
    /// </summary>
    Task<ValidatedLeaseApplication> ValidateApplicationDataAsync(
        int spaceId,
        int tenantId,
        DateTime startDate,
        DateTime endDate,
        int dueDay,
        IReadOnlyCollection<LeaseRateOverrideInput> rateOverrides,
        SpaceAccessScope accessScope,
        bool isRentFree,
        decimal? rentedArea,
        bool isGroupFlow = false,
        int? excludeLeaseId = null,
        int? excludeLeaseGroupId = null);

    /// <summary>
    /// Uzatma doğrulamaları (durum, tarih, alan hâlâ aktif/kiralanabilir, çakışma, tarife, KDV).
    /// Lease.Areas (Space yüklü) dolu olmalıdır. Kapsam ve gruplu sözleşme kontrolü çağırana aittir.
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
        decimal area,
        bool isRentFree = false);

    /// <summary>
    /// Onay doğrulamalarını çalıştırır, alan satırının anlık alanlarını alanın o anki değerlerinden yeniden yazar
    /// ve sözleşmeyi Active yapar; inceleme geçmişi ve işlem günlüğünü ekler. Lease.Areas dolu olmalıdır.
    /// Kaydetme ve tahakkuk üretimi çağırana aittir.
    /// </summary>
    Task ActivateAsync(
        Lease lease,
        string actorUserId,
        string? explanation,
        SpaceAccessScope accessScope);

    Task AddReviewAsync(
        Lease lease,
        LeaseReviewActionType actionType,
        LeaseStatus? fromStatus,
        LeaseStatus? toStatus,
        string actorUserId,
        string? explanation = null);
}
