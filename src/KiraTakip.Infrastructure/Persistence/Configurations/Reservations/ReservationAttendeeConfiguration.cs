using KiraTakip.Data.Configurations;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KiraTakip.Data.Configurations.Reservations;

internal sealed class ReservationAttendeeConfiguration : IEntityTypeConfiguration<ReservationAttendee>
{
    public void Configure(EntityTypeBuilder<ReservationAttendee> entity)
    {
        entity.Property(attendee => attendee.DisplayName).HasMaxLength(200);
        entity.Property(attendee => attendee.EmailAddress).HasMaxLength(256);
        entity.Property(attendee => attendee.NormalizedEmailAddress).HasMaxLength(256);
        entity.HasOne(attendee => attendee.Reservation)
              .WithMany(reservation => reservation.Attendees)
              .HasForeignKey(attendee => attendee.ReservationId)
              .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(attendee => new
              {
                  attendee.ReservationId,
                  attendee.NormalizedEmailAddress
              })
              .IsUnique()
              .HasDatabaseName("UX_RezervasyonKatilimcilari_RezervasyonEposta")
              .HasFilter("[IsDeleted] = 0");
    }
}
