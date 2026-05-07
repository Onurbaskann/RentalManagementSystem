using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Services;

public class SeedDataService
{
    private readonly ApplicationDbContext _ctx;
    private readonly ITahakkukUretimService _tahakkukUretim;

    public SeedDataService(ApplicationDbContext ctx, ITahakkukUretimService tahakkukUretim)
    {
        _ctx = ctx;
        _tahakkukUretim = tahakkukUretim;
    }

    public async Task SeedBorcTipleriAsync()
    {
        if (await _ctx.BorcTipleri.AnyAsync()) return;

        _ctx.BorcTipleri.AddRange(
            new BorcTipi { Ad = "Kira Bedeli",   Kod = "KIRA",     Aktif = true, Sira = 1 },
            new BorcTipi { Ad = "Ortak Gider",   Kod = "ORTAK",    Aktif = true, Sira = 2 },
            new BorcTipi { Ad = "Portal Gideri", Kod = "PORTAL",   Aktif = true, Sira = 3 },
            new BorcTipi { Ad = "Depozito",      Kod = "DEPOZITO", Aktif = true, Sira = 99, TekSeferlikMi = true }
        );
        await _ctx.SaveChangesAsync();
    }

    public async Task EnsureDepozitoBorcTipiAsync()
    {
        if (await _ctx.BorcTipleri.AnyAsync(b => b.Kod == "DEPOZITO")) return;
        _ctx.BorcTipleri.Add(new BorcTipi { Ad = "Depozito", Kod = "DEPOZITO", Aktif = true, Sira = 99, TekSeferlikMi = true });
        await _ctx.SaveChangesAsync();
    }

    public async Task EnsureManuelBorcTipiAsync()
    {
        if (await _ctx.BorcTipleri.AnyAsync(b => b.Kod == "MANUEL")) return;
        _ctx.BorcTipleri.Add(new BorcTipi { Ad = "Manuel Borç", Kod = "MANUEL", Aktif = true, Sira = 50, TekSeferlikMi = false });
        await _ctx.SaveChangesAsync();
    }

    public async Task EnsureToplantiBorcTipiAsync()
    {
        if (await _ctx.BorcTipleri.AnyAsync(b => b.Kod == "TOPLANTI")) return;
        _ctx.BorcTipleri.Add(new BorcTipi
        {
            Ad = "Toplantı Salonu Kullanım Bedeli",
            Kod = "TOPLANTI",
            Aktif = true,
            Sira = 60,
            TekSeferlikMi = false
        });
        await _ctx.SaveChangesAsync();
    }

    public async Task EnsureVarsayilanRezervasyonUcretKuralAsync()
    {
        if (await _ctx.RezervasyonUcretKurallari.AnyAsync()) return;
        _ctx.RezervasyonUcretKurallari.Add(new RezervasyonUcretKural
        {
            BirimId = null,
            UcretsizSureDakika = 120,
            UcretlendirmePeriyoduDakika = 60,
            PeriyotUcreti = 500,
            KdvOrani = 20,
            Aktif = true,
            Aciklama = "Varsayılan genel kural — 2 saat ücretsiz, sonrası 500 ₺/saat",
            OlusturmaTarihi = DateTime.UtcNow
        });
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedTasinmazTipleriAsync()
    {
        if (await _ctx.TasinmazTipleri.AnyAsync()) return;

        _ctx.TasinmazTipleri.AddRange(
            new TasinmazTipi { Ad = "Bina",      Kod = "BINA",       Aktif = true, Sira = 1,  OlusturmaTarihi = DateTime.UtcNow },
            new TasinmazTipi { Ad = "Arazi",     Kod = "ARAZI",      Aktif = true, Sira = 2,  OlusturmaTarihi = DateTime.UtcNow },
            new TasinmazTipi { Ad = "Tarla",     Kod = "TARLA",      Aktif = true, Sira = 3,  OlusturmaTarihi = DateTime.UtcNow },
            new TasinmazTipi { Ad = "Depo",      Kod = "DEPO",       Aktif = true, Sira = 4,  OlusturmaTarihi = DateTime.UtcNow },
            new TasinmazTipi { Ad = "Otomat",    Kod = "OTOMAT",     Aktif = true, Sira = 5,  OlusturmaTarihi = DateTime.UtcNow },
            new TasinmazTipi { Ad = "Bankamatik",Kod = "BANKAMATIK", Aktif = true, Sira = 6,  OlusturmaTarihi = DateTime.UtcNow },
            new TasinmazTipi { Ad = "Kantin",    Kod = "KANTIN",     Aktif = true, Sira = 7,  OlusturmaTarihi = DateTime.UtcNow },
            new TasinmazTipi { Ad = "Diğer",     Kod = "DIGER",      Aktif = true, Sira = 99, OlusturmaTarihi = DateTime.UtcNow }
        );
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedBirimTurleriAsync()
    {
        if (await _ctx.BirimTurleri.AnyAsync()) return;

        _ctx.BirimTurleri.AddRange(
            new BirimTuru { Ad = "Ofis",             Kod = "OFIS",      Aktif = true, KiralanabilirMi = true,  RezervasyonYapilabilirMi = false, Sira = 1,  OlusturmaTarihi = DateTime.UtcNow },
            new BirimTuru { Ad = "Toplantı Salonu",  Kod = "TOPLANTI",  Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true,  Sira = 10, OlusturmaTarihi = DateTime.UtcNow },
            new BirimTuru { Ad = "Etkinlik Alanı",   Kod = "ETKINLIK",  Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true,  Sira = 11, OlusturmaTarihi = DateTime.UtcNow },
            new BirimTuru { Ad = "Konferans Salonu", Kod = "KONFERANS", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true,  Sira = 12, OlusturmaTarihi = DateTime.UtcNow },
            new BirimTuru { Ad = "Diğer",            Kod = "DIGER",     Aktif = true, KiralanabilirMi = true,  RezervasyonYapilabilirMi = false, Sira = 99, OlusturmaTarihi = DateTime.UtcNow }
        );
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedKiraciKategorileriAsync()
    {
        if (await _ctx.KiraciKategorileri.AnyAsync()) return;

        _ctx.KiraciKategorileri.AddRange(
            new KiraciKategori { Ad = "Akademisyen",         Kod = "AKADEMISYEN",  Aktif = true, Sira = 1,  OlusturmaTarihi = DateTime.UtcNow },
            new KiraciKategori { Ad = "Akademisyen Olmayan", Kod = "AKAD_OLMAYAN", Aktif = true, Sira = 2,  OlusturmaTarihi = DateTime.UtcNow },
            new KiraciKategori { Ad = "Firma",               Kod = "FIRMA",        Aktif = true, Sira = 3,  OlusturmaTarihi = DateTime.UtcNow },
            new KiraciKategori { Ad = "Kamu Kurumu",         Kod = "KAMU",         Aktif = true, Sira = 4,  OlusturmaTarihi = DateTime.UtcNow },
            new KiraciKategori { Ad = "Diğer",               Kod = "DIGER",        Aktif = true, Sira = 99, OlusturmaTarihi = DateTime.UtcNow }
        );
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedSektorlerAsync()
    {
        if (await _ctx.Sektorler.AnyAsync()) return;

        _ctx.Sektorler.AddRange(
            new Sektor { Ad = "Yazılım",  Kod = "YAZILIM",  Aktif = true, Sira = 1,  OlusturmaTarihi = DateTime.UtcNow },
            new Sektor { Ad = "Lojistik", Kod = "LOJISTIK", Aktif = true, Sira = 2,  OlusturmaTarihi = DateTime.UtcNow },
            new Sektor { Ad = "Gıda",     Kod = "GIDA",     Aktif = true, Sira = 3,  OlusturmaTarihi = DateTime.UtcNow },
            new Sektor { Ad = "Tarım",    Kod = "TARIM",    Aktif = true, Sira = 4,  OlusturmaTarihi = DateTime.UtcNow },
            new Sektor { Ad = "Finans",   Kod = "FINANS",   Aktif = true, Sira = 5,  OlusturmaTarihi = DateTime.UtcNow },
            new Sektor { Ad = "Eğitim",   Kod = "EGITIM",   Aktif = true, Sira = 6,  OlusturmaTarihi = DateTime.UtcNow },
            new Sektor { Ad = "Kamu",     Kod = "KAMU",     Aktif = true, Sira = 7,  OlusturmaTarihi = DateTime.UtcNow },
            new Sektor { Ad = "Diğer",    Kod = "DIGER",    Aktif = true, Sira = 99, OlusturmaTarihi = DateTime.UtcNow }
        );
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedTarifelerAsync()
    {
        if (await _ctx.Tarifeler.AnyAsync()) return;

        var aktifBorcTipleri = await _ctx.BorcTipleri.Where(b => b.Aktif && !b.TekSeferlikMi).ToListAsync();
        if (!aktifBorcTipleri.Any()) return;

        var cariYil = DateTime.Now.Year;
        var tarife = new Tarife
        {
            Yil             = cariYil,
            Aciklama        = $"{cariYil} Yılı Tarifesi",
            Aktif           = true,
            OlusturmaTarihi = DateTime.Now
        };

        foreach (var bt in aktifBorcTipleri)
        {
            tarife.Kalemler.Add(new TarifeKalemi
            {
                BorcTipiId       = bt.Id,
                HesaplamaYontemi = HesaplamaYontemi.Sabit,
                BirimDeger       = bt.Kod switch { "KIRA" => 10000m, "ORTAK" => 500m, _ => 0m },
                KdvOrani         = bt.Kod == "ORTAK" ? 20m : 0m
            });
        }

        _ctx.Tarifeler.Add(tarife);
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedDomainDataAsync()
    {
        if (await _ctx.Tasinmazlar.AnyAsync()) return;

        var now = DateTime.Now;
        var adminUser = await _ctx.Users.FirstOrDefaultAsync(u => u.Email == "admin@kiratakip.local");
        var adminId = adminUser?.Id ?? "";

        var tipiMap = await _ctx.TasinmazTipleri.ToDictionaryAsync(t => t.Kod, t => t.Id);

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
        var teknokent = MakeTasinmaz("Teknokent A Blok", tipiMap.GetValueOrDefault("BINA"), KiralamaSekli.BirimBazli,
            "İzmir", "Bornova", "Ege Üniversitesi", "Ege Üniversitesi Teknokent Kampüsü", 500, 4500, 5,
            "Ofis bazlı kiralanabilir teknokent binası",
            new[] { ("101",1,55m,"Girişe yakın ofis"), ("102",1,65m,"Cephe görünümlü"),
                    ("201",2,80m,(string?)null), ("202",2,75m,(string?)null),
                    ("301",3,90m,(string?)null), ("302",3,70m,"Toplantı odalı"),
                    ("401",4,110m,(string?)null), ("501",5,120m,"Teras erişimi") });

        var camlık = MakeTasinmaz("Çamlık Kantini", tipiMap.GetValueOrDefault("BINA"), KiralamaSekli.TekParca,
            "İzmir", "Karşıyaka", "Çamlık", "Çamlık Mahallesi No: 45", 200, 350, null,
            "Bütün olarak kiralanan kantin binası");
        var tarla = MakeTasinmaz("Bornova Tarlası", tipiMap.GetValueOrDefault("TARLA"), KiralamaSekli.TekParca,
            "İzmir", "Bornova", "Doğanlar", "Doğanlar Köyü Mevkii", 12000, 0, null,
            "12.000 m² ekilebilir tarla alanı");
        var dukkan = MakeTasinmaz("Atatürk Cd. Dükkan", tipiMap.GetValueOrDefault("BINA"), KiralamaSekli.TekParca,
            "İzmir", "Konak", "Alsancak", "Atatürk Caddesi No: 112", 0, 180, null,
            "Alsancak'ta sokak cepheli dükkan");
        var sanayiB = MakeTasinmaz("Sanayi Sitesi B Blok", tipiMap.GetValueOrDefault("BINA"), KiralamaSekli.BirimBazli,
            "İzmir", "Kemalpaşa", "OSB", "Kemalpaşa OSB 5. Cadde", 300, 2700, 3,
            "3 katlı sanayi binası",
            new[] { ("101",1,180m,"Zemin kat depo bölümü"), ("201",2,220m,(string?)null), ("301",3,240m,"Yönetim katı") });
        var arazi = MakeTasinmaz("Menemen Arazi", tipiMap.GetValueOrDefault("ARAZI"), KiralamaSekli.TekParca,
            "İzmir", "Menemen", "Görece", "Görece Köyü Arazi Parseli 412", 8500, 0, null,
            "8.500 m² imarsız arazi");
        var depo = MakeTasinmaz("Buca Deposu", tipiMap.GetValueOrDefault("DEPO"), KiralamaSekli.TekParca,
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

        foreach (var s in sozlesmeler.Where(s => s.Durum == SozlesmeDurumu.Aktif))
            await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
    }

    public async Task SeedTahakkuklarAsync()
    {
        if (await _ctx.KiraTahakkuklar.AnyAsync()) return;

        var aktifSozlesmeler = await _ctx.Sozlesmeler
            .Where(s => s.Durum == SozlesmeDurumu.Aktif)
            .ToListAsync();

        foreach (var s in aktifSozlesmeler)
            await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
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

    private static Tasinmaz MakeTasinmaz(string ad, int? tasinmazTipiId, KiralamaSekli sekli,
        string il, string ilce, string mahalle, string acikAdres,
        decimal acikM2, decimal kapaliM2, int? katSayisi, string? aciklama,
        IEnumerable<(string ofisNo, int katNo, decimal m2, string? ofisAciklama)>? ofisler = null)
    {
        var t = new Tasinmaz
        {
            Ad = ad, TasinmazTipiId = tasinmazTipiId, KiralamaSekli = sekli,
            Il = il, Ilce = ilce, Mahalle = mahalle, AcikAdres = acikAdres,
            AcikYuzolcumu = acikM2, KapaliYuzolcumu = kapaliM2,
            KatSayisi = katSayisi, Aciklama = aciklama,
            KayitTarihi = DateTime.Now.AddMonths(-Random.Shared.Next(12, 60))
        };

        if (sekli == KiralamaSekli.BirimBazli && ofisler != null)
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
