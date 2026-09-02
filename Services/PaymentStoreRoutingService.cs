using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos.PaymentStoreRouting;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class PaymentStoreRoutingService(
    IPaymentStoreRoutingRepository routingRepository,
    IChargeTypeRepository chargeTypeRepository,
    IPropertyRepository propertyRepository,
    IUnitRepository unitRepository,
    IStoreRepository storeRepository,
    IPaymentStoreRoutingBusinessRules businessRules,
    IUnitOfWork unitOfWork) : IPaymentStoreRoutingService, ITransactionalService
{
    public async Task<PaymentStoreRoutingIndexDataDto> GetManagementDataAsync(TableQuery query)
    {
        var chargeTypes = await chargeTypeRepository.GetListAsync();
        var properties = await propertyRepository.GetListAsync(null);
        var units = await unitRepository.GetAllOptionsAsync(null);

        return new PaymentStoreRoutingIndexDataDto
        {
            Routings = await routingRepository.GetPagedListAsync(query),
            HistoryCount = await routingRepository.GetHistoryCountAsync(),
            MissingDefaults = await routingRepository.GetMissingDefaultsAsync(),
            ChargeTypes = chargeTypes
                .Select(item => new ChargeTypeRoutingOptionDto(item.Id, item.Name, item.IsActive))
                .ToList(),
            Properties = properties
                .OrderBy(item => item.Name)
                .Select(item => new PaymentStoreRoutingLookupDto(item.Id, item.Name))
                .ToList(),
            Units = units
                .Select(item => new PaymentStoreRoutingLookupDto(item.Id, item.Name, item.PropertyName))
                .ToList(),
            Stores = await storeRepository.GetRoutingOptionsAsync()
        };
    }

    public async Task UpsertAsync(UpsertPaymentStoreRoutingInput input)
    {
        await businessRules.EnsureUpsertAllowedAsync(input);

        var propertyId = input.Scope == PaymentRoutingScope.Property ? input.PropertyId : null;
        var unitId = input.Scope == PaymentRoutingScope.Unit ? input.UnitId : null;
        var existing = await routingRepository.FindActiveAsync(input.ChargeTypeId, propertyId, unitId);
        if (existing != null)
        {
            existing.StoreId = input.StoreId;
        }
        else
        {
            await routingRepository.AddAsync(new PaymentStoreRouting
            {
                ChargeTypeId = input.ChargeTypeId,
                PropertyId = propertyId,
                UnitId = unitId,
                StoreId = input.StoreId,
                IsActive = true
            });
        }

        await SaveWithDuplicateConflictAsync();
    }

    public async Task DeactivateOverrideAsync(int id)
    {
        var routing = await businessRules.GetActiveOverrideAsync(id);
        routing.IsActive = false;
        await unitOfWork.SaveChangesAsync();
    }

    public Task<int?> GetDefaultStoreIdAsync(int chargeTypeId)
        => routingRepository.GetDefaultStoreIdAsync(chargeTypeId);

    public Task<bool> HasUsableDefaultAsync(int chargeTypeId)
        => routingRepository.HasUsableDefaultAsync(chargeTypeId);

    private async Task SaveWithDuplicateConflictAsync()
    {
        try
        {
            await unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new BusinessException(
                "Bu borç tipi ve kapsam için zaten aktif bir yönlendirme bulunuyor.",
                ErrorType.Conflict,
                "PAYMENT_ROUTING_DUPLICATE");
        }
    }
}
