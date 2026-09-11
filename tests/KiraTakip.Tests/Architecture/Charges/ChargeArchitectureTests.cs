using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Pricing;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Reservations;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Pricing;
using KiraTakip.Services.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.ManualCharge;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class ChargeArchitectureTests : IDisposable
{
    private readonly ApplicationDbContext _ctx;
    private readonly IDbContextTransaction _tx;

    public ChargeArchitectureTests(DatabaseFixture fixture)
    {
        _ctx = fixture.CreateContext();
        _tx = _ctx.Database.BeginTransaction();
    }

    public void Dispose()
    {
        _tx.Rollback();
        _tx.Dispose();
        _ctx.Dispose();
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    private ManualChargeService CreateManuelBorcService()
    {
        return new ManualChargeService(new ChargeRepository(_ctx),
                                       new LeaseRepository(_ctx),
                                       new ChargeTypeRepository(_ctx),
                                       new UnitRepository(_ctx),
                                       new TenantRepository(_ctx),
                                       new UnitOfWork(_ctx));
    }

    private ChargeGenerationService CreateUretimService()
    {
        return new ChargeGenerationService(new ChargeRepository(_ctx),
                                           new ChargeTypeRepository(_ctx),
                                           new UnitOfWork(_ctx),
                                           new RateResolverService(
                                               new LeaseRateOverrideRepository(_ctx),
                                               new UnitRateRepository(_ctx),
                                               new PropertyRateOverrideRepository(_ctx),
                                               new RateScheduleRepository(_ctx),
                                               new LeaseRepository(_ctx),
                                               new UnitRepository(_ctx),
                                               new TenantRepository(_ctx)),
                                           new LeaseRepository(_ctx),
                                           new UnitRepository(_ctx),
                                           new TenantRepository(_ctx));
    }

    private ReservationService CreateReservationService()
    {
        return new ReservationService(new ReservationRepository(_ctx),
                                      new ReservationRateOverrideRepository(_ctx),
                                      new ChargeRepository(_ctx),
                                      new ChargeTypeRepository(_ctx),
                                      new UnitRepository(_ctx),
                                      new TenantRepository(_ctx),
                                      ReservationBusinessRulesTestFactory.Create(),
                                      new UnitOfWork(_ctx),
                                      new KiraTakip.Infrastructure.Persistence.EfCoreConcurrencyViolationDetector());
    }

    private async Task<(Property, Unit, Tenant, ChargeType)> SeedTemelAsync()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];

        var property = new Property { Name = $"Plaza_{uid}", City = "İstanbul", District = "Kadıköy" };
        _ctx.Properties.Add(property);

        var borcTipi = new ChargeType { Name = $"Kira_{uid}", Code = $"KR_{uid}", Behavior = ChargeTypeBehavior.MonthlyFixed };
        _ctx.ChargeTypes.Add(borcTipi);

        var unitType = new UnitType { Name = $"Ofis_{uid}", Code = $"OF_{uid}", Usage = UnitTypeUsage.Rentable };
        _ctx.UnitTypes.Add(unitType);

        await _ctx.SaveChangesAsync();

        var unit = new Unit { PropertyId = property.Id, Name = $"Ofis_{uid}", Area = 50, UnitTypeId = unitType.Id };
        _ctx.Units.Add(unit);

        var tenant = new Tenant { Name = $"Kiraci_{uid}" };
        _ctx.Tenants.Add(tenant);

        await _ctx.SaveChangesAsync();

        return (property, unit, tenant, borcTipi);
    }

    // ── Test 1: Sözleşme tahakkuku BirimId invariantı ────────────────────────

    [Fact]
    public async Task SozlesmeTahakkuku_BirimId_DogruSetEdilir()
    {
        var (property, unit, tenant, borcTipi) = await SeedTemelAsync();
        var kategori = new Category { Type = CategoryType.Tenant, Name = "Kat", Code = $"KT_{unit.Name}" };
        _ctx.Kategoriler.Add(kategori);
        await _ctx.SaveChangesAsync();

        _ctx.TasinmazTarifeler.Add(new PropertyRateOverride
        {
            PropertyId = property.Id,
            TenantCategoryId = kategori.Id,
            ChargeTypeId = borcTipi.Id,
            UnitValue = 3000,
            CalculationMethod = CalculationMethod.Fixed,
            KdvRate = 0
        });
        tenant.TenantCategoryId = kategori.Id;
        await _ctx.SaveChangesAsync();

        var lease = new Lease
        {
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31)
        };
        _ctx.Leases.Add(lease);
        await _ctx.SaveChangesAsync();

        await CreateUretimService().GenerateForLeaseAsync(new GenerateLeaseChargesInput(lease.Id));

        var tahakkuklar = await _ctx.Charges
            .Where(t => t.LeaseId == lease.Id)
            .ToListAsync();

        Assert.NotEmpty(tahakkuklar);
        Assert.All(tahakkuklar, t =>
        {
            Assert.Equal(unit.Id, t.UnitId);
            Assert.Equal(tenant.Id, t.TenantId);
            Assert.Equal(ChargeSourceType.Lease, t.SourceType);
            Assert.Null(t.ReservationId);
        });
    }

    // ── Test 2: Manuel borç sözleşmesiz, BirimId zorunlu ────────────────────

    [Fact]
    public async Task ManuelBorc_SozlesmeSiz_BirimIdIleOlusturulabilir()
    {
        var (_, unit, tenant, _) = await SeedTemelAsync();
        var uid = unit.Name;

        var manuelBorcTipi = new ChargeType
        {
            Name = $"Manuel_{uid}",
            Code = $"MN_{uid}",
            Behavior = ChargeTypeBehavior.UserManual
        };
        _ctx.ChargeTypes.Add(manuelBorcTipi);
        await _ctx.SaveChangesAsync();

        var input = new CreateManualChargeInput(
            tenant.Id, null, unit.Id, manuelBorcTipi.Id, "Test manuel borç",
            1500m, false, 0m, new DateTime(2026, 2, 1), null,
            new ManualChargeAccessScopeInput());

        await CreateManuelBorcService().CreateAsync(input);

        var charge = await _ctx.Charges.FirstOrDefaultAsync(c => c.TenantId == tenant.Id && c.UnitId == unit.Id);
        Assert.NotNull(charge);
        var chargeId = charge.Id;
        Assert.NotNull(charge);
        Assert.Equal(unit.Id, charge.UnitId);
        Assert.Equal(tenant.Id, charge.TenantId);
        Assert.Equal(ChargeSourceType.Manual, charge.SourceType);
        Assert.Null(charge.LeaseId);
    }

    // ── Test 3: Manuel borç farklı kiracı → hata ─────────────────────────────

    [Fact]
    public async Task ManuelBorc_FarkliKiraci_HataVerir()
    {
        var (_, unit, kiraci1, _) = await SeedTemelAsync();
        var uid = Guid.NewGuid().ToString("N")[..8];

        var kiraci2 = new Tenant { Name = $"Kiraci2_{uid}", TenantNo = uid };
        _ctx.Tenants.Add(kiraci2);

        var manuelBorcTipi = new ChargeType
        {
            Name = $"Manuel_{uid}",
            Code = $"MN_{uid}",
            Behavior = ChargeTypeBehavior.UserManual
        };
        _ctx.ChargeTypes.Add(manuelBorcTipi);
        await _ctx.SaveChangesAsync();

        var lease = new Lease
        {
            UnitId = unit.Id,
            TenantId = kiraci1.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        _ctx.Leases.Add(lease);
        await _ctx.SaveChangesAsync();

        var input = new CreateManualChargeInput(
            kiraci2.Id, lease.Id, unit.Id, manuelBorcTipi.Id, "Test manuel borç",
            500m, false, 0m, new DateTime(2026, 2, 1), null,
            new ManualChargeAccessScopeInput());

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateManuelBorcService().CreateAsync(input));

        Assert.Equal("MANUAL_CHARGE_LEASE_TENANT_MISMATCH", exception.Code);
    }

    [Fact]
    public async Task ManuelBorc_SozlesmeBirimiyleEslesmeyenBirim_HataVerir()
    {
        var (property, leaseUnit, tenant, _) = await SeedTemelAsync();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var otherUnit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = leaseUnit.UnitTypeId,
            Name = $"DigerOfis_{uid}",
            Area = 25
        };
        var manualChargeType = new ChargeType
        {
            Name = $"Manuel_{uid}",
            Code = $"MN_{uid}",
            Behavior = ChargeTypeBehavior.UserManual
        };
        _ctx.Units.Add(otherUnit);
        _ctx.ChargeTypes.Add(manualChargeType);
        await _ctx.SaveChangesAsync();

        var lease = new Lease
        {
            UnitId = leaseUnit.Id,
            TenantId = tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        _ctx.Leases.Add(lease);
        await _ctx.SaveChangesAsync();

        var input = new CreateManualChargeInput(
            tenant.Id, lease.Id, otherUnit.Id, manualChargeType.Id, "Test manuel borç",
            500m, false, 0m, new DateTime(2026, 2, 1), null,
            new ManualChargeAccessScopeInput());

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => CreateManuelBorcService().CreateAsync(input));

        Assert.Equal("MANUAL_CHARGE_LEASE_UNIT_MISMATCH", exception.Code);
    }

    [Fact]
    public async Task ManuelBorc_YetkisizBirimdeOlusturulamaz()
    {
        var (_, unit, tenant, _) = await SeedTemelAsync();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var manualChargeType = new ChargeType
        {
            Name = $"Manuel_{uid}",
            Code = $"MN_{uid}",
            Behavior = ChargeTypeBehavior.UserManual
        };
        _ctx.ChargeTypes.Add(manualChargeType);
        await _ctx.SaveChangesAsync();

        var input = new CreateManualChargeInput(
            tenant.Id, null, unit.Id, manualChargeType.Id, "Test manuel borç",
            500m, false, 0m, new DateTime(2026, 2, 1), null,
            new ManualChargeAccessScopeInput([], []));

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => CreateManuelBorcService().CreateAsync(input));

        Assert.Equal("MANUAL_CHARGE_UNIT_FORBIDDEN", exception.Code);
    }

    [Fact]
    public async Task ManuelBorc_YetkisizKapsamdanIptalEdilemez()
    {
        var (_, unit, tenant, _) = await SeedTemelAsync();
        var uid = Guid.NewGuid().ToString("N")[..8];
        var manualChargeType = new ChargeType
        {
            Name = $"Manuel_{uid}",
            Code = $"MN_{uid}",
            Behavior = ChargeTypeBehavior.UserManual
        };
        _ctx.ChargeTypes.Add(manualChargeType);
        await _ctx.SaveChangesAsync();

        var service = CreateManuelBorcService();
        await service.CreateAsync(new CreateManualChargeInput(
            tenant.Id, null, unit.Id, manualChargeType.Id, "Test manuel borç",
            500m, false, 0m, new DateTime(2026, 2, 1), null,
            new ManualChargeAccessScopeInput()));

        var createdCharge = await _ctx.Charges.FirstAsync(c => c.TenantId == tenant.Id && c.UnitId == unit.Id);
        var chargeId = createdCharge.Id;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CancelAsync(
            new CancelManualChargeInput(
                chargeId,
                "Test iptal nedeni",
                new ManualChargeAccessScopeInput([], []))));

        Assert.Equal("MANUAL_CHARGE_NOT_FOUND", exception.Code);
    }

    // ── Test 4: Reservation tahakkuku BirimId + ReservationId invariantı ─────

    [Fact]
    public async Task RezervasyonTahakkuku_BirimId_VeRezervasyonId_SetEdilir()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Rez_Plaza_{uid}", City = "Ankara", District = "Çankaya" };
        _ctx.Properties.Add(property);

        var rezBorcTipi = new ChargeType
        {
            Name = $"Toplanti_{uid}",
            Code = $"TP_{uid}",
            Behavior = ChargeTypeBehavior.ReservationSpecific,
            IsSystem = true
        };
        _ctx.ChargeTypes.Add(rezBorcTipi);
        await _ctx.SaveChangesAsync();

        var birimTuru = new UnitType
        {
            Name = $"Salon_{uid}",
            Code = $"SL_{uid}",
            Usage = UnitTypeUsage.Reservable,
            ChargeTypeId = rezBorcTipi.Id
        };
        _ctx.UnitTypes.Add(birimTuru);
        await _ctx.SaveChangesAsync();

        var unit = new Unit { PropertyId = property.Id, Name = $"Salon101_{uid}", Area = 80, UnitTypeId = birimTuru.Id };
        _ctx.Units.Add(unit);

        var tenant = new Tenant { Name = $"KiraciRez_{uid}" };
        _ctx.Tenants.Add(tenant);
        await _ctx.SaveChangesAsync();

        var baslangic = new DateTime(2026, 3, 1, 10, 0, 0);
        var bitis = new DateTime(2026, 3, 1, 12, 0, 0);
        var reservation = new Reservation
        {
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = baslangic,
            EndDate = bitis,
            TotalDurationMinutes = 120,
            FreeDurationMinutes = 0,
            PaidDurationMinutes = 120,
            UnitRate = 250m,
            RateAmount = 500m,
            TotalAmount = 500m,
            Status = ReservationStatus.Confirmed
        };
        _ctx.Reservations.Add(reservation);
        await _ctx.SaveChangesAsync();

        var chargeId = await CreateReservationService().TransferToChargeAsync(
            new TransferReservationToChargeInput(
                reservation.Id,
                new ReservationAccessScopeInput()));

        var charge = await _ctx.Charges.FindAsync(chargeId);
        Assert.NotNull(charge);
        Assert.Equal(unit.Id, charge.UnitId);
        Assert.Equal(tenant.Id, charge.TenantId);
        Assert.Equal(reservation.Id, charge.ReservationId);
        Assert.Equal(ChargeSourceType.Reservation, charge.SourceType);
        Assert.Null(charge.LeaseId);
    }

    // ── Test 5: Kapsam filtresi — yetkiliBirimIds ─────────────────────────────

    [Fact]
    public async Task ScopeFiltresi_YetkiliBirimIds_DogruFiltreler()
    {
        var uid = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Scope_Plaza_{uid}", City = "İzmir", District = "Konak" };
        _ctx.Properties.Add(property);

        var tenant = new Tenant { Name = $"ScopeKiraci_{uid}" };
        _ctx.Tenants.Add(tenant);

        var borcTipi = new ChargeType { Name = $"ScopeBT_{uid}", Code = $"SC_{uid}", Behavior = ChargeTypeBehavior.MonthlyFixed };
        _ctx.ChargeTypes.Add(borcTipi);

        var unitType = new UnitType { Name = $"ScopeUT_{uid}", Code = $"SU_{uid}", Usage = UnitTypeUsage.Rentable };
        _ctx.UnitTypes.Add(unitType);
        await _ctx.SaveChangesAsync();

        var birim1 = new Unit { PropertyId = property.Id, Name = $"Birim1_{uid}", Area = 30, UnitTypeId = unitType.Id };
        var birim2 = new Unit { PropertyId = property.Id, Name = $"Birim2_{uid}", Area = 40, UnitTypeId = unitType.Id };
        _ctx.Units.AddRange(birim1, birim2);
        await _ctx.SaveChangesAsync();

        var t1 = new Charge
        {
            TenantId = tenant.Id,
            UnitId = birim1.Id,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            DueDate = new DateTime(2026, 1, 31),
            ExpectedAmount = 1000,
            TotalAmount = 1000,
            SourceType = ChargeSourceType.Lease,
            Status = ChargeStatus.Pending
        };
        var t2 = new Charge
        {
            TenantId = tenant.Id,
            UnitId = birim2.Id,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            DueDate = new DateTime(2026, 1, 31),
            ExpectedAmount = 2000,
            TotalAmount = 2000,
            SourceType = ChargeSourceType.Lease,
            Status = ChargeStatus.Pending
        };
        _ctx.Charges.AddRange(t1, t2);
        await _ctx.SaveChangesAsync();

        var repo = new ChargeRepository(_ctx);
        var sonuclar = await repo.GetListAsync(null, authorizedPropertyIds: null, authorizedUnitIds: [birim1.Id]);

        Assert.NotEmpty(sonuclar);
        Assert.All(sonuclar.Where(s => s.Id == t1.Id || s.Id == t2.Id), s =>
            Assert.Equal(birim1.Id, s.UnitId));
        Assert.DoesNotContain(sonuclar, s => s.Id == t2.Id);
        Assert.Contains(sonuclar, s => s.Id == t1.Id);
    }
}
