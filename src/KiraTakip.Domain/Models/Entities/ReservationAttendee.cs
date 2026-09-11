using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("RezervasyonKatilimcilari")]
public class ReservationAttendee : BaseEntity
{
    [Column("RezervasyonId")]
    public int ReservationId { get; set; }

    [Column("AdSoyad")]
    public string DisplayName { get; set; } = string.Empty;

    [Column("EpostaAdresi")]
    public string EmailAddress { get; set; } = string.Empty;

    [Column("NormalizeEpostaAdresi")]
    public string NormalizedEmailAddress { get; set; } = string.Empty;

    [Column("RezervasyonSahibiMi")]
    public bool IsReservationOwner { get; set; }

    public Reservation Reservation { get; set; } = null!;
}
