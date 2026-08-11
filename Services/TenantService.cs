using KiraTakip.Data;
using KiraTakip.Infrastructure.Exceptions;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TenantService(
    ITenantRepository tenantRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IDocumentService documentService) : ITenantService, ITransactionalService
{
    public Task<List<TenantListItemDto>> GetAllAsync(GetTenantsInput input)
        => tenantRepository.GetListAsync(
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public Task<PagedResult<TenantListItemDto>> GetPagedAsync(GetPagedTenantsInput input)
        => tenantRepository.GetPagedListAsync(
            input.Query,
            input.PropertyIds?.ToList(),
            input.UnitIds?.ToList());

    public Task<TenantDetailsDto?> GetDetailsAsync(GetTenantDetailsInput input)
        => tenantRepository.GetDetailsAsync(
            input.TenantId,
            input.AccessScope?.PropertyIds?.ToList(),
            input.AccessScope?.UnitIds?.ToList());

    public async Task<TenantDetailsDto> GetProfileAsync(GetTenantProfileInput input)
        => Guard.NotFound(
            await tenantRepository.GetDetailsAsync(input.TenantId),
            "Kiracı bulunamadı.",
            "Tenant.ProfileNotFound");

    public async Task<CreatedTenantDto> CreateAsync(CreateTenantInput input)
    {
        Guard.Forbidden(
            HasScopeRestriction(input.AccessScope),
            "Kapsamlı kullanıcılar bağımsız kiracı kaydı oluşturamaz.",
            "Tenant.CreateRequiresGlobalAccess");

        var tenantNo = input.TenantNo;
        if (string.IsNullOrWhiteSpace(tenantNo))
            tenantNo = await GenerateTenantNoAsync();
        else
            Guard.InvalidField(
                await tenantRepository.TenantNoExistsAsync(tenantNo),
                nameof(input.TenantNo),
                "Bu Kiracı No zaten kullanımda.",
                "Tenant.TenantNoConflict");

        Guard.InvalidField(
            !string.IsNullOrWhiteSpace(input.TaxNo)
                && await tenantRepository.TaxNoExistsAsync(input.TaxNo),
            nameof(input.TaxNo),
            "Bu Vergi No zaten kullanımda.",
            "Tenant.TaxNoConflict");

        await EnsureClassificationAsync(input.TenantCategoryId, input.SectorId);
        await EnsureRequiredDocumentsAsync(input.Documents);

        var tenant = new Tenant
        {
            TenantNo = tenantNo,
            Name = input.Name,
            TradeRegistryNo = input.TradeRegistryNo,
            TaxNo = input.TaxNo,
            TaxOffice = input.TaxOffice,
            MersisNo = input.MersisNo,
            Phone = input.Phone,
            Email = input.Email,
            Address = input.Address,
            TenantCategoryId = input.TenantCategoryId,
            SectorId = input.SectorId,
            RegistrationDate = DateTime.Now
        };

        await tenantRepository.AddAsync(tenant);
        await unitOfWork.SaveChangesAsync();

        foreach (var document in input.Documents)
        {
            await documentService.UploadAsync(new UploadDocumentInput(
                DocumentOwnerType.Tenant,
                tenant.Id,
                document.DocumentTypeId,
                document.FileName,
                document.MimeType,
                document.Content));
        }

        return new CreatedTenantDto(tenant.Id, tenant.DisplayName);
    }

    public async Task UpdateAsync(UpdateTenantInput input)
    {
        var tenant = Guard.NotFound(
            await tenantRepository.GetForUpdateAsync(
                input.TenantId,
                input.AccessScope.PropertyIds?.ToList(),
                input.AccessScope.UnitIds?.ToList()),
            "Kiracı bulunamadı.",
            "Tenant.NotFound");

        Guard.InvalidField(
            await tenantRepository.TenantNoExistsAsync(input.TenantNo, input.TenantId),
            nameof(input.TenantNo),
            "Bu Kiracı No zaten kullanımda.",
            "Tenant.TenantNoConflict");
        Guard.InvalidField(
            !string.IsNullOrWhiteSpace(input.TaxNo)
                && await tenantRepository.TaxNoExistsAsync(input.TaxNo, input.TenantId),
            nameof(input.TaxNo),
            "Bu Vergi No zaten kullanımda.",
            "Tenant.TaxNoConflict");

        await EnsureClassificationAsync(input.TenantCategoryId, input.SectorId);

        tenant.TenantNo = input.TenantNo;
        tenant.TenantCategoryId = input.TenantCategoryId;
        tenant.SectorId = input.SectorId;
        tenant.Name = input.Name;
        tenant.TradeRegistryNo = input.TradeRegistryNo;
        tenant.TaxNo = input.TaxNo;
        tenant.TaxOffice = input.TaxOffice;
        tenant.MersisNo = input.MersisNo;
        tenant.Phone = input.Phone;
        tenant.Email = input.Email;
        tenant.Address = input.Address;

        await tenantRepository.UpdateAsync(tenant);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<string> GenerateTenantNoAsync()
    {
        var existingTenantNos = await tenantRepository.GetExistingTenantNosAsync();
        var usedTenantNos = existingTenantNos.ToHashSet();

        for (var index = 1; index <= 999999; index++)
        {
            var tenantNo = $"KRC-{index:D6}";
            if (!usedTenantNos.Contains(tenantNo)) return tenantNo;
        }

        throw new BusinessException(
            "Kiracı No üretilemedi.",
            ErrorType.Failure,
            "Tenant.NumberGenerationFailed");
    }

    public Task<bool> IsInactiveAsync(CheckTenantInactiveInput input, CancellationToken ct = default)
        => tenantRepository.IsInactiveAsync(input.TenantId, ct);

    private async Task EnsureClassificationAsync(int? tenantCategoryId, int? sectorId)
    {
        var tenantCategory = tenantCategoryId.HasValue
            ? await categoryRepository.GetByIdAndTipiAsync(tenantCategoryId.Value, CategoryType.Tenant)
            : null;
        Guard.InvalidField(
            tenantCategory == null || !tenantCategory.IsActive,
            nameof(CreateTenantInput.TenantCategoryId),
            "Geçerli ve aktif bir kiracı kategorisi seçilmelidir.",
            "Tenant.InvalidCategory");

        var sector = sectorId.HasValue
            ? await categoryRepository.GetByIdAndTipiAsync(sectorId.Value, CategoryType.Sector)
            : null;
        Guard.InvalidField(
            sector == null || !sector.IsActive,
            nameof(CreateTenantInput.SectorId),
            "Geçerli ve aktif bir sektör seçilmelidir.",
            "Tenant.InvalidSector");
    }

    private async Task EnsureRequiredDocumentsAsync(
        IReadOnlyList<TenantDocumentUploadInput> documents)
    {
        var requiredTypes = await documentService.GetTypesAsync(
            new GetDocumentTypesInput(DocumentOwnerType.Tenant, RequiredOnly: true));
        var uploadedTypeIds = documents.Select(document => document.DocumentTypeId).ToHashSet();

        foreach (var documentType in requiredTypes)
        {
            Guard.InvalidField(
                !uploadedTypeIds.Contains(documentType.Id),
                $"dosya_{documentType.Id}",
                $"'{documentType.Name}' belgesi zorunludur.",
                "Tenant.RequiredDocument");
        }
    }

    private static bool HasScopeRestriction(TenantAccessScopeInput accessScope)
        => accessScope.PropertyIds != null || accessScope.UnitIds != null;
}
