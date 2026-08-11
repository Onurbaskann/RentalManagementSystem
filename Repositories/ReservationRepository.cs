using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Models.Dtos;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class ReservationRepository : RepositoryBase<Reservation>, IReservationRepository
{
    public ReservationRepository(ApplicationDbContext ctx) : base(ctx) { }

    public async Task<List<ReservationListItemDto>> GetListAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(reservation =>
                propertyIds.Contains(reservation.Unit.PropertyId)
                || unitIds.Contains(reservation.UnitId));
        }

        return await ProjectList(query).ToListAsync();
    }

    public async Task<PagedResult<ReservationListItemDto>> GetPagedListAsync(
        TableQuery tableQuery,
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(reservation =>
                propertyIds.Contains(reservation.Unit.PropertyId)
                || unitIds.Contains(reservation.UnitId));
        }

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(reservation =>
                EF.Functions.Like(reservation.Unit.Name, $"%{search}%")
                || EF.Functions.Like(reservation.Unit.Property.Name, $"%{search}%")
                || EF.Functions.Like(reservation.Tenant.Name, $"%{search}%"));
        }

        var currentTime = DateTime.Now;

        query = tableQuery.Status switch
        {
            "planlandi" => query.Where(reservation =>
                reservation.Status == ReservationStatus.Planned
                && reservation.EndDate > currentTime),
            "tamamlandi" => query.Where(reservation =>
                reservation.Status == ReservationStatus.Completed
                || (reservation.Status == ReservationStatus.Planned
                    && reservation.EndDate <= currentTime)),
            "aktarildi" => query.Where(reservation =>
                reservation.Status == ReservationStatus.TransferredToCharge),
            "iptal" => query.Where(reservation =>
                reservation.Status == ReservationStatus.Cancelled),
            _ => query
        };

        return await GetPagedResultAsync(
            query,
            ProjectList(query, currentTime),
            tableQuery);
    }

    public async Task<List<ReservationListItemDto>> GetTenantListAsync(
        int tenantId,
        DateTime currentTime,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(reservation => reservation.TenantId == tenantId);

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(reservation =>
                propertyIds.Contains(reservation.Unit.PropertyId)
                || unitIds.Contains(reservation.UnitId));
        }

        return await ProjectList(query, currentTime).ToListAsync();
    }

    public async Task<PagedResult<ReservationListItemDto>> GetTenantPagedListAsync(
        int tenantId,
        DateTime currentTime,
        TableQuery tableQuery,
        List<int>? authorizedPropertyIds = null,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(reservation => reservation.TenantId == tenantId);

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(reservation =>
                propertyIds.Contains(reservation.Unit.PropertyId)
                || unitIds.Contains(reservation.UnitId));
        }

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(reservation =>
                EF.Functions.Like(reservation.Unit.Name, $"%{search}%")
                || EF.Functions.Like(reservation.Unit.Property.Name, $"%{search}%"));
        }

        return await GetPagedResultAsync(
            query,
            ProjectList(query, currentTime),
            tableQuery);
    }

    private IQueryable<ReservationListItemDto> ProjectList(
        IQueryable<Reservation> query,
        DateTime? currentTime = null)
        => query
            .OrderByDescending(reservation => reservation.CreatedAt)
            .ThenByDescending(reservation => reservation.Id)
            .Select(reservation => new ReservationListItemDto
            {
                Id = reservation.Id,
                UnitId = reservation.UnitId,
                UnitName = reservation.Unit.Name,
                PropertyId = reservation.Unit.PropertyId,
                PropertyName = reservation.Unit.Property.Name,
                TenantId = reservation.TenantId,
                TenantDisplayName = reservation.Tenant.DisplayName,
                ChargeId = _ctx.Charges
                    .Where(charge => charge.ReservationId == reservation.Id)
                    .Select(charge => (int?)charge.Id)
                    .FirstOrDefault(),
                StartDate = reservation.StartDate,
                EndDate = reservation.EndDate,
                TotalDurationMinutes = reservation.TotalDurationMinutes,
                FreeDurationMinutes = reservation.FreeDurationMinutes,
                PaidDurationMinutes = reservation.PaidDurationMinutes,
                TotalAmount = reservation.TotalAmount,
                Status = currentTime.HasValue
                    && reservation.Status == ReservationStatus.Planned
                    && reservation.EndDate <= currentTime.Value
                        ? ReservationStatus.Completed
                        : reservation.Status,
                Description = reservation.Description
            });

    public async Task<ReservationListItemDto?> GetByIdAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ReservationListItemDto
            {
                Id = r.Id,
                UnitId = r.UnitId,
                UnitName = r.Unit.Name,
                PropertyId = r.Unit.PropertyId,
                PropertyName = r.Unit.Property.Name,
                TenantId = r.TenantId,
                TenantDisplayName = r.Tenant.DisplayName,
                ChargeId = _ctx.Charges.Where(t => t.ReservationId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                TotalDurationMinutes = r.TotalDurationMinutes,
                FreeDurationMinutes = r.FreeDurationMinutes,
                PaidDurationMinutes = r.PaidDurationMinutes,
                TotalAmount = r.TotalAmount,
                Status = r.Status,
                Description = r.Description
            })
            .FirstOrDefaultAsync();
    }

    public Task<Reservation?> GetForOperationAsync(int id)
        => _dbSet
            .Include(reservation => reservation.Unit)
                .ThenInclude(unit => unit.UnitType)
            .FirstOrDefaultAsync(reservation => reservation.Id == id);

    public async Task<bool> IsConflictAsync(int unitId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet.AnyAsync(r =>
            r.UnitId == unitId &&
            r.Status != ReservationStatus.Cancelled &&
            r.StartDate < endDate &&
            r.EndDate > startDate);
    }

    public Task<List<int>> GetActiveUnitIdsAsync(IReadOnlyCollection<int> unitIds, DateTime now)
        => _dbSet
            .Where(reservation => unitIds.Contains(reservation.UnitId)
                && reservation.Status == ReservationStatus.Planned
                && reservation.EndDate >= now)
            .Select(reservation => reservation.UnitId)
            .Distinct()
            .ToListAsync();

    public Task<bool> HasPlannedForUnitTypeAsync(int unitTypeId)
        => _dbSet.AsNoTracking().AnyAsync(reservation =>
            reservation.Status == ReservationStatus.Planned
            && _ctx.Units.Any(unit => unit.UnitTypeId == unitTypeId && unit.Id == reservation.UnitId));

    public Task<bool> ExistsForUnitAsync(int unitId)
        => _dbSet.AsNoTracking().AnyAsync(reservation => reservation.UnitId == unitId);

}
