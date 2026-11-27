using KiraTakip.Models.Common;
using KiraTakip.Data;
using KiraTakip.Models.Dtos;
using KiraTakip.Models.Enums;
using KiraTakip.Repositories.Common;
using KiraTakip.Repositories.Interfaces.Reservations;
using Microsoft.EntityFrameworkCore;
using KiraTakip.Models.Dtos.Reservation;

namespace KiraTakip.Repositories.Reservations;

public class ReservationRepository(ApplicationDbContext ctx) : RepositoryBase<Reservation>(ctx), IReservationRepository
{
    public async Task<List<ReservationListItemDto>> GetListAsync(
        SpaceAccessScope scope)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        query = ApplyScope(query, scope);

        return await ProjectList(query).ToListAsync();
    }

    public async Task<PagedResult<ReservationListItemDto>> GetPagedListAsync(
        TableQuery tableQuery,
        SpaceAccessScope scope)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

        query = ApplyScope(query, scope);

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(reservation =>
                EF.Functions.Like(reservation.Space.Name, $"%{search}%")
                || EF.Functions.Like(reservation.Tenant.Name, $"%{search}%")
                || EF.Functions.Like(reservation.ReservationNo, $"%{search}%"));
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
        SpaceAccessScope scope)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(reservation => reservation.Status == ReservationStatus.Cancelled);

        query = ApplyScope(query, scope);

        return query.CountAsync();
    }

    public async Task<List<ReservationListItemDto>> GetTenantListAsync(
        int tenantId,
        SpaceAccessScope scope)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(reservation => reservation.TenantId == tenantId);

        query = ApplyScope(query, scope);

        return await ProjectList(query).ToListAsync();
    }

    public async Task<PagedResult<ReservationListItemDto>> GetTenantPagedListAsync(
        int tenantId,
        TableQuery tableQuery,
        SpaceAccessScope scope)
    {
        var query = _dbSet.AsNoTracking()
            .Where(reservation => reservation.TenantId == tenantId);

        query = ApplyScope(query, scope);

        if (!string.IsNullOrWhiteSpace(tableQuery.Q))
        {
            var search = tableQuery.Q.Trim();
            query = query.Where(reservation =>
                EF.Functions.Like(reservation.Space.Name, $"%{search}%"));
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
                ReservationNo = reservation.ReservationNo,
                SpaceId = reservation.SpaceId,
                SpaceName = reservation.Space.Name,
                SpacePath = reservation.Space.Path,
                TenantId = reservation.TenantId,
                TenantDisplayName = reservation.Tenant.Name,
                ChargeId = _ctx.Charges
                    .Where(charge => charge.ReservationId == reservation.Id)
                    .Select(charge => (int?)charge.Id)
                    .FirstOrDefault(),
                ChargeNo = _ctx.Charges
                    .Where(charge => charge.ReservationId == reservation.Id)
                    .Select(charge => charge.ChargeNo)
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
                ReservationNo = r.ReservationNo,
                SpaceId = r.SpaceId,
                SpaceName = r.Space.Name,
                SpacePath = r.Space.Path,
                TenantId = r.TenantId,
                TenantDisplayName = r.Tenant.Name,
                ChargeId = _ctx.Charges.Where(t => t.ReservationId == r.Id).Select(t => (int?)t.Id).FirstOrDefault(),
                ChargeNo = _ctx.Charges.Where(t => t.ReservationId == r.Id).Select(t => t.ChargeNo).FirstOrDefault(),
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
            .Include(reservation => reservation.Space)
            .Include(reservation => reservation.Attendees)
            .FirstOrDefaultAsync(reservation => reservation.Id == id);

    public Task<int?> GetSpaceIdAsync(int reservationId)
        => _dbSet.AsNoTracking()
            .Where(reservation => reservation.Id == reservationId)
            .Select(reservation => (int?)reservation.SpaceId)
            .FirstOrDefaultAsync();

    public Task AcquireSpaceDecisionLockAsync(int spaceId)
    {
        if (_ctx.Database.CurrentTransaction == null)
            throw new InvalidOperationException("Rezervasyon karar kilidi aktif transaction gerektirir.");

        var resource = $"KiraTakip.Reservation.Space.{spaceId}";
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
            .ThenBy(reservation => reservation.Space.Name)
            .Select(reservation => new ReservationCalendarItemDto(
                reservation.Id,
                reservation.SpaceId,
                reservation.Space.Name,
                reservation.Space.Path,
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
            .ThenBy(reservation => reservation.SpaceId)
            .Select(reservation => new TenantReservationCalendarItemDto(
                reservation.SpaceId,
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

        if (query.SpaceId.HasValue)
            reservations = reservations.Where(reservation => reservation.SpaceId == query.SpaceId.Value);

        return ApplyScope(reservations, query.Scope);
    }

    public async Task<bool> IsConflictAsync(
        int spaceId,
        DateTime startDate,
        DateTime endDate,
        int? excludedReservationId = null)
    {
        return await _dbSet.AnyAsync(r =>
            r.SpaceId == spaceId &&
            r.Status == ReservationStatus.Confirmed &&
            (!excludedReservationId.HasValue || r.Id != excludedReservationId.Value) &&
            r.StartDate < endDate &&
            r.EndDate > startDate);
    }

    public Task<ReservationSpaceContextDto?> GetSpaceContextAsync(int spaceId)
        => _ctx.Spaces.AsNoTracking()
            .Where(space => space.Id == spaceId)
            .Select(space => new ReservationSpaceContextDto(
                space.Id,
                space.Path,
                space.IsActive,
                space.Usage))
            .FirstOrDefaultAsync();

    public Task<List<ReservationSpaceOptionDto>> GetReservableSpacesAsync(SpaceAccessScope scope)
    {
        var roots = scope.RootPaths.ToList();
        return _ctx.Spaces.AsNoTracking()
            .Where(space => space.IsActive && space.Usage == SpaceUsage.Reservable)
            .Where(space => scope.HasGlobalAccess || roots.Any(path => space.Path.StartsWith(path)))
            .OrderBy(space => space.Path)
            .Select(space => new ReservationSpaceOptionDto(space.Id, space.Name, space.Path))
            .ToListAsync();
    }

    public Task<List<ReservationSpaceOptionDto>> GetActiveSpaceOptionsAsync()
        => _ctx.Spaces.AsNoTracking()
            .Where(space => space.IsActive)
            .OrderBy(space => space.Path)
            .Select(space => new ReservationSpaceOptionDto(space.Id, space.Name, space.Path))
            .ToListAsync();

    private static IQueryable<Reservation> ApplyScope(IQueryable<Reservation> query, SpaceAccessScope scope)
    {
        if (scope.HasGlobalAccess)
            return query;

        var roots = scope.RootPaths.ToList();
        return query.Where(reservation => roots.Any(path => reservation.Space.Path.StartsWith(path)));
    }

    public Task<List<string>> GetExistingReservationNosAsync()
        => _dbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(reservation => reservation.ReservationNo)
            .ToListAsync();
}
