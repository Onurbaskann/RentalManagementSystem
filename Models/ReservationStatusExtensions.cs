namespace KiraTakip.Models;

public static class ReservationStatusExtensions
{
    public static string GetDisplayLabel(this ReservationStatus status)
        => status switch
        {
            ReservationStatus.PendingApproval => "Onay Bekliyor",
            ReservationStatus.Confirmed => "Onaylandı",
            ReservationStatus.Rejected => "Reddedildi",
            ReservationStatus.Cancelled => "İptal Edildi",
            ReservationStatus.Completed => "Tamamlandı",
            _ => "Bilinmiyor"
        };

    public static string GetBadgeCssClass(this ReservationStatus status)
        => status switch
        {
            ReservationStatus.Confirmed => "badge-kurumsal",
            ReservationStatus.Completed => "badge-kirali",
            ReservationStatus.Rejected or ReservationStatus.Cancelled => "badge-gecmis",
            _ => "badge-bos"
        };
}
