using KiraTakip.Data;
using KiraTakip.Infrastructure.Transactions;
using KiraTakip.Models;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class LeaseService : ILeaseService, ITransactionalService
{
    private readonly ILeaseRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IStatisticsService _statisticsService;

    public LeaseService(
        ILeaseRepository repo,
        IUnitOfWork uow,
        IStatisticsService statisticsService)
    {
        _repo = repo;
        _uow = uow;
        _statisticsService = statisticsService;
    }

    public async Task<List<LeaseListItemDto>> GetAllAsync(string? filter = null, IReadOnlyList<int>? propertyIds = null)
    {
        var authorizedPropertyIds = propertyIds?.ToList();
        var list = await _repo.GetListAsync(filter, authorizedPropertyIds);
        foreach (var item in list)
        {
            var lease = new Lease
            {
                Id = item.Id,
                TenantId = item.TenantId,
                UnitId = item.UnitId,
                Unit = new Unit { Id = item.UnitId, Area = item.UnitArea }
            };
            item.MonthlyAmount = await _statisticsService.AylikBedelAsync(lease);
        }
        return list;
    }

    public async Task<LeaseDetailDto?> GetByIdAsync(int id)
    {
        return await _repo.GetDetayAsync(id);
    }

    public async Task<Lease> CreateAsync(Lease lease, decimal? monthlyAmount = null)
    {
        lease.ActivityLog.Add(new LeaseActivityLog
        {
            ActivityType = LeaseActivityType.Creation,
            TransactionDate = DateTime.Now,
            Description = "Sözleşme oluşturuldu.",
            NewRentAmount = monthlyAmount
        });

        await _repo.AddAsync(lease);
        await _uow.SaveChangesAsync();
        return lease;
    }

    public async Task UzatAsync(int id, DateTime newEndDate, decimal oldAmount, decimal newAmount,
        bool isKdvApplied, decimal kdvRate, decimal? inflationRate, string? description)
    {
        var lease = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.ActivityLog))
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        var oldEndDate = lease.EndDate;

        lease.EndDate = newEndDate;
        lease.IsKdvApplied = isKdvApplied;

        decimal? kdvAmount = isKdvApplied ? newAmount * kdvRate / 100 : null;
        decimal? kdvIncludedAmount = isKdvApplied ? newAmount + kdvAmount : null;

        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = id,
            ActivityType = LeaseActivityType.Extension,
            TransactionDate = DateTime.Now,
            Description = description ?? "Sözleşme süresi uzatıldı.",
            OldEndDate = oldEndDate,
            NewEndDate = newEndDate,
            OldRentAmount = oldAmount,
            NewRentAmount = newAmount,
            InflationRate = inflationRate,
            IsKdvApplied = isKdvApplied,
            KdvRate = isKdvApplied ? kdvRate : null,
            KdvAmount = kdvAmount,
            KdvIncludedAmount = kdvIncludedAmount
        });

        await _uow.SaveChangesAsync();
    }

    public async Task FeshetAsync(int id, DateTime terminationDate, string terminationReason, string? description)
    {
        var lease = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.ActivityLog))
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        lease.Status = LeaseStatus.Terminated;
        lease.TerminationDate = terminationDate;
        lease.TerminationReason = terminationReason;

        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = id,
            ActivityType = LeaseActivityType.Termination,
            TransactionDate = DateTime.Now,
            Description = description ?? terminationReason
        });

        await _uow.SaveChangesAsync();
    }

    public async Task VadeGuncelleAsync(int id, DueDateRuleType ruleType, int dueDay, string? description)
    {
        if (dueDay < 1 || dueDay > 31)
            throw new ArgumentOutOfRangeException(nameof(dueDay), "Vade günü 1-31 arasında olmalıdır.");

        var lease = await _repo.GetByIdAsync(id, include: q => q.Include(x => x.ActivityLog))
            ?? throw new InvalidOperationException($"Sözleşme {id} bulunamadı.");

        var oldRuleType = lease.DueDateRuleType;
        var oldDueDay = lease.DueDay;

        if (oldRuleType == ruleType && oldDueDay == dueDay) return;

        lease.DueDateRuleType = ruleType;
        lease.DueDay = dueDay;

        lease.ActivityLog.Add(new LeaseActivityLog
        {
            LeaseId = id,
            ActivityType = LeaseActivityType.ChargeRegeneration,
            TransactionDate = DateTime.Now,
            Description = description ?? $"Vade kuralı güncellendi: {oldRuleType}({oldDueDay}) → {ruleType}({dueDay})"
        });

        await _uow.SaveChangesAsync();
    }

    public async Task<List<LeaseListItemDto>> GetByTenantIdAsync(int tenantId)
    {
        var list = await _repo.GetByTenantIdAsync(tenantId);
        foreach (var item in list)
        {
            var lease = new Lease
            {
                Id = item.Id,
                TenantId = item.TenantId,
                UnitId = item.UnitId,
                Unit = new Unit { Id = item.UnitId, Area = item.UnitArea }
            };
            item.MonthlyAmount = await _statisticsService.AylikBedelAsync(lease);
        }
        return list;
    }

    public async Task<List<LeaseListItemDto>> GetByUnitIdAsync(int unitId)
    {
        var list = await _repo.GetByUnitIdAsync(unitId);
        foreach (var item in list)
        {
            var lease = new Lease
            {
                Id = item.Id,
                TenantId = item.TenantId,
                UnitId = item.UnitId,
                Unit = new Unit { Id = item.UnitId, Area = item.UnitArea }
            };
            item.MonthlyAmount = await _statisticsService.AylikBedelAsync(lease);
        }
        return list;
    }

    public async Task<Dictionary<int, decimal?>> GetDepozitoTutarlariAsync(IEnumerable<int> leaseIds)
    {
        return await _repo.GetDepozitoTutarlariAsync(leaseIds);
    }
}
