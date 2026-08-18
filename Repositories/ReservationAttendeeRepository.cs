using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;

namespace KiraTakip.Repositories;

public class ReservationAttendeeRepository(ApplicationDbContext context)
    : RepositoryBase<ReservationAttendee>(context), IReservationAttendeeRepository
{
}
