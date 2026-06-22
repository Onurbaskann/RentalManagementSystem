using KiraTakip.Infrastructure;

namespace KiraTakip.Models.Entities;

public class Kiraci : BaseEntity
{
    public int? KiraciKategoriId { get; set; }
    public int? SektorId { get; set; }
    public string KiraciNo { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public string? TicaretSicilNo { get; set; }
    [AuditMask(MaskType.VergiNo)]
    public string? VergiNo { get; set; }
    public string? VergiDairesi { get; set; }
    public string? MersisNo { get; set; }
    [AuditMask(MaskType.Telefon)]
    public string Telefon { get; set; } = string.Empty;
    [AuditMask(MaskType.Email)]
    public string Email { get; set; } = string.Empty;
    public string? Adres { get; set; }
    public DateTime KayitTarihi { get; set; }

    public string GosterimAdi => Ad;

    public Kategori? KiraciKategori { get; set; }
    public Kategori? Sektor { get; set; }
}
