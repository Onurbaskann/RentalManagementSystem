using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class PaymentService : IPaymentService, ITransactionalService
{
    private readonly IPaymentRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IChargeService _chargeService;

    public PaymentService(
        IPaymentRepository repo,
        IUnitOfWork uow,
        IChargeService tahakkukService)
    {
        _repo = repo;
        _uow = uow;
        _chargeService = tahakkukService;
    }

    public async Task<List<OdemeListItemDto>> GetAllAsync(int? tahakkukId = null, IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetListAsync(tahakkukId, tasinmazIds?.ToList());
    }

    public async Task<PagedResult<OdemeListItemDto>> GetPagedAsync(TableQuery q, int? tahakkukId = null, IReadOnlyList<int>? tasinmazIds = null)
    {
        return await _repo.GetPagedListAsync(q, tahakkukId, tasinmazIds?.ToList());
    }

    public async Task<OdemeDetayDto?> GetByIdAsync(int id)
    {
        return await _repo.GetDetayAsync(id);
    }

    public async Task<PaymentAllocation> EkleAsync(PaymentAllocation odeme)
    {
        odeme.EntryDate = DateTime.Now;
        odeme.Status = PaymentStatus.PendingApproval;
        await _repo.AddAsync(odeme);
        await _uow.SaveChangesAsync();
        return odeme;
    }

    public async Task<bool> OnaylaAsync(int id, string onaylayanUserId)
    {
        var odeme = await _repo.GetByIdAsync(id);
        if (odeme == null || odeme.Status != PaymentStatus.PendingApproval) return false;

        odeme.Status = PaymentStatus.Approved;
        odeme.ApprovedByUserId = onaylayanUserId;
        odeme.ApprovalDate = DateTime.Now;
        await _repo.UpdateAsync(odeme);
        await _uow.SaveChangesAsync();

        await _chargeService.OdenenTutarGuncelleAsync(odeme.ChargeId);
        return true;
    }

    public async Task<bool> ReddetAsync(int id, string neden)
    {
        var odeme = await _repo.GetByIdAsync(id);
        if (odeme == null || odeme.Status != PaymentStatus.PendingApproval) return false;

        odeme.Status = PaymentStatus.Rejected;
        odeme.RejectionReason = neden;
        await _repo.UpdateAsync(odeme);
        await _uow.SaveChangesAsync();

        await _chargeService.OdenenTutarGuncelleAsync(odeme.ChargeId);
        return true;
    }
}
