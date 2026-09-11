using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Models.Constants;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
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
using KiraTakip.Models.Dtos.Charge;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class PaymentArchitectureTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IDbContextTransaction _transaction;

    public PaymentArchitectureTests(DatabaseFixture fixture)
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
        Property Property,
        Unit Unit,
        Tenant Tenant,
        Lease Lease,
        Charge Charge,
        ApplicationUser User,
        ChargeType ChargeType,
        ChargeLineItem LineItem,
        int StoreAccountId)> SeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var property = new Property
        {
            Name = $"Ödeme Plaza {suffix}",
            City = "İstanbul",
            District = "Kadıköy"
        };
        var unitType = new UnitType
        {
            Name = $"Ödeme Ofis {suffix}",
            Code = $"PAY_{suffix}",
            Usage = UnitTypeUsage.Rentable
        };
        var tenant = new Tenant
        {
            Name = $"Ödeme Kiracı {suffix}",
            TenantNo = $"PAY-{suffix}"
        };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"payment-{suffix}@test.local",
            NormalizedUserName = $"PAYMENT-{suffix}@TEST.LOCAL",
            Email = $"payment-{suffix}@test.local",
            NormalizedEmail = $"PAYMENT-{suffix}@TEST.LOCAL",
            EmailConfirmed = true,
            IsActive = true
        };
        var chargeType = new ChargeType
        {
            Name = $"Ödeme Borç Tipi {suffix}",
            Code = $"PAYCT_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.UserManual
        };
        var store = new Store
        {
            Name = $"Ödeme Mağazası {suffix}",
            Code = $"PAYSTORE_{suffix}",
            IsActive = true
        };
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

        _context.Properties.Add(property);
        _context.UnitTypes.Add(unitType);
        _context.Tenants.Add(tenant);
        _context.Users.Add(user);
        _context.ChargeTypes.Add(chargeType);
        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        var unit = new Unit
        {
            PropertyId = property.Id,
            UnitTypeId = unitType.Id,
            Name = $"Ofis {suffix}",
            Area = 50
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        var lease = new Lease
        {
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            Status = LeaseStatus.Active
        };
        _context.Leases.Add(lease);
        await _context.SaveChangesAsync();

        var charge = new Charge
        {
            TenantId = tenant.Id,
            UnitId = unit.Id,
            LeaseId = lease.Id,
            PeriodStart = new DateTime(2026, 1, 1),
            PeriodEnd = new DateTime(2026, 1, 31),
            DueDate = new DateTime(2026, 2, 5),
            ExpectedAmount = 1000m,
            TotalAmount = 1000m,
            PaidAmount = 0m,
            Status = ChargeStatus.Pending
        };
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();

        var lineItem = new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = chargeType.Id,
            Description = "Test kalemi",
            Amount = 1000m,
            TotalAmount = 1000m
        };
        _context.ChargeLineItems.Add(lineItem);

        _context.PaymentStoreRoutings.Add(new PaymentStoreRouting
        {
            ChargeTypeId = chargeType.Id,
            PropertyId = null,
            UnitId = null,
            StoreId = store.Id,
            IsActive = true
        });
        await _context.SaveChangesAsync();

        return (property, unit, tenant, lease, charge, user, chargeType, lineItem, store.Accounts.Single().Id);
    }

    [Fact]
    public async Task Create_SingleLineItemCharge_BindsLineItemAndResolvedStoreAccount()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        var accessScope = new PaymentAccessScopeInput([], [seed.Unit.Id]);

        var paymentId = await service.CreateAsync(new CreatePaymentInput(
            seed.Charge.Id,
            new DateTime(2026, 2, 1),
            400m,
            PaymentChannel.Eft,
            PaymentSourceType.Manual,
            "Test ödeme",
            seed.User.Id,
            accessScope,
            null));

        var payment = await _context.PaymentAllocations.FindAsync(paymentId);
        Assert.NotNull(payment);
        Assert.Equal(seed.Lease.Id, payment.LeaseId);
        Assert.Equal(seed.Charge.Id, payment.ChargeId);
        Assert.Equal(seed.LineItem.Id, payment.ChargeLineItemId);
        Assert.Equal(seed.StoreAccountId, payment.StoreAccountId);

        var details = await service.GetByIdAsync(new GetPaymentByIdInput(paymentId, accessScope));
        Assert.NotNull(details);
        var inaccessibleDetails = await service.GetByIdAsync(
            new GetPaymentByIdInput(paymentId, new PaymentAccessScopeInput([], [])));
        Assert.Null(inaccessibleDetails);
    }

    [Fact]
    public async Task Create_OutOfScopeCharge_IsForbidden()
    {
        var seed = await SeedAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().CreateAsync(
            new CreatePaymentInput(
                seed.Charge.Id,
                new DateTime(2026, 2, 1),
                400m,
                PaymentChannel.Eft,
                PaymentSourceType.Manual,
                null,
                seed.User.Id,
                new PaymentAccessScopeInput([], []),
                null)));

        Assert.Equal("PAYMENT_CHARGE_FORBIDDEN", exception.Code);
    }

    [Fact]
    public async Task Create_AmountAboveLineItemAvailable_IsRejected()
    {
        var seed = await SeedAsync();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => CreateService().CreateAsync(
            new CreatePaymentInput(
                seed.Charge.Id,
                new DateTime(2026, 2, 1),
                1000.01m,
                PaymentChannel.Eft,
                PaymentSourceType.Manual,
                null,
                seed.User.Id,
                new PaymentAccessScopeInput(),
                null)));

        Assert.Equal("PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE", exception.Code);
    }

    [Fact]
    public async Task Create_AdminFlow_SubtractsPendingPaymentsFromAvailableAmount()
    {
        var seed = await SeedAsync();
        _context.PaymentAllocations.Add(new PaymentAllocation
        {
            ChargeId = seed.Charge.Id,
            ChargeLineItemId = seed.LineItem.Id,
            StoreAccountId = seed.StoreAccountId,
            LeaseId = seed.Lease.Id,
            CreatedByUserId = seed.User.Id,
            PaymentDate = new DateTime(2026, 2, 1),
            Amount = 700m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.PendingApproval
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() => CreateService().CreateAsync(
            new CreatePaymentInput(
                seed.Charge.Id,
                new DateTime(2026, 2, 2),
                400m,
                PaymentChannel.Eft,
                PaymentSourceType.Manual,
                null,
                seed.User.Id,
                new PaymentAccessScopeInput(),
                null)));

        Assert.Equal("PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE", exception.Code);
    }

    [Fact]
    public async Task Create_MultiLineItemChargeWithoutSelection_RequiresLineItem()
    {
        var seed = await SeedAsync();
        _context.ChargeLineItems.Add(new ChargeLineItem
        {
            ChargeId = seed.Charge.Id,
            ChargeTypeId = seed.ChargeType.Id,
            Description = "İkinci kalem",
            Amount = 500m,
            TotalAmount = 500m
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().CreateAsync(
            new CreatePaymentInput(
                seed.Charge.Id,
                new DateTime(2026, 2, 1),
                100m,
                PaymentChannel.Eft,
                PaymentSourceType.Manual,
                null,
                seed.User.Id,
                new PaymentAccessScopeInput(),
                null)));

        Assert.Equal("PAYMENT_LINE_ITEM_SELECTION_REQUIRED", exception.Code);
    }

    [Fact]
    public async Task Create_LineItemFromAnotherCharge_IsRejected()
    {
        var seed = await SeedAsync();
        var otherCharge = new Charge
        {
            TenantId = seed.Tenant.Id,
            UnitId = seed.Unit.Id,
            LeaseId = seed.Lease.Id,
            PeriodStart = new DateTime(2026, 3, 1),
            PeriodEnd = new DateTime(2026, 3, 31),
            DueDate = new DateTime(2026, 4, 5),
            ExpectedAmount = 500m,
            TotalAmount = 500m,
            PaidAmount = 0m,
            Status = ChargeStatus.Pending
        };
        _context.Charges.Add(otherCharge);
        await _context.SaveChangesAsync();
        var otherLineItem = new ChargeLineItem
        {
            ChargeId = otherCharge.Id,
            ChargeTypeId = seed.ChargeType.Id,
            Description = "Diğer tahakkuk kalemi",
            Amount = 500m,
            TotalAmount = 500m
        };
        _context.ChargeLineItems.Add(otherLineItem);
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().CreateAsync(
            new CreatePaymentInput(
                seed.Charge.Id,
                new DateTime(2026, 2, 1),
                100m,
                PaymentChannel.Eft,
                PaymentSourceType.Manual,
                null,
                seed.User.Id,
                new PaymentAccessScopeInput(),
                ChargeLineItemId: otherLineItem.Id)));

        Assert.Equal("PAYMENT_LINE_ITEM_CHARGE_MISMATCH", exception.Code);
    }

    [Fact]
    public async Task Create_WhenRoutingMissing_BlocksPaymentAndWritesNoAllocation()
    {
        var seed = await SeedAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var chargeTypeNoRouting = new ChargeType
        {
            Name = $"Yönlendirmesiz {suffix}",
            Code = $"NR_{suffix}",
            IsActive = true,
            Behavior = ChargeTypeBehavior.UserManual
        };
        _context.ChargeTypes.Add(chargeTypeNoRouting);
        await _context.SaveChangesAsync();

        var charge = new Charge
        {
            TenantId = seed.Tenant.Id,
            UnitId = seed.Unit.Id,
            LeaseId = seed.Lease.Id,
            PeriodStart = new DateTime(2026, 4, 1),
            PeriodEnd = new DateTime(2026, 4, 30),
            DueDate = new DateTime(2026, 5, 5),
            ExpectedAmount = 300m,
            TotalAmount = 300m,
            PaidAmount = 0m,
            Status = ChargeStatus.Pending
        };
        _context.Charges.Add(charge);
        await _context.SaveChangesAsync();
        _context.ChargeLineItems.Add(new ChargeLineItem
        {
            ChargeId = charge.Id,
            ChargeTypeId = chargeTypeNoRouting.Id,
            Description = "Yönlendirmesiz kalem",
            Amount = 300m,
            TotalAmount = 300m
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().CreateAsync(
            new CreatePaymentInput(
                charge.Id,
                new DateTime(2026, 4, 2),
                100m,
                PaymentChannel.Eft,
                PaymentSourceType.Manual,
                null,
                seed.User.Id,
                new PaymentAccessScopeInput(),
                null)));

        Assert.Equal("PAYMENT_ROUTING_NOT_FOUND", exception.Code);
        Assert.False(await _context.PaymentAllocations.AnyAsync(payment => payment.ChargeId == charge.Id));
    }

    [Fact]
    public async Task Create_SnapshotsResolvedStoreAccountAndIgnoresLaterRoutingChange()
    {
        var seed = await SeedAsync();
        var service = CreateService();

        var firstPaymentId = await service.CreateAsync(new CreatePaymentInput(
            seed.Charge.Id,
            new DateTime(2026, 2, 1),
            100m,
            PaymentChannel.Eft,
            PaymentSourceType.Manual,
            null,
            seed.User.Id,
            new PaymentAccessScopeInput(),
            null));

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var storeB = new Store { Name = $"Mağaza B {suffix}", Code = $"STOREB_{suffix}", IsActive = true };
        storeB.Accounts.Add(new StoreAccount
        {
            ProviderCode = PaymentProviderCodes.Paratika,
            Currency = CurrencyCodes.Try,
            MerchantId = $"MERCHANT-B-{suffix}",
            MerchantUser = "b-user",
            ProtectedMerchantPassword = "protected",
            ValidFrom = DateTime.UtcNow,
            IsActive = true
        });
        _context.Stores.Add(storeB);
        await _context.SaveChangesAsync();
        var storeAccountBId = storeB.Accounts.Single().Id;

        var routing = await _context.PaymentStoreRoutings.SingleAsync(item =>
            item.ChargeTypeId == seed.ChargeType.Id && item.PropertyId == null && item.UnitId == null);
        routing.StoreId = storeB.Id;
        await _context.SaveChangesAsync();

        var secondPaymentId = await service.CreateAsync(new CreatePaymentInput(
            seed.Charge.Id,
            new DateTime(2026, 2, 2),
            100m,
            PaymentChannel.Eft,
            PaymentSourceType.Manual,
            null,
            seed.User.Id,
            new PaymentAccessScopeInput(),
            null));

        var firstPayment = await _context.PaymentAllocations.FindAsync(firstPaymentId);
        var secondPayment = await _context.PaymentAllocations.FindAsync(secondPaymentId);
        Assert.Equal(seed.StoreAccountId, firstPayment!.StoreAccountId);
        Assert.Equal(storeAccountBId, secondPayment!.StoreAccountId);
    }

    [Fact]
    public async Task Approve_RechecksCurrentLineItemApprovedTotal()
    {
        var seed = await SeedAsync();
        var approvedPayment = new PaymentAllocation
        {
            ChargeId = seed.Charge.Id,
            ChargeLineItemId = seed.LineItem.Id,
            StoreAccountId = seed.StoreAccountId,
            LeaseId = seed.Lease.Id,
            CreatedByUserId = seed.User.Id,
            PaymentDate = new DateTime(2026, 2, 1),
            Amount = 700m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.Approved
        };
        var pendingPayment = new PaymentAllocation
        {
            ChargeId = seed.Charge.Id,
            ChargeLineItemId = seed.LineItem.Id,
            StoreAccountId = seed.StoreAccountId,
            LeaseId = seed.Lease.Id,
            CreatedByUserId = seed.User.Id,
            PaymentDate = new DateTime(2026, 2, 2),
            Amount = 400m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.PendingApproval
        };
        _context.PaymentAllocations.AddRange(approvedPayment, pendingPayment);
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => CreateService().ApproveAsync(
            new ApprovePaymentInput(
                pendingPayment.Id,
                seed.User.Id,
                new PaymentAccessScopeInput())));

        Assert.Equal("PAYMENT_APPROVAL_EXCEEDS_LINE_ITEM_REMAINING", exception.Code);
        Assert.Equal(PaymentStatus.PendingApproval, pendingPayment.Status);
    }

    [Fact]
    public async Task Approve_UpdatesChargeAndCannotRunTwice()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        var paymentId = await service.CreateAsync(new CreatePaymentInput(
            seed.Charge.Id,
            new DateTime(2026, 2, 1),
            400m,
            PaymentChannel.Eft,
            PaymentSourceType.Manual,
            null,
            seed.User.Id,
            new PaymentAccessScopeInput(),
            null));

        var input = new ApprovePaymentInput(
            paymentId,
            seed.User.Id,
            new PaymentAccessScopeInput());
        await service.ApproveAsync(input);

        var charge = await _context.Charges.FindAsync(seed.Charge.Id);
        Assert.Equal(400m, charge!.PaidAmount);
        Assert.Equal(ChargeStatus.PartiallyPaid, charge.Status);
        var lineItem = await _context.ChargeLineItems.FindAsync(seed.LineItem.Id);
        Assert.Equal(400m, lineItem!.PaidAmount);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.ApproveAsync(input));
        Assert.Equal("PAYMENT_NOT_PENDING", exception.Code);
    }

    [Fact]
    public async Task Reject_OnlyPendingPaymentCanBeRejected()
    {
        var seed = await SeedAsync();
        var service = CreateService();
        var paymentId = await service.CreateAsync(new CreatePaymentInput(
            seed.Charge.Id,
            new DateTime(2026, 2, 1),
            250m,
            PaymentChannel.BankTransfer,
            PaymentSourceType.Manual,
            null,
            seed.User.Id,
            new PaymentAccessScopeInput(),
            null));

        var input = new RejectPaymentInput(
            paymentId,
            "Dekont doğrulanamadı.",
            new PaymentAccessScopeInput());
        await service.RejectAsync(input);

        var payment = await _context.PaymentAllocations.FindAsync(paymentId);
        Assert.Equal(PaymentStatus.Rejected, payment!.Status);
        Assert.Equal("Dekont doğrulanamadı.", payment.RejectionReason);
        var lineItem = await _context.ChargeLineItems.FindAsync(seed.LineItem.Id);
        Assert.Equal(0m, lineItem!.PaidAmount);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.RejectAsync(input));
        Assert.Equal("PAYMENT_NOT_PENDING", exception.Code);
    }

    [Fact]
    public async Task TenantReport_ApprovedAndPendingAmountsLimitNewReport()
    {
        var seed = await SeedAsync();
        _context.PaymentAllocations.Add(new PaymentAllocation
        {
            ChargeId = seed.Charge.Id,
            ChargeLineItemId = seed.LineItem.Id,
            StoreAccountId = seed.StoreAccountId,
            LeaseId = seed.Lease.Id,
            CreatedByUserId = seed.User.Id,
            PaymentDate = new DateTime(2026, 2, 1),
            Amount = 700m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.PendingApproval
        });
        await _context.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            CreateService().ReportTenantPaymentAsync(new ReportTenantPaymentInput(
                seed.Tenant.Id,
                seed.Charge.Id,
                new DateTime(2026, 2, 2),
                300.01m,
                PaymentChannel.BankTransfer,
                null,
                seed.User.Id,
                "dekont.pdf",
                "application/pdf",
                [1, 2, 3],
                new PaymentAccessScopeInput(),
                null)));

        Assert.Equal("TENANT_PAYMENT_AMOUNT_EXCEEDS_AVAILABLE", exception.Code);
    }

    [Fact]
    public async Task TenantReport_CreatesPaymentAndReceiptTogether()
    {
        var seed = await SeedAsync();

        await CreateService().ReportTenantPaymentAsync(new ReportTenantPaymentInput(
            seed.Tenant.Id,
            seed.Charge.Id,
            new DateTime(2026, 2, 2),
            400m,
            PaymentChannel.BankTransfer,
            "Test ödeme bildirimi",
            seed.User.Id,
            "dekont.pdf",
            "application/pdf",
            [1, 2, 3],
            new PaymentAccessScopeInput(),
            null));

        var payment = await _context.PaymentAllocations
            .OrderByDescending(item => item.Id)
            .FirstAsync(item => item.ChargeId == seed.Charge.Id);
        var document = await _context.Belgeler
            .Include(item => item.Content)
            .SingleAsync(item => item.OwnerType == DocumentOwnerType.Payment
                && item.OwnerId == payment.Id);

        Assert.Equal(PaymentStatus.PendingApproval, payment.Status);
        Assert.Equal(seed.LineItem.Id, payment.ChargeLineItemId);
        Assert.Equal(seed.StoreAccountId, payment.StoreAccountId);
        Assert.Equal("dekont.pdf", document.FileName);
        Assert.Equal(new byte[] { 1, 2, 3 }, document.Content!.Content);
    }

    [Fact]
    public async Task TenantChargeList_IsTenantScopedAndCalculatesOverdueDynamically()
    {
        var seed = await SeedAsync();
        _context.PaymentAllocations.Add(new PaymentAllocation
        {
            ChargeId = seed.Charge.Id,
            ChargeLineItemId = seed.LineItem.Id,
            StoreAccountId = seed.StoreAccountId,
            LeaseId = seed.Lease.Id,
            CreatedByUserId = seed.User.Id,
            PaymentDate = new DateTime(2026, 2, 1),
            Amount = 250m,
            PaymentChannel = PaymentChannel.Eft,
            Status = PaymentStatus.PendingApproval
        });
        await _context.SaveChangesAsync();

        var repository = new ChargeRepository(_context);
        var query = new TenantChargeQueryInput(1, 10, null, "gecikti", null, null, null);
        var result = await repository.GetTenantPagedListAsync(
            new GetTenantChargeIndexInput(seed.Tenant.Id, new DateTime(2026, 3, 1), query));
        var otherTenantResult = await repository.GetTenantPagedListAsync(
            new GetTenantChargeIndexInput(seed.Tenant.Id + 1, new DateTime(2026, 3, 1), query));

        var directUnitScopeResult = await repository.GetTenantPagedListAsync(
            new GetTenantChargeIndexInput(
                seed.Tenant.Id,
                new DateTime(2026, 3, 1),
                query,
                [],
                [seed.Unit.Id]));
        var outsideScopeResult = await repository.GetTenantPagedListAsync(
            new GetTenantChargeIndexInput(
                seed.Tenant.Id,
                new DateTime(2026, 3, 1),
                query,
                [],
                []));

        var item = Assert.Single(result.Items);
        Assert.Equal(ChargeStatus.Overdue, item.Status);
        Assert.Equal(250m, item.PendingPaymentAmount);
        Assert.Empty(otherTenantResult.Items);
        Assert.Empty(outsideScopeResult.Items);
        Assert.Single(directUnitScopeResult.Items);
        Assert.Equal(ChargeStatus.Pending, seed.Charge.Status);
    }
}
