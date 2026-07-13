using System.ComponentModel.DataAnnotations.Schema;
using KiraTakip.Infrastructure;

namespace KiraTakip.Models.Entities;

[Table("Kiracilar")]
public class Tenant : BaseEntity
{
    [Column("KiraciKategoriId")]
    public int? TenantCategoryId { get; set; }

    [Column("SektorId")]
    public int? SectorId { get; set; }

    [Column("KiraciNo")]
    public string TenantNo { get; set; } = string.Empty;

    [Column("Ad")]
    public string Name { get; set; } = string.Empty;

    [Column("TicaretSicilNo")]
    public string? TradeRegistryNo { get; set; }

    [Column("VergiNo")]
    [AuditMask(MaskType.VergiNo)]
    public string? TaxNo { get; set; }

    [Column("VergiDairesi")]
    public string? TaxOffice { get; set; }

    [Column("MersisNo")]
    public string? MersisNo { get; set; }

    [Column("Telefon")]
    [AuditMask(MaskType.Telefon)]
    public string Phone { get; set; } = string.Empty;

    [Column("Email")]
    [AuditMask(MaskType.Email)]
    public string Email { get; set; } = string.Empty;

    [Column("Adres")]
    public string? Address { get; set; }

    [Column("KayitTarihi")]
    public DateTime RegistrationDate { get; set; }

    public string DisplayName => Name;

    public Category? TenantCategory { get; set; }
    public Category? Sector { get; set; }
}
