using System.ComponentModel.DataAnnotations.Schema;

namespace KiraTakip.Models.Entities;

[Table("KullaniciYetkileri")]
public class UserPermission : BaseEntity
{
    [Column("UserId")]
    public string UserId { get; set; } = string.Empty;

    [Column("Permission")]
    public string Permission { get; set; } = string.Empty;
}
