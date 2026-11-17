using KiraTakip.Models.Dtos.Lease;

namespace KiraTakip.Services.Interfaces.Leases;

public interface ILeaseGroupService
{
    Task<LeaseGroup> CreateGroupAsync(CreateLeaseGroupInput input);
    Task UpdateGroupAsync(UpdateLeaseGroupInput input);
    Task ResubmitGroupAsync(ResubmitLeaseGroupInput input);
    Task DeleteGroupDraftAsync(DeleteLeaseGroupDraftInput input);
    Task ApproveGroupAsync(ApproveLeaseGroupInput input);
    Task<LeaseGroupDetailDto?> GetDetailAsync(GetLeaseGroupInput input);
    Task<LeaseGroupLinkDto?> GetLinkByLeaseIdAsync(int leaseId);
}
