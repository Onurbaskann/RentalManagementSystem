using KiraTakip.Data;
using KiraTakip.Models;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class SeedDataService
{
    private readonly ApplicationDbContext _ctx;

    public SeedDataService(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task SeedDomainDataAsync()
    {
        if (await _ctx.Tasinmazlar.AnyAsync()) return;

        var now = DateTime.Now;
        var adminUser = await _ctx.Users.FirstOrDefaultAsync(u => u.Email == "admin@kiratakip.local");
        var adminId = adminUser?.Id ?? "";

        // --- Kiracılar ---
        var ahmet = Kiraci("KRC-000001", KiraciTuru.Gercek, "Ahmet", "Yılmaz",
            tcNo: "12345678901", telefon: "0532 111 2233", email: "ahmet@example.com", adres: "İzmir, Bornova");
        var ayse = Kiraci("KRC-000002", KiraciTuru.Gercek, "Ayşe", "Demir",
            tcNo: "98765432100", telefon: "0533 222 3344", email: "ayse@example.com", adres: "İzmir, Karşıyaka");
        var mehmet = Kiraci("KRC-000003", KiraciTuru.Gercek, "Mehmet", "Kaya",
            tcNo: "11122233344", telefon: "0541 333 4455", email: "mehmet@example.com", adres: "İzmir, Konak");
        var yildiz = Kiraci("KRC-000004", KiraciTuru.Tuzel, "Yıldız Yazılım A.Ş.",
            vergiNo: "1234567890", vergiDairesi: "Bornova VD", ticaretSicilNo: "İZM-12345",
            telefon: "0232 444 5566", email: "info@yildiz.com", adres: "İzmir, Bornova Teknokent");
        var anadolu = Kiraci("KRC-000005", KiraciTuru.Tuzel, "Anadolu Lojistik Ltd.",
            vergiNo: "9876543210", vergiDairesi: "Buca VD", ticaretSicilNo: "İZM-67890",
            telefon: "0232 555 6677", email: "info@anadolu.com", adres: "İzmir, Buca");
        var egeTarim = Kiraci("KRC-000006", KiraciTuru.Tuzel, "Ege Tarım Koop.",
            vergiNo: "5556667770", vergiDairesi: "Menemen VD", mersisNo: "0555666777000010",
            telefon: "0232 666 7788", email: "info@egetarim.com", adres: "İzmir, Menemen");
        var maviCafe = Kiraci("KRC-000007", KiraciTuru.Tuzel, "Mavi Cafe & Restoran",
            vergiNo: "3334445550", vergiDairesi: "Konak VD", ticaretSicilNo: "İZM-33444",
            telefon: "0232 777 8899", email: "info@mavicafe.com", adres: "İzmir, Alsancak");

        _ctx.Kiraciler.AddRange(ahmet, ayse, mehmet, yildiz, anadolu, egeTarim, maviCafe);

        // --- Taşınmazlar ---
        var teknokent = MakeTasinmaz("Teknokent A Blok", TasinmazTipi.Bina, KiralamaSekli.OfisBazli,
            "İzmir", "Bornova", "Ege Üniversitesi", "Ege Üniversitesi Teknokent Kampüsü", 500, 4500, 5,
            "Ofis bazlı kiralanabilir teknokent binası",
            new[] { ("101",1,55m,"Girişe yakın ofis"), ("102",1,65m,"Cephe görünümlü"),
                    ("201",2,80m,(string?)null), ("202",2,75m,(string?)null),
                    ("301",3,90m,(string?)null), ("302",3,70m,"Toplantı odalı"),
                    ("401",4,110m,(string?)null), ("501",5,120m,"Teras erişimi") });

        var camlık = MakeTasinmaz("Çamlık Kantini", TasinmazTipi.Bina, KiralamaSekli.TekParca,
            "İzmir", "Karşıyaka", "Çamlık", "Çamlık Mahallesi No: 45", 200, 350, null,
            "Bütün olarak kiralanan kantin binası");
        var tarla = MakeTasinmaz("Bornova Tarlası", TasinmazTipi.Tarla, KiralamaSekli.TekParca,
            "İzmir", "Bornova", "Doğanlar", "Doğanlar Köyü Mevkii", 12000, 0, null,
            "12.000 m² ekilebilir tarla alanı");
        var dukkan = MakeTasinmaz("Atatürk Cd. Dükkan", TasinmazTipi.Bina, KiralamaSekli.TekParca,
            "İzmir", "Konak", "Alsancak", "Atatürk Caddesi No: 112", 0, 180, null,
            "Alsancak'ta sokak cepheli dükkan");
        var sanayiB = MakeTasinmaz("Sanayi Sitesi B Blok", TasinmazTipi.Bina, KiralamaSekli.OfisBazli,
            "İzmir", "Kemalpaşa", "OSB", "Kemalpaşa OSB 5. Cadde", 300, 2700, 3,
            "3 katlı sanayi binası",
            new[] { ("101",1,180m,"Zemin kat depo bölümü"), ("201",2,220m,(string?)null), ("301",3,240m,"Yönetim katı") });
        var arazi = MakeTasinmaz("Menemen Arazi", TasinmazTipi.Arazi, KiralamaSekli.TekParca,
            "İzmir", "Menemen", "Görece", "Görece Köyü Arazi Parseli 412", 8500, 0, null,
            "8.500 m² imarsız arazi");
        var depo = MakeTasinmaz("Buca Deposu", TasinmazTipi.Depo, KiralamaSekli.TekParca,
            "İzmir", "Buca", "Sanayi", "Buca Sanayi Sitesi B-12", 150, 1200, null,
            "1.200 m² kapalı lojistik deposu");

        _ctx.Tasinmazlar.AddRange(teknokent, camlık, tarla, dukkan, sanayiB, arazi, depo);
        await _ctx.SaveChangesAsync();

        // --- Birim referansları ---
        var tkOfis101 = teknokent.Birimler.First(b => b.OfisNo == "101");
        var tkOfis102 = teknokent.Birimler.First(b => b.OfisNo == "102");
        var tkOfis201 = teknokent.Birimler.First(b => b.OfisNo == "201");
        var tkOfis302 = teknokent.Birimler.First(b => b.OfisNo == "302");
        var snOfis101 = sanayiB.Birimler.First(b => b.OfisNo == "101");
        var snOfis201 = sanayiB.Birimler.First(b => b.OfisNo == "201");
        var camlıkBirim = camlık.Birimler[0];
        var tarlaBirim = tarla.Birimler[0];
        var dukkanBirim = dukkan.Birimler[0];
        var depoBirim = depo.Birimler[0];

        // --- Sözleşmeler ---
        var sozlesmeler = new List<KiraSozlesmesi>
        {
            MakeSozlesme(tkOfis101, yildiz, now.AddMonths(-6), now.AddMonths(18), 15000, KiraPeriyodu.Aylik, 45000, kdv: true, kdvOrani: 20, "Teknokent Ofis 101 — Yıldız Yazılım"),
            MakeSozlesme(tkOfis102, ahmet, now.AddMonths(-5), now.AddDays(45), 7500, KiraPeriyodu.Aylik, 22500, kdv: false, notlar: "Teknokent Ofis 102 — Ahmet Yılmaz"),
            MakeSozlesme(tkOfis201, ayse, now.AddYears(-1), now.AddDays(12), 9000, KiraPeriyodu.Aylik, 27000, kdv: false, notlar: "Teknokent Ofis 201 — Ayşe Demir"),
            MakeSozlesme(tkOfis302, mehmet, now.AddMonths(-3), now.AddMonths(21), 8000, KiraPeriyodu.Aylik, 24000, kdv: false, notlar: "Teknokent Ofis 302 — Mehmet Kaya"),
            MakeSozlesme(snOfis101, egeTarim, now.AddMonths(-8), now.AddMonths(16), 120000, KiraPeriyodu.Yillik, 60000, kdv: true, kdvOrani: 20, "Sanayi B Ofis 101 — Ege Tarım"),
            MakeSozlesme(snOfis201, anadolu, now.AddYears(-1), now.AddDays(22), 144000, KiraPeriyodu.Yillik, 72000, kdv: true, kdvOrani: 20, "Sanayi B Ofis 201 — Anadolu Lojistik"),
            MakeSozlesme(camlıkBirim, maviCafe, now.AddMonths(-3), now.AddMonths(21), 8500, KiraPeriyodu.Aylik, 25500, kdv: true, kdvOrani: 20, "Çamlık Kantini — Mavi Cafe"),
            MakeSozlesme(dukkanBirim, ahmet, now.AddYears(-2), now.AddMonths(-3), 6000, KiraPeriyodu.Aylik, 18000, kdv: false, notlar: "Atatürk Cd. Dükkan — Ahmet Yılmaz"),
            MakeSozlesme(tarlaBirim, egeTarim, now.AddMonths(-2), now.AddMonths(22), 36000, KiraPeriyodu.Yillik, 36000, kdv: true, kdvOrani: 20, "Bornova Tarlası — Ege Tarım"),
            MakeSozlesme(depoBirim, anadolu, now.AddMonths(-8), now.AddMonths(16), 180000, KiraPeriyodu.Yillik, 90000, kdv: true, kdvOrani: 20, "Buca Deposu — Anadolu Lojistik"),
        };

        foreach (var s in sozlesmeler)
        {
            if (s.BitisTarihi < now)
                s.Durum = SozlesmeDurumu.SonaErdi;
        }

        _ctx.Sozlesmeler.AddRange(sozlesmeler);
        await _ctx.SaveChangesAsync();

        // --- 2. Ödeme Takip Verileri (Tahakkuk, Ödeme, Banka Hareketi) ---
        // 1. Bu Ay Beklenen Tahsilat
        var s1 = sozlesmeler[0];
        var t1 = new KiraTahakkuk
        {
            KiraSozlesmesiId = s1.Id,
            DonemBaslangic = new DateTime(now.Year, now.Month, 1),
            DonemBitis = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1),
            VadeTarihi = new DateTime(now.Year, now.Month, 5),
            BeklenenTutar = s1.KiraBedeli,
            KdvTutari = s1.KdvUygulanacakMi ? (s1.KiraBedeli * s1.KdvOrani / 100) : 0,
            ToplamTutar = s1.KiraBedeli + (s1.KdvUygulanacakMi ? (s1.KiraBedeli * s1.KdvOrani / 100) : 0),
            OdenenTutar = 0,
            Durum = TahakkukDurumu.Bekleniyor
        };

        // 2. Gecikmiş Tahakkuk
        var s2 = sozlesmeler[1];
        var t2 = new KiraTahakkuk
        {
            KiraSozlesmesiId = s2.Id,
            DonemBaslangic = now.AddMonths(-1),
            DonemBitis = now.AddDays(-1),
            VadeTarihi = now.AddMonths(-1).AddDays(5),
            BeklenenTutar = s2.KiraBedeli,
            ToplamTutar = s2.KiraBedeli,
            OdenenTutar = 0,
            Durum = TahakkukDurumu.Gecikti
        };

        // 3. Onay Bekleyen Ödeme
        var s3 = sozlesmeler[2];
        var t3 = new KiraTahakkuk
        {
            KiraSozlesmesiId = s3.Id,
            DonemBaslangic = now.AddMonths(-1),
            DonemBitis = now.AddDays(-1),
            VadeTarihi = now.AddMonths(-1).AddDays(5),
            BeklenenTutar = s3.KiraBedeli,
            ToplamTutar = s3.KiraBedeli,
            OdenenTutar = 0,
            Durum = TahakkukDurumu.Bekleniyor
        };

        _ctx.KiraTahakkuklar.AddRange(t1, t2, t3);
        await _ctx.SaveChangesAsync();

        var o1 = new KiraOdeme
        {
            KiraTahakkukId = t3.Id,
            KiraSozlesmesiId = s3.Id,
            OdemeTarihi = now.AddDays(-2),
            Tutar = t3.ToplamTutar,
            OdemeKanali = OdemeKanali.Havale,
            Aciklama = "Ayşe Demir kira ödemesi",
            Durum = OdemeDurumu.OnayBekliyor,
            GirenUserId = adminId
        };
        _ctx.KiraOdemeler.Add(o1);

        // 4. Eşleşmemiş Banka Hareketi
        var b1 = new BankaHareketi
        {
            ImportBatchId = Guid.NewGuid(),
            HareketTarihi = now.AddDays(-1),
            Tutar = 12000,
            Aciklama = "Gelen Havale: Mehmet Kaya Kira",
            KarsiUnvan = "MEHMET KAYA",
            BankaKodu = "AKB",
            EslesmeDurumu = BankaEslesmeDurumu.Eslestirilmedi,
            ImportEdenUserId = adminId
        };

        var b2 = new BankaHareketi
        {
            ImportBatchId = Guid.NewGuid(),
            HareketTarihi = now.AddDays(-3),
            Tutar = 8500,
            Aciklama = "MAVİ CAFE KİRA ÖDEMESİ",
            KarsiUnvan = "MAVİ CAFE LTD",
            BankaKodu = "AKB",
            EslesmeDurumu = BankaEslesmeDurumu.Eslestirilmedi,
            ImportEdenUserId = adminId
        };
        _ctx.BankaHareketleri.AddRange(b1, b2);

        await _ctx.SaveChangesAsync();
    }

    private static Kiraci Kiraci(string kiraciNo, KiraciTuru tur, string ad, string? soyad = null,
        string? tcNo = null, string? vergiNo = null, string? vergiDairesi = null,
        string? ticaretSicilNo = null, string? mersisNo = null,
        string telefon = "", string email = "", string? adres = null) => new()
    {
        KiraciNo = kiraciNo,
        KiraciTuru = tur,
        Ad = ad,
        Soyad = soyad,
        TcKimlikNo = tcNo,
        VergiNo = vergiNo,
        VergiDairesi = vergiDairesi,
        TicaretSicilNo = ticaretSicilNo,
        MersisNo = mersisNo,
        Telefon = telefon,
        Email = email,
        Adres = adres,
        KayitTarihi = DateTime.Now.AddMonths(-Random.Shared.Next(6, 36))
    };

    private static Tasinmaz MakeTasinmaz(string ad, TasinmazTipi tipi, KiralamaSekli sekli,
        string il, string ilce, string mahalle, string acikAdres,
        decimal acikM2, decimal kapaliM2, int? katSayisi, string? aciklama,
        IEnumerable<(string ofisNo, int katNo, decimal m2, string? ofisAciklama)>? ofisler = null)
    {
        var t = new Tasinmaz
        {
            Ad = ad, Tipi = tipi, KiralamaSekli = sekli,
            Il = il, Ilce = ilce, Mahalle = mahalle, AcikAdres = acikAdres,
            AcikYuzolcumu = acikM2, KapaliYuzolcumu = kapaliM2,
            KatSayisi = katSayisi, Aciklama = aciklama,
            KayitTarihi = DateTime.Now.AddMonths(-Random.Shared.Next(12, 60))
        };

        if (sekli == KiralamaSekli.OfisBazli && ofisler != null)
        {
            foreach (var (ofisNo, katNo, m2, ofisAciklama) in ofisler)
                t.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Ofis, OfisNo = ofisNo, KatNo = katNo,
                    Ad = $"Ofis {ofisNo}", Yuzolcumu = m2, Aciklama = ofisAciklama
                });
        }
        else
        {
            t.Birimler.Add(new Birim
            {
                BirimTipi = BirimTipi.Komple, Ad = "Komple",
                Yuzolcumu = kapaliM2 > 0 ? kapaliM2 : acikM2
            });
        }
        return t;
    }

    private static KiraSozlesmesi MakeSozlesme(Birim birim, Kiraci kiraci,
        DateTime baslangic, DateTime bitis, decimal bedel, KiraPeriyodu periyot,
        decimal? depozito, bool kdv, decimal kdvOrani = 20, string? notlar = null) => new()
    {
        Birim = birim,
        BirimId = birim.Id,
        Kiraci = kiraci,
        KiraciId = kiraci.Id,
        BaslangicTarihi = baslangic,
        BitisTarihi = bitis,
        KiraBedeli = bedel,
        Periyot = periyot,
        Depozito = depozito,
        Notlar = notlar,
        Durum = SozlesmeDurumu.Aktif,
        KdvUygulanacakMi = kdv,
        KdvOrani = kdv ? kdvOrani : 0,
        IslemGecmisi = [new SozlesmeIslemGecmisi
        {
            IslemTipi = SozlesmeIslemTipi.Olusturma,
            IslemTarihi = baslangic,
            Aciklama = "Sözleşme oluşturuldu.",
            YeniKiraBedeli = bedel
        }]
    };
}
