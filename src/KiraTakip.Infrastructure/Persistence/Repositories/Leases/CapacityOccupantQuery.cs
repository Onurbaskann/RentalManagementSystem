using System.Linq.Expressions;
using KiraTakip.Data;
using KiraTakip.Domain.Leases;
using KiraTakip.Models.Entities;
using KiraTakip.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories.Leases;

internal static class CapacityOccupantQuery
{
    internal static readonly LeaseStatus[] OpenStatuses =
        [LeaseStatus.Active, LeaseStatus.Draft, LeaseStatus.RevisionRequested];

    internal static IQueryable<Lease> OpenForUnit(ApplicationDbContext ctx, int unitId)
        => ctx.Leases.IgnoreQueryFilters().AsNoTracking()
            .Where(lease => lease.UnitId == unitId
                && !lease.IsDeleted
                && OpenStatuses.Contains(lease.Status));

    internal static readonly Expression<Func<Lease, CapacityOccupant>> Projection =
        lease => new CapacityOccupant(
            lease.StartDate,
            lease.EndDate,
            lease.LeaseArea != null ? lease.LeaseArea.AllocatedArea : 0m,
            lease.Id,
            lease.LeaseArea != null ? (decimal?)lease.LeaseArea.RentedArea : null,
            lease.LeaseArea != null ? (LeaseAreaMode?)lease.LeaseArea.AllocationMode : null);
}
