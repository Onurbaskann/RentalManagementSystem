using KiraTakip.Data;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Properties;
using Microsoft.EntityFrameworkCore.Storage;

namespace KiraTakip.Tests;

// PropertyRepository.GetDetailsAsync'teki N+1/OUTER APPLY düzeltmesinin (birim başına 12 correlated
// subquery yerine bulk sorgu + bellekte birleştirme) her birim durumunda (boş, kiralı, süresi yakında
// dolan, rezervasyon tarifeli) doğru sonuç ürettiğini doğrular.
[Collection("Database collection")]
public class PropertyDetailsBulkFieldsTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public PropertyDetailsBulkFieldsTests(DatabaseFixture fixture)
    {
        _context = fixture.CreateContext();
        _transaction = _context.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Fact]
    public async Task GetDetailsAsync_ShouldPopulateActiveLeaseAndReservationRateFieldsPerUnit()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.Now;

        var property = new Property { Name = $"Bulk Detay Taşınmaz {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Bulk Detay Birim Türü {suffix}",
            Code = $"BDU_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        _context.AddRange(property, unitType);
        await _context.SaveChangesAsync();

        var vacantUnit = new Unit { PropertyId = property.Id, UnitTypeId = unitType.Id, Name = $"Boş {suffix}", Area = 10m };
        var leasedUnit = new Unit { PropertyId = property.Id, UnitTypeId = unitType.Id, Name = $"Kiralı {suffix}", Area = 20m };
        var expiringUnit = new Unit { PropertyId = property.Id, UnitTypeId = unitType.Id, Name = $"Süresi Dolan {suffix}", Area = 30m };
        var reservationRateUnit = new Unit { PropertyId = property.Id, UnitTypeId = unitType.Id, Name = $"Tarifeli {suffix}", Area = 40m };
        _context.Units.AddRange(vacantUnit, leasedUnit, expiringUnit, reservationRateUnit);
        await _context.SaveChangesAsync();

        var leasedTenant = new Tenant { TenantNo = $"BDT1-{suffix}", Name = $"Kiracı 1 {suffix}" };
        var expiringTenant = new Tenant { TenantNo = $"BDT2-{suffix}", Name = $"Kiracı 2 {suffix}" };
        _context.Tenants.AddRange(leasedTenant, expiringTenant);
        await _context.SaveChangesAsync();

        var leasedLease = new Lease
        {
            LeaseNo = $"TEST-{Guid.NewGuid():N}"[..20],
            UnitId = leasedUnit.Id,
            TenantId = leasedTenant.Id,
            StartDate = now.AddMonths(-1),
            EndDate = now.AddYears(1),
            Status = LeaseStatus.Active,
            IsRentFree = false
        };
        var expiringLease = new Lease
        {
            LeaseNo = $"TEST-{Guid.NewGuid():N}"[..20],
            UnitId = expiringUnit.Id,
            TenantId = expiringTenant.Id,
            StartDate = now.AddMonths(-6),
            EndDate = now.AddDays(10),
            Status = LeaseStatus.Active,
            IsRentFree = true
        };
        _context.Leases.AddRange(leasedLease, expiringLease);

        _context.RezervasyonTarifeler.Add(new ReservationRateOverride
        {
            UnitId = reservationRateUnit.Id,
            PeriodRate = 55m,
            BillingPeriodMinutes = 60,
            FreeDurationMinutes = 15,
            KdvRate = 20m
        });
        await _context.SaveChangesAsync();

        var repository = new PropertyRepository(_context);

        var details = await repository.GetDetailsAsync(property.Id);

        Assert.NotNull(details);
        var vacant = Assert.Single(details!.Units, u => u.Id == vacantUnit.Id);
        var leased = Assert.Single(details.Units, u => u.Id == leasedUnit.Id);
        var expiring = Assert.Single(details.Units, u => u.Id == expiringUnit.Id);
        var reservationRated = Assert.Single(details.Units, u => u.Id == reservationRateUnit.Id);

        Assert.Equal(OccupancyStatus.Vacant, vacant.Status);
        Assert.Null(vacant.ActiveLeaseId);
        Assert.Null(vacant.ReservationRateOverrideId);

        Assert.Equal(OccupancyStatus.Leased, leased.Status);
        Assert.Equal(leasedLease.Id, leased.ActiveLeaseId);
        Assert.Equal(leasedTenant.Id, leased.ActiveLeaseTenantId);
        Assert.False(leased.ActiveLeaseIsRentFree);
        Assert.Equal(leasedTenant.Name, leased.ActiveLeaseTenantDisplayName);
        Assert.Equal(leasedLease.EndDate, leased.ActiveLeaseEndDate);

        Assert.Equal(OccupancyStatus.ExpiringSoon, expiring.Status);
        Assert.Equal(expiringLease.Id, expiring.ActiveLeaseId);
        Assert.True(expiring.ActiveLeaseIsRentFree);

        Assert.Equal(OccupancyStatus.Vacant, reservationRated.Status);
        Assert.NotNull(reservationRated.ReservationRateOverrideId);
        Assert.Equal(55m, reservationRated.ReservationPeriodRate);
        Assert.Equal(60, reservationRated.ReservationBillingPeriodMinutes);
        Assert.Equal(15, reservationRated.ReservationFreeDurationMinutes);
        Assert.Equal(20m, reservationRated.ReservationVatRate);
    }
}
