namespace KiraTakip.Services.Banking;

public interface IBankaHareketiParser
{
    string BankCode { get; }
    IEnumerable<BankTransaction> Parse(Stream csv);
}
