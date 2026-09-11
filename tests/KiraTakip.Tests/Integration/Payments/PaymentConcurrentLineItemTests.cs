using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
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
using KiraTakip.Models.Dtos.Payment;

namespace KiraTakip.Tests;

[Collection("Database collection")]
public class PaymentConcurrentLineItemTests(DatabaseFixture fixture)
{
    [Fact]
    public async Task ConcurrentApprovals_ShouldNotOverpaySingleLineItem()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        int chargeId;
        int lineItemId;
        int firstPaymentId;
        int secondPaymentId;
        int propertyId;
        int unitTypeId;
        int unitId;
        int tenantId;
        int chargeTypeId;
        int storeId;
        string actorId;

        await using (var setup = fixture.CreateContext())
        {
            var property = new Property { Name = $"Eşzamanlı Onay {suffix}" };
            var unitType = new UnitType
            {
                Name = $"Eşzamanlı Onay Birimi {suffix}",
                Code = $"CONC-A-{suffix}",
                Usage = UnitTypeUsage.Rentable
            };
            var tenant = new Tenant { TenantNo = $"CONC-A-{suffix}", Name = $"Eşzamanlı Onay Kiracı {suffix}" };
            var chargeType = new ChargeType
            {
                Name = $"Eşzamanlı Onay Borç Tipi {suffix}",
                Code = $"CONCA_{suffix}",
                IsActive = true,
                Behavior = ChargeTypeBehavior.UserManual
            };
            var store = new Store { Name = $"Eşzamanlı Onay Mağaza {suffix}", Code = $"CONCASTORE_{suffix}", IsActive = true };
            store.Accounts.Add(new StoreAccount
            {
                ProviderCode = PaymentProviderCodes.Paratika,
                Currency = CurrencyCodes.Try,
                MerchantId = $"MERCHANT-A-{suffix}",
                MerchantUser = "test-user",
                ProtectedMerchantPassword = "protected",
                ValidFrom = DateTime.UtcNow,
                IsActive = true
            });
            setup.AddRange(property, unitType, tenant, chargeType, store);
            await setup.SaveChangesAsync();

            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitTypeId = unitType.Id,
                Name = $"Eşzamanlı Onay Ofis {suffix}",
                Area = 40m
            };
            setup.Units.Add(unit);
            await setup.SaveChangesAsync();

            var charge = new Charge
            {
                TenantId = tenant.Id,
                UnitId = unit.Id,
                PeriodStart = new DateTime(2026, 1, 1),
                PeriodEnd = new DateTime(2026, 1, 31),
                DueDate = new DateTime(2026, 2, 5),
                ExpectedAmount = 1000m,
                TotalAmount = 1000m,
                PaidAmount = 0m,
                Status = ChargeStatus.Pending
            };
            setup.Charges.Add(charge);
            await setup.SaveChangesAsync();

            var lineItem = new ChargeLineItem
            {
                ChargeId = charge.Id,
                ChargeTypeId = chargeType.Id,
                Description = "Eşzamanlı onay kalemi",
                Amount = 1000m,
                TotalAmount = 1000m
            };
            setup.ChargeLineItems.Add(lineItem);
            setup.PaymentStoreRoutings.Add(new PaymentStoreRouting
            {
                ChargeTypeId = chargeType.Id,
                PropertyId = null,
                UnitId = null,
                StoreId = store.Id,
                IsActive = true
            });
            await setup.SaveChangesAsync();

            actorId = await setup.Users.Select(user => user.Id).FirstAsync();
            var storeAccountId = store.Accounts.Single().Id;
            var firstPayment = new PaymentAllocation
            {
                ChargeId = charge.Id,
                ChargeLineItemId = lineItem.Id,
                StoreAccountId = storeAccountId,
                CreatedByUserId = actorId,
                PaymentDate = DateTime.Today,
                Amount = 600m,
                PaymentChannel = PaymentChannel.Eft,
                Status = PaymentStatus.PendingApproval
            };
            var secondPayment = new PaymentAllocation
            {
                ChargeId = charge.Id,
                ChargeLineItemId = lineItem.Id,
                StoreAccountId = storeAccountId,
                CreatedByUserId = actorId,
                PaymentDate = DateTime.Today,
                Amount = 600m,
                PaymentChannel = PaymentChannel.Eft,
                Status = PaymentStatus.PendingApproval
            };
            setup.PaymentAllocations.AddRange(firstPayment, secondPayment);
            await setup.SaveChangesAsync();

            chargeId = charge.Id;
            lineItemId = lineItem.Id;
            firstPaymentId = firstPayment.Id;
            secondPaymentId = secondPayment.Id;
            propertyId = property.Id;
            unitTypeId = unitType.Id;
            unitId = unit.Id;
            tenantId = tenant.Id;
            chargeTypeId = chargeType.Id;
            storeId = store.Id;
        }

        try
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstTask = ApproveAsync(firstPaymentId, ready.Task);
            var secondTask = ApproveAsync(secondPaymentId, ready.Task);
            ready.SetResult();
            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.Single(results, result => result == "approved");
            Assert.Single(results, result => result == "PAYMENT_APPROVAL_EXCEEDS_LINE_ITEM_REMAINING");

            await using var verify = fixture.CreateContext();
            var lineItem = await verify.ChargeLineItems.SingleAsync(item => item.Id == lineItemId);
            var approvedSum = await verify.PaymentAllocations
                .Where(payment => payment.ChargeId == chargeId && payment.Status == PaymentStatus.Approved)
                .SumAsync(payment => payment.Amount);
            Assert.Equal(600m, lineItem.PaidAmount);
            Assert.Equal(600m, approvedSum);
        }
        finally
        {
            await using var cleanup = fixture.CreateContext();
            cleanup.PaymentAllocations.RemoveRange(
                cleanup.PaymentAllocations.Where(payment => payment.ChargeId == chargeId));
            await cleanup.SaveChangesAsync();
            cleanup.ChargeLineItems.RemoveRange(
                cleanup.ChargeLineItems.Where(item => item.ChargeId == chargeId));
            await cleanup.SaveChangesAsync();
            cleanup.Charges.Remove(await cleanup.Charges.SingleAsync(c => c.Id == chargeId));
            await cleanup.SaveChangesAsync();
            cleanup.PaymentStoreRoutings.RemoveRange(
                cleanup.PaymentStoreRoutings.Where(routing => routing.ChargeTypeId == chargeTypeId));
            await cleanup.SaveChangesAsync();
            cleanup.StoreAccounts.RemoveRange(cleanup.StoreAccounts.Where(a => a.StoreId == storeId));
            await cleanup.SaveChangesAsync();
            cleanup.Stores.Remove(await cleanup.Stores.SingleAsync(s => s.Id == storeId));
            cleanup.ChargeTypes.Remove(await cleanup.ChargeTypes.SingleAsync(ct => ct.Id == chargeTypeId));
            cleanup.Units.Remove(await cleanup.Units.SingleAsync(u => u.Id == unitId));
            await cleanup.SaveChangesAsync();
            cleanup.Tenants.Remove(await cleanup.Tenants.SingleAsync(t => t.Id == tenantId));
            cleanup.UnitTypes.Remove(await cleanup.UnitTypes.SingleAsync(ut => ut.Id == unitTypeId));
            cleanup.Properties.Remove(await cleanup.Properties.SingleAsync(p => p.Id == propertyId));
            await cleanup.SaveChangesAsync();
        }

        async Task<string> ApproveAsync(int paymentId, Task start)
        {
            await start;
            await using var context = fixture.CreateContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var service = CreatePaymentService(context);
            try
            {
                await service.ApproveAsync(new ApprovePaymentInput(
                    paymentId,
                    actorId,
                    new PaymentAccessScopeInput()));
                await transaction.CommitAsync();
                return "approved";
            }
            catch (BusinessException exception)
            {
                await transaction.RollbackAsync();
                return exception.Code ?? "business-error";
            }
        }
    }

    [Fact]
    public async Task ConcurrentCreates_ShouldNotExceedAvailableAmountWithPendingPayments()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        int chargeId;
        int lineItemId;
        int propertyId;
        int unitTypeId;
        int unitId;
        int tenantId;
        int chargeTypeId;
        int storeId;
        string actorId;

        await using (var setup = fixture.CreateContext())
        {
            var property = new Property { Name = $"Eşzamanlı Oluşturma {suffix}" };
            var unitType = new UnitType
            {
                Name = $"Eşzamanlı Oluşturma Birimi {suffix}",
                Code = $"CONC-B-{suffix}",
                Usage = UnitTypeUsage.Rentable
            };
            var tenant = new Tenant { TenantNo = $"CONC-B-{suffix}", Name = $"Eşzamanlı Oluşturma Kiracı {suffix}" };
            var chargeType = new ChargeType
            {
                Name = $"Eşzamanlı Oluşturma Borç Tipi {suffix}",
                Code = $"CONCB_{suffix}",
                IsActive = true,
                Behavior = ChargeTypeBehavior.UserManual
            };
            var store = new Store { Name = $"Eşzamanlı Oluşturma Mağaza {suffix}", Code = $"CONCBSTORE_{suffix}", IsActive = true };
            store.Accounts.Add(new StoreAccount
            {
                ProviderCode = PaymentProviderCodes.Paratika,
                Currency = CurrencyCodes.Try,
                MerchantId = $"MERCHANT-B-{suffix}",
                MerchantUser = "test-user",
                ProtectedMerchantPassword = "protected",
                ValidFrom = DateTime.UtcNow,
                IsActive = true
            });
            setup.AddRange(property, unitType, tenant, chargeType, store);
            await setup.SaveChangesAsync();

            var unit = new Unit
            {
                PropertyId = property.Id,
                UnitTypeId = unitType.Id,
                Name = $"Eşzamanlı Oluşturma Ofis {suffix}",
                Area = 40m
            };
            setup.Units.Add(unit);
            await setup.SaveChangesAsync();

            var charge = new Charge
            {
                TenantId = tenant.Id,
                UnitId = unit.Id,
                PeriodStart = new DateTime(2026, 1, 1),
                PeriodEnd = new DateTime(2026, 1, 31),
                DueDate = new DateTime(2026, 2, 5),
                ExpectedAmount = 1000m,
                TotalAmount = 1000m,
                PaidAmount = 0m,
                Status = ChargeStatus.Pending
            };
            setup.Charges.Add(charge);
            await setup.SaveChangesAsync();

            var lineItem = new ChargeLineItem
            {
                ChargeId = charge.Id,
                ChargeTypeId = chargeType.Id,
                Description = "Eşzamanlı oluşturma kalemi",
                Amount = 1000m,
                TotalAmount = 1000m
            };
            setup.ChargeLineItems.Add(lineItem);
            setup.PaymentStoreRoutings.Add(new PaymentStoreRouting
            {
                ChargeTypeId = chargeType.Id,
                PropertyId = null,
                UnitId = null,
                StoreId = store.Id,
                IsActive = true
            });
            await setup.SaveChangesAsync();

            actorId = await setup.Users.Select(user => user.Id).FirstAsync();
            chargeId = charge.Id;
            lineItemId = lineItem.Id;
            propertyId = property.Id;
            unitTypeId = unitType.Id;
            unitId = unit.Id;
            tenantId = tenant.Id;
            chargeTypeId = chargeType.Id;
            storeId = store.Id;
        }

        try
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstTask = CreateAsync(ready.Task);
            var secondTask = CreateAsync(ready.Task);
            ready.SetResult();
            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.Single(results, result => result == "created");
            Assert.Single(results, result => result == "PAYMENT_AMOUNT_EXCEEDS_LINE_ITEM_AVAILABLE");

            await using var verify = fixture.CreateContext();
            var totalAmount = await verify.PaymentAllocations
                .Where(payment => payment.ChargeId == chargeId)
                .SumAsync(payment => payment.Amount);
            Assert.Equal(600m, totalAmount);
        }
        finally
        {
            await using var cleanup = fixture.CreateContext();
            cleanup.PaymentAllocations.RemoveRange(
                cleanup.PaymentAllocations.Where(payment => payment.ChargeId == chargeId));
            await cleanup.SaveChangesAsync();
            cleanup.ChargeLineItems.RemoveRange(
                cleanup.ChargeLineItems.Where(item => item.ChargeId == chargeId));
            await cleanup.SaveChangesAsync();
            cleanup.Charges.Remove(await cleanup.Charges.SingleAsync(c => c.Id == chargeId));
            await cleanup.SaveChangesAsync();
            cleanup.PaymentStoreRoutings.RemoveRange(
                cleanup.PaymentStoreRoutings.Where(routing => routing.ChargeTypeId == chargeTypeId));
            await cleanup.SaveChangesAsync();
            cleanup.StoreAccounts.RemoveRange(cleanup.StoreAccounts.Where(a => a.StoreId == storeId));
            await cleanup.SaveChangesAsync();
            cleanup.Stores.Remove(await cleanup.Stores.SingleAsync(s => s.Id == storeId));
            cleanup.ChargeTypes.Remove(await cleanup.ChargeTypes.SingleAsync(ct => ct.Id == chargeTypeId));
            cleanup.Units.Remove(await cleanup.Units.SingleAsync(u => u.Id == unitId));
            await cleanup.SaveChangesAsync();
            cleanup.Tenants.Remove(await cleanup.Tenants.SingleAsync(t => t.Id == tenantId));
            cleanup.UnitTypes.Remove(await cleanup.UnitTypes.SingleAsync(ut => ut.Id == unitTypeId));
            cleanup.Properties.Remove(await cleanup.Properties.SingleAsync(p => p.Id == propertyId));
            await cleanup.SaveChangesAsync();
        }

        async Task<string> CreateAsync(Task start)
        {
            await start;
            await using var context = fixture.CreateContext();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var service = CreatePaymentService(context);
            try
            {
                await service.CreateAsync(new CreatePaymentInput(
                    chargeId,
                    DateTime.Today,
                    600m,
                    PaymentChannel.Eft,
                    PaymentSourceType.Manual,
                    null,
                    actorId,
                    new PaymentAccessScopeInput(),
                    ChargeLineItemId: lineItemId));
                await transaction.CommitAsync();
                return "created";
            }
            catch (BusinessException exception)
            {
                await transaction.RollbackAsync();
                return exception.Code ?? "business-error";
            }
        }
    }

    private static PaymentService CreatePaymentService(ApplicationDbContext context)
    {
        var unitOfWork = new UnitOfWork(context);
        var paymentRepository = new PaymentAllocationRepository(context);
        var chargeLineItemRepository = new ChargeLineItemRepository(context);
        var chargeService = new ChargeService(
            new ChargeRepository(context),
            new PaymentAllocationRepository(context),
            new UnitRepository(context),
            chargeLineItemRepository,
            unitOfWork);
        var documentService = new DocumentService(
            new DocumentRepository(context),
            new DocumentContentRepository(context),
            new DocumentTypeRepository(context),
            new TenantRepository(context),
            new LeaseRepository(context),
            paymentRepository,
            unitOfWork);
        var storeResolver = new PaymentStoreResolver(new PaymentStoreRoutingRepository(context));

        return new PaymentService(
            paymentRepository,
            unitOfWork,
            chargeService,
            documentService,
            chargeLineItemRepository,
            storeResolver,
            new PaymentBusinessRules());
    }
}
