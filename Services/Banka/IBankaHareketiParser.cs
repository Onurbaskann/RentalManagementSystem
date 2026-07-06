namespace KiraTakip.Services.Banka;

public interface IBankaHareketiParser
{
    string BankCode { get; }
    IEnumerable<BankTransaction> Parse(Stream csv);
}
