namespace KiraTakip.Models.ViewModels;

public class RateMatrixViewModel
{
    public int Year { get; set; }
    public bool IsActive { get; set; }
    public List<RateMatrixChargeTypeColumn> Columns { get; set; } = [];
    public List<RateMatrixRow> Rows { get; set; } = [];
    public List<RateMatrixReservationRow> ReservationRows { get; set; } = [];
}
