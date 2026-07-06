using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class ChargeService : IChargeService
{
    private readonly IChargeRepository _repo;
    private readonly IUnitOfWork _uow;
    public ChargeService(IChargeRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    // ── Listeleme ────────────────────────────────────────────────────────
    public async Task<List<ChargeListItemDto>> GetListAsync(int? leaseId = null, IReadOnlyList<int>? propertyIds = null, IReadOnlyList<int>? unitIds = null)
    {
        return await _repo.GetListAsync(leaseId, propertyIds?.ToList(), unitIds?.ToList());
    }

    public async Task<PagedResult<ChargeListItemDto>> GetPagedAsync(TableQuery q, int? leaseId = null, IReadOnlyList<int>? propertyIds = null, IReadOnlyList<int>? unitIds = null)
    {
        return await _repo.GetPagedListAsync(q, leaseId, propertyIds?.ToList(), unitIds?.ToList());
    }

    public Task<ChargeDetailDto?> GetDetailsAsync(int id) => _repo.GetDetailsAsync(id);

    // ── Business: Gecikme Güncelleme ─────────────────────────────────────
    public async Task UpdateDelaysAsync()
    {
        var gecikmisBekleyenler = await _repo.GetGeciktirileceklerAsync(DateTime.Today);
        if (gecikmisBekleyenler.Count == 0) return;

        foreach (var t in gecikmisBekleyenler)
        {
            t.Status = ChargeStatus.Overdue;
            await _repo.UpdateAsync(t);
        }

        await _uow.SaveChangesAsync();
    }

    // ── Business: Ödenen Amount Güncelleme ────────────────────────────────
    public async Task UpdatePaidAmountAsync(int chargeId)
    {
        var charge = await _repo.GetByIdAsync(chargeId);
        if (charge == null) return;

        var odenenTutar = await _repo.GetOdenenTutarAsync(chargeId);
        charge.PaidAmount = odenenTutar;

        charge.Status = odenenTutar >= charge.TotalAmount
            ? ChargeStatus.Paid
            : odenenTutar > 0
                ? ChargeStatus.PartiallyPaid
                : DateTime.Today > charge.DueDate
                    ? ChargeStatus.Overdue
                    : ChargeStatus.Pending;

        await _repo.UpdateAsync(charge);
        await _uow.SaveChangesAsync();
    }

}
