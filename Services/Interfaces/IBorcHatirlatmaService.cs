using System.Threading;
using System.Threading.Tasks;

namespace KiraTakip.Services.Interfaces;

public class BorcHatirlatmaSonucDto
{
    public int ToplamBorclu { get; set; }
    public int BasariliGonderim { get; set; }
    public int CooldownAtlanan { get; set; }
    public int BasarisizGonderim { get; set; }
}

public interface IBorcHatirlatmaService
{
    Task<BorcHatirlatmaSonucDto> GonderAsync(CancellationToken ct = default);
}
