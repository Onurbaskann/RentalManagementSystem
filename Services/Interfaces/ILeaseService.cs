using KiraTakip.Models;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface ILeaseService
{
    Task<List<LeaseListItemDto>> GetAllAsync(GetLeasesInput input);
    Task<LeaseDetailDto?> GetDetailsAsync(GetLeaseDetailsInput input);
    Task<LeaseDetailDto> GetTenantDetailsAsync(GetTenantLeaseDetailsInput input);
    Task<Lease> CreateAsync(CreateLeaseInput input);
    Task<Lease> CreateDraftAsync(CreateLeaseDraftInput input);
    Task<LeaseDraftEditDto?> GetDraftForEditAsync(GetLeaseDraftInput input);
    Task<IReadOnlyList<LeaseReviewHistoryDto>> GetReviewHistoryAsync(int leaseId);
    Task UpdateDraftAsync(UpdateLeaseDraftInput input);
    Task ResubmitRevisionAsync(ResubmitLeaseRevisionInput input);
    Task RequestRevisionAsync(RequestLeaseRevisionInput input);
    Task ApproveAsync(ApproveLeaseInput input);
    Task DeleteDraftAsync(DeleteLeaseDraftInput input);
    Task ExtendAsync(ExtendLeaseInput input);
    Task TerminateAsync(TerminateLeaseInput input);
    Task UpdateDueDateAsync(UpdateLeaseDueDateInput input);
    Task RegenerateAsync(RegenerateLeaseInput input);
    Task<IList<ChargeLineItemPreview>> GetDefaultLineItemsAsync(ComposeLeaseLineItemsInput input);
    Task<List<LeaseListItemDto>> GetByTenantAsync(GetLeasesByTenantInput input);
    Task<List<LeaseListItemDto>> GetTenantPortalLeasesAsync(GetTenantPortalLeasesInput input);
    Task<List<LeaseListItemDto>> GetByUnitAsync(GetLeasesByUnitInput input);
    Task<Dictionary<int, decimal?>> GetDepositsAsync(GetLeaseDepositsInput input);
}
