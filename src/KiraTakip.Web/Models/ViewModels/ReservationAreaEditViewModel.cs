namespace KiraTakip.Web.Models.ViewModels;

public class ReservationAreaEditViewModel
{
    public int? Id { get; set; }
    public string? UnitNo { get; set; }
    public string? Name { get; set; }
    public decimal Area { get; set; }
    public int? UnitTypeId { get; set; }
    public string? Description { get; set; }
    public bool HasActiveReservation { get; set; }
}
