using System.Globalization;
using System.Text;
using KiraTakip.Models;
using KiraTakip.Models.Entities;

namespace KiraTakip.Services.Banka;

public class AkbankCsvParser : IBankaHareketiParser
{
    public string BankaKodu => "AKBANK";

    // Beklenen kolon sırası: Tarih;Açıklama;Borç;Alacak;Bakiye;KarşıHesap;KarşıUnvan
    // Alacak sütunundaki değerler pozitif tutar (gelen para).
    // Borç sütunundaki değerler negatif tutar (giden para) olarak kaydedilir.
    public IEnumerable<BankaHareketi> Parse(Stream csv, Guid batchId, string userId)
    {
        using var reader = new StreamReader(csv, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        string? headerLine = reader.ReadLine();
        if (headerLine == null) yield break;

        var headers = headerLine.Split(';').Select(h => h.Trim().ToLowerInvariant()).ToArray();

        int idxTarih       = FindIndex(headers, "tarih", "işlem tarihi", "islem tarihi");
        int idxAciklama    = FindIndex(headers, "açıklama", "aciklama", "işlem açıklaması");
        int idxBorc        = FindIndex(headers, "borç", "borc");
        int idxAlacak      = FindIndex(headers, "alacak");
        int idxBakiye      = FindIndex(headers, "bakiye");
        int idxKarsiHesap  = FindIndex(headers, "karşı hesap no", "karsi hesap", "karşı hesap");
        int idxKarsiUnvan  = FindIndex(headers, "karşı hesap adı", "karsi unvan", "karşı unvan");

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

            yield return new BankaHareketi
            {
                ImportBatchId    = batchId,
                HareketTarihi    = tarih.Value,
                Tutar            = tutar,
                Aciklama         = Get(cols, idxAciklama),
                KarsiHesap       = Get(cols, idxKarsiHesap) is { Length: > 0 } kh ? kh : null,
                KarsiUnvan       = Get(cols, idxKarsiUnvan) is { Length: > 0 } ku ? ku : null,
                Bakiye           = idxBakiye >= 0 ? ParseDecimal(Get(cols, idxBakiye)) : null,
                BankaKodu        = BankaKodu,
                EslesmeDurumu    = BankaEslesmeDurumu.Eslestirilmedi,
                ImportTarihi     = DateTime.Now,
                ImportEdenUserId = userId
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
