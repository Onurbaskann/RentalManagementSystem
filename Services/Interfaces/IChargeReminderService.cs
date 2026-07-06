namespace KiraTakip.Services.Interfaces;

public class BorcHatirlatmaSonucDto
{
    public int ToplamBorclu { get; set; }
    public int BasariliGonderim { get; set; }
    public int CooldownAtlanan { get; set; }
    public int BasarisizGonderim { get; set; }
}

public interface IChargeReminderService
{
    Task<BorcHatirlatmaSonucDto> GonderAsync(CancellationToken ct = default);
}
