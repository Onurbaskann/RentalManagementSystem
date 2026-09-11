using Castle.DynamicProxy;
using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Dtos.Tenant;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Web.Models.ViewModels;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Catalog;
using KiraTakip.Repositories.Documents;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Documents;
using KiraTakip.Services.Interfaces.Tenants;
using KiraTakip.Services.Tenants;
using KiraTakip.Web.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace KiraTakip.Tests;

public class TenantValidationTests
{
    [Theory]
    [InlineData("tenantNo", 21, "TenantNo")]
    [InlineData("name", 201, "Name")]
    [InlineData("phone", 31, "Phone")]
    [InlineData("email", 201, "Email")]
    [InlineData("representativeEmail", 257, "InitialRepresentativeEmail")]
    [InlineData("representativeName", 201, "InitialRepresentativeFullName")]
    public void Validator_RejectsValuesExceedingDatabaseLimits(
        string property,
        int length,
        string expectedField)
    {
        var model = ValidModel();
        var value = new string('a', length);

        switch (property)
        {
            case "tenantNo": model.TenantNo = value; break;
            case "name": model.Name = value; break;
            case "phone": model.Phone = value; break;
            case "email": model.Email = $"{value}@test.local"; break;
            case "representativeEmail": model.InitialRepresentativeEmail = $"{value}@test.local"; break;
            case "representativeName": model.InitialRepresentativeFullName = value; break;
        }

        var result = new TenantFormViewModelValidator().Validate(model);

        Assert.Contains(result.Errors, error => error.Field == expectedField);
    }

    [Fact]
    public void Validator_RejectsInvalidTaxAndRepresentativeEmail()
    {
        var model = ValidModel();
        model.TaxNo = "123";
        model.InitialRepresentativeEmail = "gecersiz";

        var result = new TenantFormViewModelValidator().Validate(model);

        Assert.Contains(result.Errors, error => error.Field == nameof(model.TaxNo));
        Assert.Contains(result.Errors, error => error.Field == nameof(model.InitialRepresentativeEmail));
    }

    private static TenantFormViewModel ValidModel()
        => new()
        {
            TenantNo = "KRC-000001",
            Name = "Test Kiracı",
            TaxNo = "1234567890",
            TaxOffice = "Test",
            Phone = "5551112233",
            Email = "test@example.com",
            Address = "Test adresi",
            TenantCategoryId = 1,
            SectorId = 2
        };
}

[Collection("Database collection")]
public class TenantArchitectureTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public TenantArchitectureTests(DatabaseFixture fixture)
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
    public async Task Repository_AppliesDirectUnitScopeToListAndDetails()
    {
        var seed = await SeedAsync();
        var repository = new TenantRepository(_context);

        var list = await repository.GetListAsync([], [seed.FirstUnitId]);
        var visible = await repository.GetDetailsAsync(seed.FirstTenantId, [], [seed.FirstUnitId]);
        var hidden = await repository.GetDetailsAsync(seed.SecondTenantId, [], [seed.FirstUnitId]);

        Assert.Contains(list, tenant => tenant.Id == seed.FirstTenantId);
        Assert.DoesNotContain(list, tenant => tenant.Id == seed.SecondTenantId);
        Assert.NotNull(visible);
        Assert.Null(hidden);
    }

    [Fact]
    public async Task Update_ChangesTenantNoWithoutReactivatingInactiveTenant()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        var newTenantNo = $"NEW-{Guid.NewGuid():N}"[..20];

        await service.UpdateAsync(new UpdateTenantInput(
            seed.FirstTenantId,
            newTenantNo,
            "Güncellenen Kiracı",
            null,
            seed.FirstTaxNo,
            "Test VD",
            null,
            "5551112233",
            "updated@test.local",
            "Adres",
            seed.TenantCategoryId,
            seed.SectorId,
            new TenantAccessScopeInput([], [seed.FirstUnitId])));

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == seed.FirstTenantId);
        Assert.Equal(newTenantNo, tenant.TenantNo);
        Assert.False(tenant.IsActive);
    }

    [Fact]
    public async Task Update_RejectsForeignScopeAndDuplicateTaxNumber()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        var scopeException = await Assert.ThrowsAsync<BusinessException>(() =>
            service.UpdateAsync(BuildUpdateInput(
                seed.SecondTenantId,
                seed.FirstTaxNo,
                seed,
                new TenantAccessScopeInput([], [seed.FirstUnitId]))));
        var taxException = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            service.UpdateAsync(BuildUpdateInput(
                seed.FirstTenantId,
                seed.SecondTaxNo,
                seed,
                new TenantAccessScopeInput([], [seed.FirstUnitId]))));

        Assert.Equal(ErrorType.NotFound, scopeException.ErrorType);
        Assert.Equal(nameof(UpdateTenantInput.TaxNo), taxException.Field);
    }

    [Fact]
    public async Task Update_RejectsCategoryWithWrongType()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        var input = BuildUpdateInput(
            seed.FirstTenantId,
            seed.FirstTaxNo,
            seed,
            new TenantAccessScopeInput([], [seed.FirstUnitId])) with
        {
            TenantCategoryId = seed.SectorId
        };

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(
            () => service.UpdateAsync(input));

        Assert.Equal(nameof(UpdateTenantInput.TenantCategoryId), exception.Field);
    }

    private TenantService CreateService()
    {
        var unitOfWork = new UnitOfWork(_context);
        return new TenantService(
            new TenantRepository(_context),
            new CategoryRepository(_context),
            unitOfWork,
            CreateDocumentService(unitOfWork));
    }

    private DocumentService CreateDocumentService(IUnitOfWork unitOfWork)
        => new(
            new DocumentRepository(_context),
            new DocumentContentRepository(_context),
            new DocumentTypeRepository(_context),
            new TenantRepository(_context),
            new LeaseRepository(_context),
            new PaymentAllocationRepository(_context),
            unitOfWork);

    private async Task<TenantSeed> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var firstTaxNo = Random.Shared.NextInt64(1_000_000_000, 10_000_000_000).ToString();
        string secondTaxNo;
        do
        {
            secondTaxNo = Random.Shared.NextInt64(1_000_000_000, 10_000_000_000).ToString();
        } while (secondTaxNo == firstTaxNo);
        var category = new Category
        {
            Type = CategoryType.Tenant,
            Name = $"Kategori {suffix}",
            Code = $"KAT_{suffix}",
            Order = 1
        };
        var sector = new Category
        {
            Type = CategoryType.Sector,
            Name = $"Sektör {suffix}",
            Code = $"SEK_{suffix}",
            Order = 1
        };
        var property1 = new Property { Name = $"Taşınmaz 1 {suffix}" };
        var property2 = new Property { Name = $"Taşınmaz 2 {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Birim Türü {suffix}",
            Code = $"UNIT_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant1 = new Tenant
        {
            TenantNo = $"T1-{suffix}",
            Name = $"Kiracı 1 {suffix}",
            TaxNo = firstTaxNo,
            TenantCategory = category,
            Sector = sector,
            IsActive = false
        };
        var tenant2 = new Tenant
        {
            TenantNo = $"T2-{suffix}",
            Name = $"Kiracı 2 {suffix}",
            TaxNo = secondTaxNo,
            TenantCategory = category,
            Sector = sector
        };

        _context.AddRange(category, sector, property1, property2, unitType, tenant1, tenant2);
        await _context.SaveChangesAsync();

        var unit1 = new Unit
        {
            PropertyId = property1.Id,
            UnitTypeId = unitType.Id,
            Name = $"Birim 1 {suffix}",
            Area = 10
        };
        var unit2 = new Unit
        {
            PropertyId = property2.Id,
            UnitTypeId = unitType.Id,
            Name = $"Birim 2 {suffix}",
            Area = 10
        };
        _context.Units.AddRange(unit1, unit2);
        await _context.SaveChangesAsync();

        _context.Leases.AddRange(
            new Lease
            {
                UnitId = unit1.Id,
                TenantId = tenant1.Id,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                Status = LeaseStatus.Active
            },
            new Lease
            {
                UnitId = unit2.Id,
                TenantId = tenant2.Id,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                Status = LeaseStatus.Active
            });
        await _context.SaveChangesAsync();

        return new TenantSeed(
            tenant1.Id,
            tenant2.Id,
            unit1.Id,
            category.Id,
            sector.Id,
            firstTaxNo,
            secondTaxNo);
    }

    private static UpdateTenantInput BuildUpdateInput(
        int tenantId,
        string taxNo,
        TenantSeed seed,
        TenantAccessScopeInput accessScope)
        => new(
            tenantId,
            $"UPD-{Guid.NewGuid():N}"[..20],
            "Güncel Kiracı",
            null,
            taxNo,
            "Test VD",
            null,
            "5551112233",
            "updated@test.local",
            "Adres",
            seed.TenantCategoryId,
            seed.SectorId,
            accessScope);

    private sealed record TenantSeed(
        int FirstTenantId,
        int SecondTenantId,
        int FirstUnitId,
        int TenantCategoryId,
        int SectorId,
        string FirstTaxNo,
        string SecondTaxNo);
}

[Collection("Database collection")]
public class TenantTransactionTests
{
    private readonly DatabaseFixture _fixture;

    public TenantTransactionTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_RollsBackTenantWhenDocumentUploadFails()
    {
        await using var context = _fixture.CreateContext();
        var category = await context.Kategoriler
            .FirstAsync(item => item.Type == CategoryType.Tenant && item.IsActive);
        var sector = await context.Kategoriler
            .FirstAsync(item => item.Type == CategoryType.Sector && item.IsActive);
        var requiredTypes = await context.DocumentTypes
            .Where(item => item.TargetEntity == DocumentOwnerType.Tenant
                && item.Required
                && item.IsActive)
            .ToListAsync();
        Assert.NotEmpty(requiredTypes);

        var unitOfWork = new UnitOfWork(context);
        var target = new TenantService(
            new TenantRepository(context),
            new CategoryRepository(context),
            unitOfWork,
            new DocumentService(
                new DocumentRepository(context),
                new DocumentContentRepository(context),
                new DocumentTypeRepository(context),
                new TenantRepository(context),
                new LeaseRepository(context),
                new PaymentAllocationRepository(context),
                unitOfWork));
        var interceptor = new TransactionInterceptor(
            context,
            NullLogger<TransactionInterceptor>.Instance);
        var service = new ProxyGenerator().CreateInterfaceProxyWithTarget<ITenantService>(
            target,
            interceptor.ToInterceptor());

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantNo = $"RB-{suffix}";
        var documents = requiredTypes
            .Select(type => new TenantDocumentUploadInput(
                type.Id,
                "gecersiz.not-allowed",
                "application/octet-stream",
                [1, 2, 3]))
            .ToList();

        await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(
            new CreateTenantInput(
                tenantNo,
                $"Rollback Kiracı {suffix}",
                null,
                $"{Random.Shared.Next(100000000, 999999999)}0",
                "Test VD",
                null,
                "5551112233",
                $"rollback-{suffix}@test.local",
                "Adres",
                category.Id,
                sector.Id,
                documents,
                new TenantAccessScopeInput())));

        context.ChangeTracker.Clear();
        Assert.False(await context.Tenants
            .IgnoreQueryFilters()
            .AnyAsync(tenant => tenant.TenantNo == tenantNo));
    }
}
