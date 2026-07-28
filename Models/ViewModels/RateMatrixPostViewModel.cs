namespace KiraTakip.Models.ViewModels;

public class RateMatrixPostViewModel
{
    public int Year { get; set; }
    public bool IsActive { get; set; }
    public List<RateMatrixCell> Cells { get; set; } = [];
    public List<RateMatrixReservationRow> ReservationCells { get; set; } = [];
}
