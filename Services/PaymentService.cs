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

    public async Task<PaymentAllocation> EkleAsync(PaymentAllocation payment)
    {
        payment.EntryDate = DateTime.Now;
        payment.Status = PaymentStatus.PendingApproval;
        await _repo.AddAsync(payment);
        await _uow.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> OnaylaAsync(int id, string onaylayanUserId)
    {
        var payment = await _repo.GetByIdAsync(id);
        if (payment == null || payment.Status != PaymentStatus.PendingApproval) return false;

        payment.Status = PaymentStatus.Approved;
        payment.ApprovedByUserId = onaylayanUserId;
        payment.ApprovalDate = DateTime.Now;
        await _repo.UpdateAsync(payment);
        await _uow.SaveChangesAsync();

        await _chargeService.OdenenTutarGuncelleAsync(payment.ChargeId);
        return true;
    }

    public async Task<bool> ReddetAsync(int id, string neden)
    {
        var payment = await _repo.GetByIdAsync(id);
        if (payment == null || payment.Status != PaymentStatus.PendingApproval) return false;

        payment.Status = PaymentStatus.Rejected;
        payment.RejectionReason = neden;
        await _repo.UpdateAsync(payment);
        await _uow.SaveChangesAsync();

        await _chargeService.OdenenTutarGuncelleAsync(payment.ChargeId);
        return true;
    }
}
