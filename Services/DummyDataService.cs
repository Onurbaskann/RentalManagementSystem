using KiraTakip.Models;

namespace KiraTakip.Services;

public class DummyDataService
{
    private static int _tasinmazIdCounter = 1;
    private static int _birimIdCounter = 1;
    private static int _kiraciIdCounter = 1;
    private static int _sozlesmeIdCounter = 1;
    private static int _islemIdCounter = 1;

    public List<Tasinmaz> Tasinmazlar { get; } = new();
    public List<Kiraci> Kiraciler { get; } = new();
    public List<KiraSozlesmesi> Sozlesmeler { get; } = new();

    public DummyDataService()
    {
        Seed();
    }

    private void Seed()
    {
        // --- Kiracılar ---
        var ahmet = SeedKiraci("KRC-000001", KiraciTuru.Gercek, "Ahmet", "Yılmaz",
            tcNo: "12345678901", telefon: "0532 111 2233", email: "ahmet@example.com", adres: "İzmir, Bornova");

        var ayse = SeedKiraci("KRC-000002", KiraciTuru.Gercek, "Ayşe", "Demir",
            tcNo: "98765432100", telefon: "0533 222 3344", email: "ayse@example.com", adres: "İzmir, Karşıyaka");

        var mehmet = SeedKiraci("KRC-000003", KiraciTuru.Gercek, "Mehmet", "Kaya",
            tcNo: "11122233344", telefon: "0541 333 4455", email: "mehmet@example.com", adres: "İzmir, Konak");

        var yildiz = SeedKiraci("KRC-000004", KiraciTuru.Tuzel, "Yıldız Yazılım A.Ş.",
            vergiNo: "1234567890", vergiDairesi: "Bornova VD", ticaretSicilNo: "İZM-12345",
            telefon: "0232 444 5566", email: "info@yildiz.com", adres: "İzmir, Bornova Teknokent");

        var anadolu = SeedKiraci("KRC-000005", KiraciTuru.Tuzel, "Anadolu Lojistik Ltd.",
            vergiNo: "9876543210", vergiDairesi: "Buca VD", ticaretSicilNo: "İZM-67890",
            telefon: "0232 555 6677", email: "info@anadolu.com", adres: "İzmir, Buca");

        var egeTarim = SeedKiraci("KRC-000006", KiraciTuru.Tuzel, "Ege Tarım Koop.",
            vergiNo: "5556667770", vergiDairesi: "Menemen VD", mersisNo: "0555666777000010",
            telefon: "0232 666 7788", email: "info@egetarim.com", adres: "İzmir, Menemen");

        var maviCafe = SeedKiraci("KRC-000007", KiraciTuru.Tuzel, "Mavi Cafe & Restoran",
            vergiNo: "3334445550", vergiDairesi: "Konak VD", ticaretSicilNo: "İZM-33444",
            telefon: "0232 777 8899", email: "info@mavicafe.com", adres: "İzmir, Alsancak");

        // --- Taşınmazlar ---
        var teknokent = AddTasinmaz("Teknokent A Blok", TasinmazTipi.Bina, KiralamaSekli.OfisBazli,
            "İzmir", "Bornova", "Ege Üniversitesi", "Ege Üniversitesi Teknokent Kampüsü",
            500, 4500, 5, "Ofis bazlı kiralanabilir teknokent binası", new[]
            {
                (ofisNo:"101", katNo:1, m2:55m, aciklama:"Girişe yakın ofis"),
                (ofisNo:"102", katNo:1, m2:65m, aciklama:"Cephe görünümlü"),
                (ofisNo:"201", katNo:2, m2:80m, aciklama:(string?)null),
                (ofisNo:"202", katNo:2, m2:75m, aciklama:(string?)null),
                (ofisNo:"301", katNo:3, m2:90m, aciklama:(string?)null),
                (ofisNo:"302", katNo:3, m2:70m, aciklama:"Toplantı odalı"),
                (ofisNo:"401", katNo:4, m2:110m, aciklama:(string?)null),
                (ofisNo:"501", katNo:5, m2:120m, aciklama:"Teras erişimi"),
            });

        var camlık = AddTasinmaz("Çamlık Kantini", TasinmazTipi.Bina, KiralamaSekli.TekParca,
            "İzmir", "Karşıyaka", "Çamlık", "Çamlık Mahallesi No: 45",
            200, 350, null, "Bütün olarak kiralanan kantin binası");

        var tarlaBornova = AddTasinmaz("Bornova Tarlası", TasinmazTipi.Tarla, KiralamaSekli.TekParca,
            "İzmir", "Bornova", "Doğanlar", "Doğanlar Köyü Mevkii",
            12000, 0, null, "12.000 m² ekilebilir tarla alanı");

        var dukkan = AddTasinmaz("Atatürk Cd. Dükkan", TasinmazTipi.Bina, KiralamaSekli.TekParca,
            "İzmir", "Konak", "Alsancak", "Atatürk Caddesi No: 112",
            0, 180, null, "Alsancak'ta sokak cepheli dükkan");

        var sanayiB = AddTasinmaz("Sanayi Sitesi B Blok", TasinmazTipi.Bina, KiralamaSekli.OfisBazli,
            "İzmir", "Kemalpaşa", "OSB", "Kemalpaşa OSB 5. Cadde",
            300, 2700, 3, "3 katlı sanayi binası", new[]
            {
                (ofisNo:"101", katNo:1, m2:180m, aciklama:"Zemin kat depo bölümü"),
                (ofisNo:"201", katNo:2, m2:220m, aciklama:(string?)null),
                (ofisNo:"301", katNo:3, m2:240m, aciklama:"Yönetim katı"),
            });

        var arazi = AddTasinmaz("Menemen Arazi", TasinmazTipi.Arazi, KiralamaSekli.TekParca,
            "İzmir", "Menemen", "Görece", "Görece Köyü Arazi Parseli 412",
            8500, 0, null, "8.500 m² imarsız arazi");

        var depo = AddTasinmaz("Buca Deposu", TasinmazTipi.Depo, KiralamaSekli.TekParca,
            "İzmir", "Buca", "Sanayi", "Buca Sanayi Sitesi B-12",
            150, 1200, null, "1.200 m² kapalı lojistik deposu");

        // --- Birim referansları ---
        var tkOfis101 = teknokent.Birimler.First(b => b.OfisNo == "101");
        var tkOfis102 = teknokent.Birimler.First(b => b.OfisNo == "102");
        var tkOfis201 = teknokent.Birimler.First(b => b.OfisNo == "201");
        var tkOfis302 = teknokent.Birimler.First(b => b.OfisNo == "302");
        var snOfis101 = sanayiB.Birimler.First(b => b.OfisNo == "101");
        var snOfis201 = sanayiB.Birimler.First(b => b.OfisNo == "201");
        var camlıkBirim = camlık.Birimler[0];
        var tarlabirim = tarlaBornova.Birimler[0];
        var dukkanBirim = dukkan.Birimler[0];
        var depoBirim = depo.Birimler[0];

        var now = DateTime.Now;

        // Aktif sözleşmeler
        SeedSozlesme(tkOfis101, yildiz, now.AddMonths(-6), now.AddMonths(18), 15000, KiraPeriyodu.Aylik, 45000,
            kdv: true, kdvOrani: 20, notlar: "Teknokent Ofis 101 — Yıldız Yazılım");

        SeedSozlesme(tkOfis102, ahmet, now.AddMonths(-5), now.AddDays(45), 7500, KiraPeriyodu.Aylik, 22500,
            kdv: false, notlar: "Teknokent Ofis 102 — Ahmet Yılmaz");

        // Süresi dolmak üzere
        SeedSozlesme(tkOfis201, ayse, now.AddYears(-1), now.AddDays(12), 9000, KiraPeriyodu.Aylik, 27000,
            kdv: false, notlar: "Teknokent Ofis 201 — Ayşe Demir");

        // Aktif uzun vadeli
        SeedSozlesme(tkOfis302, mehmet, now.AddMonths(-3), now.AddMonths(21), 8000, KiraPeriyodu.Aylik, 24000,
            kdv: false, notlar: "Teknokent Ofis 302 — Mehmet Kaya");

        SeedSozlesme(snOfis101, egeTarim, now.AddMonths(-8), now.AddMonths(16), 120000, KiraPeriyodu.Yillik, 60000,
            kdv: true, kdvOrani: 20, notlar: "Sanayi B Ofis 101 — Ege Tarım");

        // Sanayi Ofis 201 süresi dolmak üzere
        SeedSozlesme(snOfis201, anadolu, now.AddYears(-1), now.AddDays(22), 144000, KiraPeriyodu.Yillik, 72000,
            kdv: true, kdvOrani: 20, notlar: "Sanayi B Ofis 201 — Anadolu Lojistik");

        SeedSozlesme(camlıkBirim, maviCafe, now.AddMonths(-3), now.AddMonths(21), 8500, KiraPeriyodu.Aylik, 25500,
            kdv: true, kdvOrani: 20, notlar: "Çamlık Kantini — Mavi Cafe");

        // Geçmiş sözleşme (dükkan)
        SeedSozlesme(dukkanBirim, ahmet, now.AddYears(-2), now.AddMonths(-3), 6000, KiraPeriyodu.Aylik, 18000,
            kdv: false, notlar: "Atatürk Cd. Dükkan — Ahmet Yılmaz");

        // Aktif uzun vadeli
        SeedSozlesme(tarlabirim, egeTarim, now.AddMonths(-2), now.AddMonths(22), 36000, KiraPeriyodu.Yillik, 36000,
            kdv: true, kdvOrani: 20, notlar: "Bornova Tarlası — Ege Tarım");

        SeedSozlesme(depoBirim, anadolu, now.AddMonths(-8), now.AddMonths(16), 180000, KiraPeriyodu.Yillik, 90000,
            kdv: true, kdvOrani: 20, notlar: "Buca Deposu — Anadolu Lojistik");

        // Tarih bazlı durum ataması
        foreach (var soz in Sozlesmeler)
        {
            if (soz.BitisTarihi < DateTime.Now)
                soz.Durum = SozlesmeDurumu.SonaErdi;
        }
    }

    private Kiraci SeedKiraci(string kiraciNo, KiraciTuru tur, string ad, string? soyad = null,
        string? tcNo = null, string? vergiNo = null, string? vergiDairesi = null,
        string? ticaretSicilNo = null, string? mersisNo = null,
        string telefon = "", string email = "", string? adres = null)
    {
        var k = new Kiraci
        {
            Id = _kiraciIdCounter++,
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
        Kiraciler.Add(k);
        return k;
    }

    private Tasinmaz AddTasinmaz(string ad, TasinmazTipi tipi, KiralamaSekli sekli,
        string il, string ilce, string mahalle, string acikAdres,
        decimal acikM2, decimal kapaliM2, int? katSayisi, string? aciklama,
        IEnumerable<(string ofisNo, int katNo, decimal m2, string? aciklama)>? ofisler = null)
    {
        var t = new Tasinmaz
        {
            Id = _tasinmazIdCounter++,
            Ad = ad,
            Tipi = tipi,
            KiralamaSekli = sekli,
            Il = il,
            Ilce = ilce,
            Mahalle = mahalle,
            AcikAdres = acikAdres,
            AcikYuzolcumu = acikM2,
            KapaliYuzolcumu = kapaliM2,
            KatSayisi = katSayisi,
            Aciklama = aciklama,
            KayitTarihi = DateTime.Now.AddMonths(-Random.Shared.Next(12, 60))
        };

        if (sekli == KiralamaSekli.OfisBazli && ofisler != null)
        {
            foreach (var (ofisNo, katNo, m2, ofisAciklama) in ofisler)
            {
                t.Birimler.Add(new Birim
                {
                    Id = _birimIdCounter++,
                    TasinmazId = t.Id,
                    Tasinmaz = t,
                    BirimTipi = BirimTipi.Ofis,
                    OfisNo = ofisNo,
                    KatNo = katNo,
                    Ad = $"Ofis {ofisNo}",
                    Yuzolcumu = m2,
                    Aciklama = ofisAciklama
                });
            }
        }
        else
        {
            t.Birimler.Add(new Birim
            {
                Id = _birimIdCounter++,
                TasinmazId = t.Id,
                Tasinmaz = t,
                BirimTipi = BirimTipi.Komple,
                Ad = "Komple",
                Yuzolcumu = kapaliM2 > 0 ? kapaliM2 : acikM2
            });
        }

        Tasinmazlar.Add(t);
        return t;
    }

    private KiraSozlesmesi SeedSozlesme(Birim birim, Kiraci kiraci,
        DateTime baslangic, DateTime bitis, decimal bedel, KiraPeriyodu periyot,
        decimal? depozito, bool kdv, decimal kdvOrani = 20, string? notlar = null)
    {
        var s = new KiraSozlesmesi
        {
            Id = _sozlesmeIdCounter++,
            BirimId = birim.Id,
            Birim = birim,
            KiraciId = kiraci.Id,
            Kiraci = kiraci,
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
                Id = _islemIdCounter++,
                KiraSozlesmesiId = _sozlesmeIdCounter - 1,
                IslemTipi = SozlesmeIslemTipi.Olusturma,
                IslemTarihi = baslangic,
                Aciklama = "Sözleşme oluşturuldu.",
                YeniKiraBedeli = bedel
            }]
        };
        birim.Sozlesmeler.Add(s);
        Sozlesmeler.Add(s);
        return s;
    }

    // --- CRUD ---

    public Tasinmaz? GetTasinmaz(int id) => Tasinmazlar.FirstOrDefault(t => t.Id == id);
    public Kiraci? GetKiraci(int id) => Kiraciler.FirstOrDefault(k => k.Id == id);
    public KiraSozlesmesi? GetSozlesme(int id) => Sozlesmeler.FirstOrDefault(s => s.Id == id);

    public Birim? GetBirim(int id) =>
        Tasinmazlar.SelectMany(t => t.Birimler).FirstOrDefault(b => b.Id == id);

    public List<Birim> GetTumBirimler() =>
        Tasinmazlar.SelectMany(t => t.Birimler).ToList();

    public string GenerateKiraciNo()
    {
        var existing = Kiraciler.Select(k => k.KiraciNo).ToHashSet();
        for (int i = 1; i <= 999999; i++)
        {
            var no = $"KRC-{i:D6}";
            if (!existing.Contains(no)) return no;
        }
        throw new InvalidOperationException("KiraciNo üretilemedi.");
    }

    public bool KiraciNoExists(string kiraciNo, int? excludeId = null) =>
        Kiraciler.Any(k => k.KiraciNo == kiraciNo && (excludeId == null || k.Id != excludeId));

    public int AddKiraci(Kiraci k)
    {
        if (string.IsNullOrWhiteSpace(k.KiraciNo))
            k.KiraciNo = GenerateKiraciNo();
        k.Id = _kiraciIdCounter++;
        k.KayitTarihi = DateTime.Now;
        Kiraciler.Add(k);
        return k.Id;
    }

    public void UpdateKiraci(Kiraci updated)
    {
        var existing = GetKiraci(updated.Id);
        if (existing == null) return;
        existing.KiraciNo = updated.KiraciNo;
        existing.KiraciTuru = updated.KiraciTuru;
        existing.Ad = updated.Ad;
        existing.Soyad = updated.Soyad;
        existing.TcKimlikNo = updated.TcKimlikNo;
        existing.PasaportNo = updated.PasaportNo;
        existing.Unvan = updated.Unvan;
        existing.AnneAdi = updated.AnneAdi;
        existing.BabaAdi = updated.BabaAdi;
        existing.DogumTarihi = updated.DogumTarihi;
        existing.DogumYeri = updated.DogumYeri;
        existing.TicaretSicilNo = updated.TicaretSicilNo;
        existing.VergiNo = updated.VergiNo;
        existing.VergiDairesi = updated.VergiDairesi;
        existing.MersisNo = updated.MersisNo;
        existing.Telefon = updated.Telefon;
        existing.Email = updated.Email;
        existing.Adres = updated.Adres;
    }

    public void TasinmazEkle(Tasinmaz t, List<Models.ViewModels.OfisBirimInputViewModel>? ofisler = null)
    {
        t.Id = _tasinmazIdCounter++;
        t.KayitTarihi = DateTime.Now;

        if (t.KiralamaSekli == KiralamaSekli.OfisBazli && ofisler != null && ofisler.Count > 0)
        {
            foreach (var o in ofisler)
            {
                var ad = string.IsNullOrWhiteSpace(o.Ad) ? $"Ofis {o.OfisNo}" : o.Ad;
                t.Birimler.Add(new Birim
                {
                    Id = _birimIdCounter++,
                    TasinmazId = t.Id,
                    Tasinmaz = t,
                    BirimTipi = BirimTipi.Ofis,
                    OfisNo = o.OfisNo,
                    KatNo = o.KatNo,
                    Ad = ad,
                    Yuzolcumu = o.Yuzolcumu,
                    Aciklama = o.Aciklama
                });
            }
        }
        else
        {
            t.Birimler.Add(new Birim
            {
                Id = _birimIdCounter++,
                TasinmazId = t.Id,
                Tasinmaz = t,
                BirimTipi = BirimTipi.Komple,
                Ad = "Komple",
                Yuzolcumu = t.KapaliYuzolcumu > 0 ? t.KapaliYuzolcumu : t.AcikYuzolcumu
            });
        }

        Tasinmazlar.Add(t);
    }

    public void SozlesmeEkle(KiraSozlesmesi s)
    {
        s.Id = _sozlesmeIdCounter++;
        var birim = GetBirim(s.BirimId);
        var kiraci = GetKiraci(s.KiraciId);
        if (birim != null) { s.Birim = birim; birim.Sozlesmeler.Add(s); }
        if (kiraci != null) s.Kiraci = kiraci;

        if (s.IslemGecmisi.Count == 0)
        {
            s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
            {
                Id = _islemIdCounter++,
                KiraSozlesmesiId = s.Id,
                IslemTipi = SozlesmeIslemTipi.Olusturma,
                IslemTarihi = DateTime.Now,
                Aciklama = "Sözleşme oluşturuldu.",
                YeniKiraBedeli = s.KiraBedeli
            });
        }

        Sozlesmeler.Add(s);
    }

    public void UzatSozlesme(int sozlesmeId, DateTime yeniBitisTarihi, decimal yeniKiraBedeli,
        bool kdvUygulanacakMi, decimal kdvOrani, decimal? tufeOrani, string? aciklama)
    {
        var s = GetSozlesme(sozlesmeId);
        if (s == null) return;

        var eskiBitis = s.BitisTarihi;
        var eskiBedel = s.KiraBedeli;

        s.BitisTarihi = yeniBitisTarihi;
        s.KiraBedeli = yeniKiraBedeli;
        s.KdvUygulanacakMi = kdvUygulanacakMi;
        if (kdvUygulanacakMi) s.KdvOrani = kdvOrani;

        decimal? kdvTutari = kdvUygulanacakMi ? yeniKiraBedeli * kdvOrani / 100 : null;
        decimal? kdvDahil = kdvUygulanacakMi ? yeniKiraBedeli + kdvTutari : null;

        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            Id = _islemIdCounter++,
            KiraSozlesmesiId = sozlesmeId,
            IslemTipi = SozlesmeIslemTipi.SureUzatma,
            IslemTarihi = DateTime.Now,
            Aciklama = aciklama ?? "Sözleşme süresi uzatıldı.",
            EskiBitisTarihi = eskiBitis,
            YeniBitisTarihi = yeniBitisTarihi,
            EskiKiraBedeli = eskiBedel,
            YeniKiraBedeli = yeniKiraBedeli,
            TufeOrani = tufeOrani,
            KdvUygulandiMi = kdvUygulanacakMi,
            KdvOrani = kdvUygulanacakMi ? kdvOrani : null,
            KdvTutari = kdvTutari,
            KdvDahilTutar = kdvDahil
        });
    }

    public void FeshetSozlesme(int sozlesmeId, DateTime fesihTarihi, string fesihNedeni, string? aciklama)
    {
        var s = GetSozlesme(sozlesmeId);
        if (s == null) return;

        s.Durum = SozlesmeDurumu.Feshedildi;
        s.FesihTarihi = fesihTarihi;
        s.FesihNedeni = fesihNedeni;

        s.IslemGecmisi.Add(new SozlesmeIslemGecmisi
        {
            Id = _islemIdCounter++,
            KiraSozlesmesiId = sozlesmeId,
            IslemTipi = SozlesmeIslemTipi.Fesih,
            IslemTarihi = DateTime.Now,
            Aciklama = aciklama ?? fesihNedeni
        });
    }

    public List<Birim> GetBosBirimler()
    {
        return GetTumBirimler()
            .Where(b => !b.Sozlesmeler.Any(s =>
                s.Durum == SozlesmeDurumu.Aktif &&
                s.BaslangicTarihi <= DateTime.Now &&
                s.BitisTarihi >= DateTime.Now))
            .ToList();
    }
}
