using KiraTakip.Data;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories;
using KiraTakip.Repositories.Charges;
using KiraTakip.Repositories.Documents;
using KiraTakip.Repositories.Leases;
using KiraTakip.Repositories.Payments;
using KiraTakip.Repositories.Properties;
using KiraTakip.Repositories.Tenants;
using KiraTakip.Services.Charges;
using KiraTakip.Services.Documents;
using KiraTakip.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KiraTakip.Models.Dtos.Payment;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class ChargeLineItemPaymentBalanceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public ChargeLineItemPaymentBalanceTests(DatabaseFixture fixture)
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

    private PaymentService CreateService()
    {
        var unitOfWork = new UnitOfWork(_context);
        var paymentRepository = new PaymentAllocationRepository(_context);
        var chargeLineItemRepository = new ChargeLineItemRepository(_context);
        var chargeService = new ChargeService(
            new ChargeRepository(_context),
            new PaymentAllocationRepository(_context),
            new UnitRepository(_context),
            chargeLineItemRepository,
            unitOfWork);
        var documentService = new DocumentService(
            new DocumentRepository(_context),
            new DocumentContentRepository(_context),
            new DocumentTypeRepository(_context),
            new TenantRepository(_context),
            new LeaseRepository(_context),
            paymentRepository,
            unitOfWork);
        var storeResolver = new PaymentStoreResolver(new PaymentStoreRoutingRepository(_context));

        return new PaymentService(
            paymentRepository,
            unitOfWork,
            chargeService,
            documentService,
            chargeLineItemRepository,
            storeResolver,
            new PaymentBusinessRules());
    }

    private async Task<(
        Charge Charge,
        ChargeLineItem FirstLineItem,
        ChargeLineItem SecondLineItem,
        ApplicationUser User,
        int StoreAccountId)> SeedTwoLineItemChargeAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property { Name = $"Bakiye Test {suffix}" };
        var unitType = new UnitType
        {
            Name = $"Bakiye Birim Türü {suffix}",
            Code = $"BAL_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant { TenantNo = $"BAL-{suffix}", Name = $"Bakiye Kiracı {suffix}" };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"balance-{suffix}@test.local",
            NormalizedUserName = $"BALANCE-{suffix}@TEST.LOCAL",
            Email = $"balance-{suffix}@test.local",
            NormalizedEmail = $"BALANCE-{suffix}@TEST.LOCAL",
            EmailConfirmed = true,
            IsActive = true
        };
        var firstChargeType = new ChargeType
        {
            Name = $"İkinci Sırada {suffix}",
            Code = $"BALCT2_{suffix}",
            IsActive = true,
            SortOrder = 20,
            Behavior = ChargeTypeBehavior.UserManual
        };
        var secondChargeType = new ChargeType
        {
            Name = $"Birinci Sırada {suffix}",
            Code = $"BALCT1_{suffix}",
            IsActive = true,
            SortOrder = 10,
            Behavior = ChargeTypeBehavior.UserManual
        };
        var store = new Store { Name = $"Bakiye Mağaza {suffix}", Code = $"BALSTORE_{suffix}", IsActive = true };
        store.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = $"MERCHANT-{suffix}",
            MerchantUser = "test-user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });

        _context.AddRange(property, unitType, tenant, user, firstChargeType, secondChargeType, store);
        await _context.SaveChangesAsync();

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Bakiye Ofis {suffix}",
            Area = 40m
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var charge = new Charge
        {
            TenantId = tenant.Id,
            UnitId = unit.Id,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            DueDate = new DateTime(2026, 2, 5),
            ExpectedAmount = 1500m,
            TotalAmount = 1500m,
            PaidAmount = 0m,
            Status = ChargeStatus.Pending
        };
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        // Kasıtlı olarak SortOrder'ı büyük olan (firstChargeType, 20) önce eklenir; sorgu
        // sonucunun ChargeType.SortOrder'a göre sıralandığı doğrulanabilsin diye.
        var firstLineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = firstChargeType.Id,
            Description = "İkinci sırada gelmesi gereken kalem",
            Amount = 1000m,
            TotalAmount = 1000m
        };
        var secondLineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = secondChargeType.Id,
            Description = "Birinci sırada gelmesi gereken kalem",
            Amount = 500m,
            TotalAmount = 500m
        };
        _context.ChargeLineItems.AddRange(firstLineItem, secondLineItem);

        _context.PaymentStoreRoutings.AddRange(
            new PaymentStoreRouting
            {
                ChargeTypeId = firstChargeType.Id,
                PropertyId = null,
                UnitId = null,
                StoreId = store.Id,
                IsActive = true
            },
            new PaymentStoreRouting
            {
                ChargeTypeId = secondChargeType.Id,
                PropertyId = null,
                UnitId = null,
                StoreId = store.Id,
                IsActive = true
            });
        await _context.SaveChangesAsync();

        return (charge, firstLineItem, secondLineItem, user, store.Accounts.Single().Id);
    }

    [Fact]
    public async Task GetPaymentBalanceAsync_ShouldSeparateApprovedAndPendingAmounts()
    {
        var seed = await SeedTwoLineItemChargeAsync();
        _context.PaymentAllocations.AddRange(
            new PaymentAllocation
            {
                ChargeId = seed.Charge.Id,
                ChargeLineItemId = seed.FirstLineItem.Id,
                StoreAccountId = seed.StoreAccountId,
                CreatedByUserId = seed.User.Id,
                PaymentDate = DateTime.Today,
                Amount = 300m,
                PaymentChannel = PaymentChannel.Eft,
                Status = PaymentStatus.Approved
            },
            new PaymentAllocation
            {
                ChargeId = seed.Charge.Id,
                ChargeLineItemId = seed.FirstLineItem.Id,
                StoreAccountId = seed.StoreAccountId,
                CreatedByUserId = seed.User.Id,
                PaymentDate = DateTime.Today,
                Amount = 200m,
                PaymentChannel = PaymentChannel.Eft,
                Status = PaymentStatus.PendingApproval
            });
        await _context.SaveChangesAsync();

        var repository = new ChargeLineItemRepository(_context);
        var balance = await repository.GetPaymentBalanceAsync(seed.FirstLineItem.Id);

        Assert.NotNull(balance);
        Assert.Equal(300m, balance.ApprovedAmount);
        Assert.Equal(200m, balance.PendingAmount);
        Assert.Equal(700m, balance.RemainingAmount);
        Assert.Equal(500m, balance.AvailableAmount);
    }

    [Fact]
    public async Task GetPaymentBalancesByChargeAsync_ShouldReturnOneRowPerLineItemOrderedByChargeTypeSortOrder()
    {
        var seed = await SeedTwoLineItemChargeAsync();

        var repository = new ChargeLineItemRepository(_context);
        var balances = await repository.GetPaymentBalancesByChargeAsync(seed.Charge.Id);

        Assert.Equal(2, balances.Count);
        Assert.Equal(seed.SecondLineItem.Id, balances[0].ChargeLineItemId);
        Assert.Equal(seed.FirstLineItem.Id, balances[1].ChargeLineItemId);
    }

    [Fact]
    public async Task GetPayableLineItemsAsync_ShouldExcludeFullyCoveredLineItems()
    {
        var seed = await SeedTwoLineItemChargeAsync();
        _context.PaymentAllocations.Add(new PaymentAllocation
        {
            ChargeId = seed.Charge.Id,
            ChargeLineItemId = seed.SecondLineItem.Id,
            StoreAccountId = seed.StoreAccountId,
            CreatedByUserId = seed.User.Id,
            PaymentDate = DateTime.Today,
            Amount = 500m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.Approved
        });
        await _context.SaveChangesAsync();

        var service = CreateService();
        var payable = await service.GetPayableLineItemsAsync(seed.Charge.Id);

        var payableIds = payable.Select(item => item.ChargeLineItemId).ToList();
        Assert.Contains(seed.FirstLineItem.Id, payableIds);
        Assert.DoesNotContain(seed.SecondLineItem.Id, payableIds);
    }

    [Fact]
    public async Task ChargeAndLineItemPaidAmounts_ShouldStayConsistentAfterApproveAndReject()
    {
        var seed = await SeedTwoLineItemChargeAsync();
        var service = CreateService();
        var scope = new PaymentAccessScopeInput();

        var approvedPaymentId = await service.CreateAsync(new CreatePaymentInput(
            seed.Charge.Id, DateTime.Today, 400m, PaymentChannel.Eft, PaymentSourceType.Manual,
            null, seed.User.Id, scope, ChargeLineItemId: seed.FirstLineItem.Id));
        var rejectedPaymentId = await service.CreateAsync(new CreatePaymentInput(
            seed.Charge.Id, DateTime.Today, 500m, PaymentChannel.Eft, PaymentSourceType.Manual,
            null, seed.User.Id, scope, ChargeLineItemId: seed.SecondLineItem.Id));

        await service.ApproveAsync(new ApprovePaymentInput(approvedPaymentId, seed.User.Id, scope));
        await service.RejectAsync(new RejectPaymentInput(rejectedPaymentId, "Test reddi", scope));

        var firstLineItem = await _context.ChargeLineItems.FindAsync(seed.FirstLineItem.Id);
        var secondLineItem = await _context.ChargeLineItems.FindAsync(seed.SecondLineItem.Id);
        var charge = await _context.Charges.FindAsync(seed.Charge.Id);
        var approvedSum = await _context.PaymentAllocations
            .Where(payment => payment.ChargeId == seed.Charge.Id && payment.Status == PaymentStatus.Approved)
            .SumAsync(payment => payment.Amount);

        Assert.Equal(400m, firstLineItem!.PaidAmount);
        Assert.Equal(0m, secondLineItem!.PaidAmount);
        Assert.Equal(400m, approvedSum);
        Assert.Equal(firstLineItem.PaidAmount + secondLineItem.PaidAmount, charge!.PaidAmount);
        Assert.Equal(approvedSum, charge.PaidAmount);
    }
}
