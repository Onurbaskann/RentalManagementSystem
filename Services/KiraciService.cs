using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class KiraciService : IKiraciService
{
    private readonly IKiraciRepository _repo;
    private readonly IUnitOfWork _uow;
    public KiraciService(IKiraciRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<List<KiraciListItemDto>> GetAllAsync(IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetListAsync(tasinmazIds?.ToList());
    }

    public async Task<KiraciDetayDto?> GetDetayAsync(int id)
    {
        return await _repo.GetDetayAsync(id);
    }

    public async Task<Tenant> CreateAsync(Tenant k)
    {
        if (string.IsNullOrWhiteSpace(k.TenantNo))
            k.TenantNo = await GenerateKiraciNoAsync();
        k.RegistrationDate = DateTime.Now;
        await _repo.AddAsync(k);
        await _uow.SaveChangesAsync();
        return k;
    }

    public async Task UpdateAsync(Tenant k)
    {
        var dbKiraci = await _repo.GetByIdAsync(k.Id);
        if (dbKiraci == null) return;

        dbKiraci.TenantCategoryId = k.TenantCategoryId;
        dbKiraci.SectorId = k.SectorId;
        dbKiraci.Name = k.Name;
        dbKiraci.TradeRegistryNo = k.TradeRegistryNo;
        dbKiraci.TaxNo = k.TaxNo;
        dbKiraci.TaxOffice = k.TaxOffice;
        dbKiraci.MersisNo = k.MersisNo;
        dbKiraci.Phone = k.Phone;
        dbKiraci.Email = k.Email;
        dbKiraci.Address = k.Address;
        dbKiraci.IsActive = k.IsActive;

        await _repo.UpdateAsync(dbKiraci); // No-op marker
        await _uow.SaveChangesAsync();
    }

    public async Task<string> GenerateKiraciNoAsync()
    {
        var existing = await _repo.GetExistingKiraciNosAsync();
        var usedSet = existing.ToHashSet();
        for (int i = 1; i <= 999999; i++)
        {
            var no = $"KRC-{i:D6}";
            if (!usedSet.Contains(no)) return no;
        }
        throw new InvalidOperationException("KiraciNo üretilemedi.");
    }

    public async Task<bool> KiraciNoExistsAsync(string kiraciNo, int? excludeId = null)
    {
        return await _repo.AnyAsync(k =>
            k.TenantNo == kiraciNo && (excludeId == null || k.Id != excludeId));
    }
}
