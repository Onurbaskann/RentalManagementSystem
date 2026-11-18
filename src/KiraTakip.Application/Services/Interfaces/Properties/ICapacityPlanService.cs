using KiraTakip.Models.Dtos.Lease;
using KiraTakip.Models.Dtos.Property;

namespace KiraTakip.Services.Interfaces.Properties;

public interface ICapacityPlanService
{
    Task<CapacityPlanPreviewDto> PreviewAsync(CapacityPlanInput input);
    Task ApplyAsync(CapacityPlanInput input, string expectedHash);
    Task<LeaseAllocatedAreaInfoDto?> GetAllocatedAreaInfoAsync(int leaseId, LeaseAccessScopeInput scope);
    Task UpdateAllocatedAreaAsync(UpdateAllocatedAreaInput input);
}
