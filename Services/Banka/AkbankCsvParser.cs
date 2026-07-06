using System.Globalization;
using System.Text;
using KiraTakip.Models;

namespace KiraTakip.Services.Banka;

public class AkbankCsvParser : IBankaHareketiParser
{
    public string BankCode => "AKBANK";

    // Beklenen kolon sırası: Tarih;Açıklama;Borç;Alacak;Bakiye;KarşıHesap;KarşıUnvan
    // Alacak sütunundaki değerler pozitif tutar (gelen para).
    // Borç sütunundaki değerler negatif tutar (giden para) olarak kaydedilir.
    public IEnumerable<BankTransaction> Parse(Stream csv)
    {
        using var reader = new StreamReader(csv, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        string? headerLine = reader.ReadLine();
        if (headerLine == null) yield break;

        var headers = headerLine.Split(';').Select(h => h.Trim().ToLowerInvariant()).ToArray();

        int idxTarih      = FindIndex(headers, "tarih", "işlem tarihi", "islem tarihi");
        int idxAciklama   = FindIndex(headers, "açıklama", "aciklama", "işlem açıklaması");
        int idxBorc       = FindIndex(headers, "borç", "borc");
        int idxAlacak     = FindIndex(headers, "alacak");
        int idxGonderenIban = FindIndex(headers, "karşı hesap no", "karsi hesap", "karşı hesap");
        int idxGonderenBilgi = FindIndex(headers, "karşı hesap adı", "karsi unvan", "karşı unvan");

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(';');
            if (cols.Length < 2) continue;

            var tarih = ParseTarih(Get(cols, idxTarih));
            if (tarih == null) continue;

            var alacak = ParseDecimal(Get(cols, idxAlacak));
            var borc   = ParseDecimal(Get(cols, idxBorc));
            var tutar  = alacak > 0 ? alacak : -borc;

            yield return new BankTransaction
            {
                TransactionDate     = tarih.Value,
                TransactionAmount     = tutar,
                Description        = Get(cols, idxAciklama),
                SenderIban    = Get(cols, idxGonderenIban) is { Length: > 0 } gi ? gi : null,
                SenderInfo = Get(cols, idxGonderenBilgi) is { Length: > 0 } gb ? gb : null,
                BankCode       = BankCode,
                MatchStatus   = BankMatchStatus.Unmatched,
            };
        }
    }

    private static int FindIndex(string[] headers, params string[] candidates)
    {
        foreach (var c in candidates)
        {
            var idx = Array.IndexOf(headers, c);
            if (idx >= 0) return idx;
        }
        return -1;
    }

    private static string Get(string[] cols, int idx)
        => idx >= 0 && idx < cols.Length ? cols[idx].Trim().Trim('"') : string.Empty;

    private static DateTime? ParseTarih(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateTime.TryParseExact(s, ["dd.MM.yyyy", "yyyy-MM-dd", "dd/MM/yyyy"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        return null;
    }

    private static decimal ParseDecimal(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        s = s.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }
}
