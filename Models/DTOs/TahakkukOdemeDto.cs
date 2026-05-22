namespace KiraTakip.Models.Dtos;

public class TahakkukOdemeDto
{
    public int Id { get; set; }
    public DateTime OdemeTarihi { get; set; }
    public decimal Tutar { get; set; }
    public OdemeKanali OdemeKanali { get; set; }
    public OdemeDurumu Durum { get; set; }
    public DateTime GirisTarihi { get; set; }
    public string? Aciklama { get; set; }
}
