namespace KiraTakip.Services.Banka;

public interface IBankaHareketiParser
{
    string BankaKodu { get; }
    IEnumerable<BankaHareketi> Parse(Stream csv);
}
