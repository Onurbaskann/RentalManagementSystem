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

        query = tableQuery.Status switch
        {
            "planlandi" or "onaylandi" => query.Where(reservation =>
                reservation.Status == ReservationStatus.Confirmed),
            "onaybekliyor" => query.Where(reservation =>
                reservation.Status == ReservationStatus.PendingApproval),
            "tamamlandi" => query.Where(reservation =>
                reservation.Status == ReservationStatus.Completed),
            "reddedildi" => query.Where(reservation =>
                reservation.Status == ReservationStatus.Rejected),
            "iptal" => query.Where(reservation =>
                reservation.Status == ReservationStatus.Cancelled),
            _ => query.Where(reservation =>
                reservation.Status != ReservationStatus.Cancelled)
        };

        return await GetPagedResultAsync(
            query,
            ProjectList(query),
            tableQuery);
    }

    public Task<int> GetCancelledCountAsync(
        List<int>? authorizedPropertyIds,
        List<int>? authorizedUnitIds = null)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(reservation => reservation.Status == ReservationStatus.Cancelled);

        if (authorizedPropertyIds != null || authorizedUnitIds != null)
        {
            var propertyIds = authorizedPropertyIds ?? [];
            var unitIds = authorizedUnitIds ?? [];
            query = query.Where(reservation =>
                propertyIds.Contains(reservation.Unit.PropertyId)
                || unitIds.Contains(reservation.UnitId));
        }

        return query.CountAsync();
    }

    public async Task<List<ReservationListItemDto>> GetTenantListAsync(
        int tenantId,
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

        return await ProjectList(query).ToListAsync();
    }

    public async Task<PagedResult<ReservationListItemDto>> GetTenantPagedListAsync(
        int tenantId,
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
            ProjectList(query),
            tableQuery);
    }

    private IQueryable<ReservationListItemDto> ProjectList(
        IQueryable<Reservation> query)
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
                TenantDisplayName = reservation.Tenant.Name,
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
                Status = reservation.Status,
                Title = reservation.Title,
                Description = reservation.Description,
                Notes = reservation.Notes,
                InternalNotes = reservation.InternalNotes,
                CancellationReason = reservation.CancellationReason,
                RejectionReason = reservation.RejectionReason,
                RequestedByDisplayName = reservation.RequestedByDisplayNameSnapshot,
                RequestedByEmailAddress = reservation.RequestedByEmailSnapshot,
                ApprovedAt = reservation.ApprovedAt,
                RejectedAt = reservation.RejectedAt,
                RejectedByDisplayName = reservation.RejectedByUser == null
                    ? null
                    : reservation.RejectedByUser.AdSoyad ?? reservation.RejectedByUser.Email,
                RowVersion = reservation.RowVersion
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
                TenantDisplayName = r.Tenant.Name,
                ChargeId = _ctx.Charges.Where(t => t.ReservationId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                TotalDurationMinutes = r.TotalDurationMinutes,
                FreeDurationMinutes = r.FreeDurationMinutes,
                PaidDurationMinutes = r.PaidDurationMinutes,
                TotalAmount = r.TotalAmount,
                Status = r.Status,
                Title = r.Title,
                Description = r.Description,
                Notes = r.Notes,
                InternalNotes = r.InternalNotes,
                CancellationReason = r.CancellationReason,
                RejectionReason = r.RejectionReason,
                RequestedByDisplayName = r.RequestedByDisplayNameSnapshot,
                RequestedByEmailAddress = r.RequestedByEmailSnapshot,
                ApprovedAt = r.ApprovedAt,
                RejectedAt = r.RejectedAt,
                RejectedByDisplayName = r.RejectedByUser == null
                    ? null
                    : r.RejectedByUser.AdSoyad ?? r.RejectedByUser.Email,
                Attendees = r.Attendees
                    .OrderByDescending(attendee => attendee.IsReservationOwner)
                    .ThenBy(attendee => attendee.DisplayName)
                    .Select(attendee => new ReservationAttendeeDto(
                        attendee.DisplayName,
                        attendee.EmailAddress,
                        attendee.IsReservationOwner))
                    .ToList(),
                RowVersion = r.RowVersion
            })
            .FirstOrDefaultAsync();
    }

    public Task<Reservation?> GetForOperationAsync(int id)
        => _dbSet
            .Include(reservation => reservation.Unit)
                .ThenInclude(unit => unit.UnitType)
            .Include(reservation => reservation.Attendees)
            .FirstOrDefaultAsync(reservation => reservation.Id == id);

    public Task<int?> GetUnitIdAsync(int reservationId)
        => _dbSet.AsNoTracking()
            .Where(reservation => reservation.Id == reservationId)
            .Select(reservation => (int?)reservation.UnitId)
            .FirstOrDefaultAsync();

    public Task AcquireUnitDecisionLockAsync(int unitId)
    {
        if (_ctx.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Rezervasyon karar kilidi aktif transaction gerektirir.");

        var resource = $"KiraTakip.Reservation.Unit.{unitId}";
        return _ctx.Database.ExecuteSqlInterpolatedAsync($@"
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            IF @result < 0
                THROW 51000, 'Rezervasyon karar kilidi alınamadı.', 1;");
    }

    public Task<List<int>> GetCompletionCandidateIdsAsync(DateTime cutoff, int batchSize)
        => _dbSet.AsNoTracking()
            .Where(reservation =>
                reservation.Status == ReservationStatus.Confirmed
                && reservation.EndDate <= cutoff)
            .OrderBy(reservation => reservation.EndDate)
            .ThenBy(reservation => reservation.Id)
            .Select(reservation => reservation.Id)
            .Take(batchSize)
            .ToListAsync();

    public Task AcquireCompletionLockAsync(int reservationId)
    {
        if (_ctx.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Rezervasyon tamamlama kilidi aktif transaction gerektirir.");

        var resource = $"KiraTakip.Reservation.Completion.{reservationId}";
        return _ctx.Database.ExecuteSqlInterpolatedAsync($@"
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 10000;
            IF @result < 0
                THROW 51000, 'Rezervasyon tamamlama kilidi alınamadı.', 1;");
    }

    public Task<Reservation?> GetForCompletionAsync(int reservationId)
        => _dbSet.FirstOrDefaultAsync(reservation => reservation.Id == reservationId);

    public Task<List<ReservationCalendarItemDto>> GetCalendarItemsAsync(
        ReservationCalendarRepositoryQuery query)
    {
        var reservations = BuildCalendarQuery(query);

        return reservations
            .OrderBy(reservation => reservation.StartDate)
            .ThenBy(reservation => reservation.Unit.Name)
            .Select(reservation => new ReservationCalendarItemDto(
                reservation.Id,
                reservation.UnitId,
                reservation.Unit.Name,
                reservation.Unit.Property.Name,
                reservation.Title,
                reservation.Tenant.Name,
                reservation.StartDate,
                reservation.EndDate,
                reservation.Status))
            .ToListAsync();
    }

    public Task<List<TenantReservationCalendarItemDto>> GetTenantCalendarItemsAsync(
        int tenantId,
        ReservationCalendarRepositoryQuery query)
    {
        var reservations = BuildCalendarQuery(query);

        return reservations
            .OrderBy(reservation => reservation.StartDate)
            .ThenBy(reservation => reservation.UnitId)
            .Select(reservation => new TenantReservationCalendarItemDto(
                reservation.UnitId,
                reservation.StartDate,
                reservation.EndDate,
                reservation.Status,
                reservation.TenantId == tenantId))
            .ToListAsync();
    }

    private IQueryable<Reservation> BuildCalendarQuery(
        ReservationCalendarRepositoryQuery query)
    {
        var reservations = _dbSet
            .AsNoTracking()
            .Where(reservation =>
                (reservation.Status == ReservationStatus.Confirmed
                    || reservation.Status == ReservationStatus.PendingApproval
                    || reservation.Status == ReservationStatus.Completed)
                && reservation.StartDate < query.ToExclusive
                && reservation.EndDate > query.FromInclusive);

        if (query.UnitId.HasValue)
            reservations = reservations.Where(reservation => reservation.UnitId == query.UnitId.Value);

        if (query.PropertyIds != null || query.UnitIds != null)
        {
            var propertyIds = query.PropertyIds ?? [];
            var unitIds = query.UnitIds ?? [];
            reservations = reservations.Where(reservation =>
                propertyIds.Contains(reservation.Unit.PropertyId)
                || unitIds.Contains(reservation.UnitId));
        }

        return reservations;
    }

    public async Task<bool> IsConflictAsync(
        int unitId,
        DateTime startDate,
        DateTime endDate,
        int? excludedReservationId = null)
    {
        return await _dbSet.AnyAsync(r =>
            r.UnitId == unitId &&
            r.Status == ReservationStatus.Confirmed &&
            (!excludedReservationId.HasValue || r.Id != excludedReservationId.Value) &&
            r.StartDate < endDate &&
            r.EndDate > startDate);
    }

    public Task<List<int>> GetActiveUnitIdsAsync(IReadOnlyCollection<int> unitIds, DateTime now)
        => _dbSet
            .Where(reservation => unitIds.Contains(reservation.UnitId)
                && reservation.Status == ReservationStatus.Confirmed
                && reservation.EndDate >= now)
            .Select(reservation => reservation.UnitId)
            .Distinct()
            .ToListAsync();

    public Task<bool> HasConfirmedForUnitTypeAsync(int unitTypeId)
        => _dbSet.AsNoTracking().AnyAsync(reservation =>
            reservation.Status == ReservationStatus.Confirmed
            && _ctx.Units.Any(unit => unit.UnitTypeId == unitTypeId && unit.Id == reservation.UnitId));

    public Task<bool> ExistsForUnitAsync(int unitId)
        => _dbSet.AsNoTracking().AnyAsync(reservation => reservation.UnitId == unitId);

}
