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
        var existingCodes = await _ctx.BorcTipleri.Select(b => b.Kod).ToListAsync();
        var toAdd = new List<BorcTipi>();

        if (!existingCodes.Contains("KIRA")) toAdd.Add(new BorcTipi { Ad = "Kira Bedeli", Kod = "KIRA", Aktif = true, Sira = 1, Davranis = BorcTipiDavranisi.AylikSabit });
        if (!existingCodes.Contains("ORTAK")) toAdd.Add(new BorcTipi { Ad = "Ortak Gider", Kod = "ORTAK", Aktif = true, Sira = 2, Davranis = BorcTipiDavranisi.AylikSabit });
        if (!existingCodes.Contains("PORTAL")) toAdd.Add(new BorcTipi { Ad = "Portal Gideri", Kod = "PORTAL", Aktif = true, Sira = 3, Davranis = BorcTipiDavranisi.AylikSabit });
        if (!existingCodes.Contains("MANUEL")) toAdd.Add(new BorcTipi { Ad = "Manuel Borç", Kod = "MANUEL", Aktif = true, Sira = 50, Davranis = BorcTipiDavranisi.ManuelTetiklemeli });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new BorcTipi { Ad = "Toplantı Salonu Kullanım Bedeli", Kod = "TOPLANTI", Aktif = true, Sira = 60, Davranis = BorcTipiDavranisi.ManuelTetiklemeli });
        if (!existingCodes.Contains("DEPOZITO")) toAdd.Add(new BorcTipi { Ad = "Depozito", Kod = "DEPOZITO", Aktif = true, Sira = 99, Davranis = BorcTipiDavranisi.IlkAyTekSeferlik });

        if (toAdd.Any())
        {
            _ctx.BorcTipleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task EnsureDepozitoBorcTipiAsync() { }
    public async Task EnsureManuelBorcTipiAsync() { }
    public async Task EnsureToplantiBorcTipiAsync() { }

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
        var existingCodes = await _ctx.TasinmazTipleri.Select(t => t.Kod).ToListAsync();
        var toAdd = new List<TasinmazTipi>();

        if (!existingCodes.Contains("BINA")) toAdd.Add(new TasinmazTipi { Ad = "Bina", Kod = "BINA", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("ARAZI")) toAdd.Add(new TasinmazTipi { Ad = "Arazi", Kod = "ARAZI", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TARLA")) toAdd.Add(new TasinmazTipi { Ad = "Tarla", Kod = "TARLA", Aktif = true, Sira = 3, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("DEPO")) toAdd.Add(new TasinmazTipi { Ad = "Depo", Kod = "DEPO", Aktif = true, Sira = 4, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("OTOMAT")) toAdd.Add(new TasinmazTipi { Ad = "Otomat", Kod = "OTOMAT", Aktif = true, Sira = 5, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("BANKAMATIK")) toAdd.Add(new TasinmazTipi { Ad = "Bankamatik", Kod = "BANKAMATIK", Aktif = true, Sira = 6, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("KANTIN")) toAdd.Add(new TasinmazTipi { Ad = "Kantin", Kod = "KANTIN", Aktif = true, Sira = 7, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("DIGER")) toAdd.Add(new TasinmazTipi { Ad = "Diğer", Kod = "DIGER", Aktif = true, Sira = 99, OlusturmaTarihi = DateTime.UtcNow });

        if (toAdd.Any())
        {
            _ctx.TasinmazTipleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedBirimTurleriAsync()
    {
        var existingCodes = await _ctx.BirimTurleri.Select(t => t.Kod).ToListAsync();
        var toAdd = new List<BirimTuru>();

        if (!existingCodes.Contains("OFIS")) toAdd.Add(new BirimTuru { Ad = "Ofis", Kod = "OFIS", Aktif = true, KiralanabilirMi = true, RezervasyonYapilabilirMi = false, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TOPLANTI")) toAdd.Add(new BirimTuru { Ad = "Toplantı Salonu", Kod = "TOPLANTI", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true, Sira = 10, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("ETKINLIK")) toAdd.Add(new BirimTuru { Ad = "Etkinlik Alanı", Kod = "ETKINLIK", Aktif = true, KiralanabilirMi = false, RezervasyonYapilabilirMi = true, Sira = 11, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("DIGER")) toAdd.Add(new BirimTuru { Ad = "Diğer", Kod = "DIGER", Aktif = true, KiralanabilirMi = true, RezervasyonYapilabilirMi = false, Sira = 99, OlusturmaTarihi = DateTime.UtcNow });

        if (toAdd.Any())
        {
            _ctx.BirimTurleri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedKiraciKategorileriAsync()
    {
        var existingCodes = await _ctx.KiraciKategorileri.Select(k => k.Kod).ToListAsync();
        var toAdd = new List<KiraciKategori>();

        if (!existingCodes.Contains("AKADEMISYEN")) toAdd.Add(new KiraciKategori { Ad = "Akademisyen", Kod = "AKADEMISYEN", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("AKAD_OLMAYAN")) toAdd.Add(new KiraciKategori { Ad = "Akademisyen Olmayan", Kod = "AKAD_OLMAYAN", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });


        if (toAdd.Any())
        {
            _ctx.KiraciKategorileri.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedSektorlerAsync()
    {
        var existingCodes = await _ctx.Sektorler.Select(s => s.Kod).ToListAsync();
        var toAdd = new List<Sektor>();

        if (!existingCodes.Contains("YAZILIM")) toAdd.Add(new Sektor { Ad = "Yazılım", Kod = "YAZILIM", Aktif = true, Sira = 1, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("LOJISTIK")) toAdd.Add(new Sektor { Ad = "Lojistik", Kod = "LOJISTIK", Aktif = true, Sira = 2, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("GIDA")) toAdd.Add(new Sektor { Ad = "Gıda", Kod = "GIDA", Aktif = true, Sira = 3, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("TARIM")) toAdd.Add(new Sektor { Ad = "Tarım", Kod = "TARIM", Aktif = true, Sira = 4, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("FINANS")) toAdd.Add(new Sektor { Ad = "Finans", Kod = "FINANS", Aktif = true, Sira = 5, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("EGITIM")) toAdd.Add(new Sektor { Ad = "Eğitim", Kod = "EGITIM", Aktif = true, Sira = 6, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("KAMU")) toAdd.Add(new Sektor { Ad = "Kamu", Kod = "KAMU", Aktif = true, Sira = 7, OlusturmaTarihi = DateTime.UtcNow });
        if (!existingCodes.Contains("DIGER")) toAdd.Add(new Sektor { Ad = "Diğer", Kod = "DIGER", Aktif = true, Sira = 99, OlusturmaTarihi = DateTime.UtcNow });

        if (toAdd.Any())
        {
            _ctx.Sektorler.AddRange(toAdd);
            await _ctx.SaveChangesAsync();
        }
    }

    public async Task SeedTarifelerAsync()
    {
        if (await _ctx.Tarifeler.AnyAsync()) return;

        var aktifBorcTipleri = await _ctx.BorcTipleri.Where(b => b.Aktif && b.Davranis == BorcTipiDavranisi.AylikSabit).ToListAsync();
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
                BirimDeger       = bt.Kod switch { "KIRA" => 20000m, "ORTAK" => 1250m, "PORTAL" => 350m, _ => 0m },
                KdvOrani         = (bt.Kod == "ORTAK" || bt.Kod == "PORTAL") ? 20m : 20m
            });
        }

        _ctx.Tarifeler.Add(tarife);
        await _ctx.SaveChangesAsync();
    }

    public async Task ClearDomainDataAsync()
    {
        _ctx.ToplantiSalonuRezervasyonlari.RemoveRange(_ctx.ToplantiSalonuRezervasyonlari);
        _ctx.KiraOdemeler.RemoveRange(_ctx.KiraOdemeler);
        _ctx.BankaHareketleri.RemoveRange(_ctx.BankaHareketleri);
        _ctx.KiraTahakkuklar.RemoveRange(_ctx.KiraTahakkuklar);
        _ctx.SozlesmeRateler.RemoveRange(_ctx.SozlesmeRateler);
        _ctx.SozlesmeIslemGecmisleri.RemoveRange(_ctx.SozlesmeIslemGecmisleri);
        _ctx.Sozlesmeler.RemoveRange(_ctx.Sozlesmeler);
        _ctx.Birimler.RemoveRange(_ctx.Birimler);
        _ctx.Tasinmazlar.RemoveRange(_ctx.Tasinmazlar);
        _ctx.Kiraciler.RemoveRange(_ctx.Kiraciler);
        _ctx.Tarifeler.RemoveRange(_ctx.Tarifeler);
        _ctx.TasinmazKiraciKategoriFiyatlari.RemoveRange(_ctx.TasinmazKiraciKategoriFiyatlari);
        await _ctx.SaveChangesAsync();
    }

    public async Task SeedDomainDataAsync()
    {
        if (await _ctx.Tasinmazlar.AnyAsync()) return;

        var now = DateTime.Now;
        var tipiMap = await _ctx.TasinmazTipleri.ToDictionaryAsync(t => t.Kod, t => t.Id);
        var birimTuruMap = await _ctx.BirimTurleri.ToDictionaryAsync(t => t.Kod, t => t.Id);
        var katMap = await _ctx.KiraciKategorileri.ToDictionaryAsync(k => k.Kod, k => k.Id);
        var sekMap = await _ctx.Sektorler.ToDictionaryAsync(s => s.Kod, s => s.Id);

        // --- Kiracılar ---
        var ahmet = Kiraci("KRC-001", KiraciTuru.Gercek, katMap["AKADEMISYEN"], sekMap["EGITIM"], "Ahmet", "Yılmaz",
            tcNo: "12345678901", telefon: "0532 111 2233", email: "ahmet@ege.edu.tr", adres: "İzmir, Bornova");
        var ayse = Kiraci("KRC-002", KiraciTuru.Gercek, katMap["AKADEMISYEN"], sekMap["YAZILIM"], "Ayşe", "Demir",
            tcNo: "98765432100", telefon: "0533 222 3344", email: "ayse@ege.edu.tr", adres: "İzmir, Karşıyaka");
        var yzCozum = Kiraci("KRC-003", KiraciTuru.Tuzel, katMap["AKAD_OLMAYAN"], sekMap["YAZILIM"], "Yapay Zeka Çözümleri A.Ş.", null,
            vergiNo: "1234567890", ticaretSicilNo: "İZM-123", telefon: "0232 444 5566", email: "info@yz.com", adres: "Teknokent");
        var biyoLab = Kiraci("KRC-004", KiraciTuru.Tuzel, katMap["AKADEMISYEN"], sekMap["DIGER"], "BiyoTek Laboratuvarları Ltd.", null,
            vergiNo: "9876543210", ticaretSicilNo: "İZM-456", telefon: "0232 555 6677", email: "info@biyotek.com", adres: "Teknokent");
        var veriBilisim = Kiraci("KRC-005", KiraciTuru.Tuzel, katMap["AKAD_OLMAYAN"], sekMap["YAZILIM"], "Veri Bilişim A.Ş.", null,
            vergiNo: "5556667770", ticaretSicilNo: "İZM-789", telefon: "0232 666 7788", email: "iletisim@veribilisim.com", adres: "Teknokent");

        _ctx.Kiraciler.AddRange(ahmet, ayse, yzCozum, biyoLab, veriBilisim);

        // --- Taşınmaz (Teknokent A Blok) ---
        var ofisTuruId = birimTuruMap["OFIS"];
        var toplantiTuruId = birimTuruMap["TOPLANTI"];

        var teknokent = new Tasinmaz
        {
            Ad = "Teknokent A Blok", TasinmazTipiId = tipiMap.GetValueOrDefault("BINA"), KiralamaSekli = KiralamaSekli.BirimBazli,
            Il = "İzmir", Ilce = "Bornova", Mahalle = "Ege Üniversitesi", AcikAdres = "Ege Üniversitesi Teknokent Kampüsü",
            AcikYuzolcumu = 500, KapaliYuzolcumu = 4500, KatSayisi = 4, Aciklama = "Ofis bazlı kiralanabilir teknokent binası",
            KayitTarihi = now.AddMonths(-36)
        };

        // 16 Ofis Ekleme
        for (int kat = 1; kat <= 4; kat++)
        {
            for (int ofis = 1; ofis <= 4; ofis++)
            {
                var ofisNo = $"{kat}0{ofis}";
                teknokent.Birimler.Add(new Birim
                {
                    BirimTipi = BirimTipi.Ofis, OfisNo = ofisNo, KatNo = kat,
                    Ad = $"Ofis {ofisNo}", Yuzolcumu = 60 + (ofis * 10), BirimTuruId = ofisTuruId
                });
            }
        }
        
        // 1 Ana Toplantı Salonu
        teknokent.Birimler.Add(new Birim
        {
            BirimTipi = BirimTipi.Ofis, OfisNo = "Z01", KatNo = 0,
            Ad = "Ana Toplantı Salonu", Yuzolcumu = 150, BirimTuruId = toplantiTuruId, Aciklama = "Ortak kullanıma açık ana rezervasyon alanı."
        });

        _ctx.Tasinmazlar.Add(teknokent);
        await _ctx.SaveChangesAsync();

        // --- Sözleşmeler ---
        var ofis101 = teknokent.Birimler.First(b => b.OfisNo == "101");
        var ofis102 = teknokent.Birimler.First(b => b.OfisNo == "102");
        var ofis201 = teknokent.Birimler.First(b => b.OfisNo == "201");
        var ofis301 = teknokent.Birimler.First(b => b.OfisNo == "301");
        var ofis302 = teknokent.Birimler.First(b => b.OfisNo == "302");
        var ofis401 = teknokent.Birimler.First(b => b.OfisNo == "401");

        var btKiraId = (await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "KIRA")).Id;

        var sozlesmeler = new List<KiraSozlesmesi>
        {
            // Aktif sözleşmeler
            MakeSozlesme(ofis101, yzCozum, now.AddMonths(-12), now.AddMonths(24), 25000, KiraPeriyodu.Aylik, 50000, true),
            MakeSozlesme(ofis201, ahmet, now.AddMonths(-6), now.AddMonths(18), 18000, KiraPeriyodu.Aylik, 18000, false),
            MakeSozlesme(ofis301, biyoLab, now.AddMonths(-8), now.AddMonths(4), 30000, KiraPeriyodu.Aylik, 60000, true),
            MakeSozlesme(ofis401, veriBilisim, now.AddMonths(-15), now.AddMonths(9), 22000, KiraPeriyodu.Aylik, 44000, true),
            
            // Süresi dolmak üzere olan sözleşmeler
            MakeSozlesme(ofis102, ayse, now.AddMonths(-11).AddDays(-20), now.AddDays(10), 16000, KiraPeriyodu.Aylik, 16000, false),
            MakeSozlesme(ofis302, yzCozum, now.AddMonths(-23).AddDays(-25), now.AddDays(5), 20000, KiraPeriyodu.Aylik, 40000, true),
        };

        foreach (var s in sozlesmeler)
        {
            if (s.BitisTarihi < now) s.Durum = SozlesmeDurumu.SonaErdi;
        }

        _ctx.Sozlesmeler.AddRange(sozlesmeler);
        await _ctx.SaveChangesAsync();

        // 5.5. Pazarlık Fiyatı Örneği — yalnızca Ofis 101 / YZ Çözüm sözleşmesi
        // Fiyat matrisi: AkademikOlmayan + KIRA = 450₺/m² × 70m² = 31.500₺
        // Pazarlık sonucu: 360₺/m² × 70m² = 25.200₺ (matrix fiyatından indirimli, m2 bazlı)
        // DEPOZITO: matrix 25.000₺ sabit; pazarlıkla 40.000₺ olarak belirlendi
        var btDepozitoId = (await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "DEPOZITO")).Id;
        _ctx.SozlesmeRateler.AddRange(
            new SozlesmeRate
            {
                SozlesmeId = sozlesmeler[0].Id,
                BorcTipiId = btKiraId,
                BirimDeger = 360,
                HesaplamaYontemi = HesaplamaYontemi.M2,
                KdvOrani = 20
            },
            new SozlesmeRate
            {
                SozlesmeId = sozlesmeler[0].Id,
                BorcTipiId = btDepozitoId,
                BirimDeger = 40000,
                HesaplamaYontemi = HesaplamaYontemi.Sabit,
                KdvOrani = 0
            }
        );
        await _ctx.SaveChangesAsync();



        // 6. Sözleşme Artış Geçmişi (Seed)
        foreach (var s in sozlesmeler.Where(s => s.BaslangicTarihi < now.AddYears(-1)))
        {
            _ctx.SozlesmeIslemGecmisleri.Add(new SozlesmeIslemGecmisi
            {
                KiraSozlesmesiId = s.Id,
                IslemTipi = SozlesmeIslemTipi.TufeArtis,
                IslemTarihi = s.BaslangicTarihi.AddYears(1),
                Aciklama = "Yıllık TÜFE Artışı Uygulandı",
                EskiKiraBedeli = s.KiraBedeli * 0.8m,
                YeniKiraBedeli = s.KiraBedeli,
                TufeOrani = 25m
            });
        }
        await _ctx.SaveChangesAsync();

        // 7. Fiyat matrisi tahakkuk üretiminden önce eklenmeli
        await SeedTasinmazFiyatlarAsync();

        // 8. Tahakkukların Üretilmesi
        foreach (var s in sozlesmeler.Where(s => s.Durum == SozlesmeDurumu.Aktif))
            await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
        await SeedRezervasyonlarAsync();
        await SeedBankaHareketleriAsync();
        await SeedTahakkuklarVeOdemelerAsync(sozlesmeler);
    }

    public async Task SeedTasinmazFiyatlarAsync()
    {
        var teknokent = await _ctx.Tasinmazlar.FirstOrDefaultAsync(t => t.Ad == "Teknokent A Blok");
        if (teknokent == null) return;

        if (await _ctx.TasinmazKiraciKategoriFiyatlari.AnyAsync(f => f.TasinmazId == teknokent.Id)) return;

        var katAkademisyen = await _ctx.KiraciKategorileri.FirstAsync(k => k.Kod == "AKADEMISYEN");
        var katAkadOlmayan = await _ctx.KiraciKategorileri.FirstAsync(k => k.Kod == "AKAD_OLMAYAN");


        var btKira = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "KIRA");
        var btOrtak = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "ORTAK");
        var btPortal = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "PORTAL");
        var btDepozito = await _ctx.BorcTipleri.FirstAsync(b => b.Kod == "DEPOZITO");

        _ctx.TasinmazKiraciKategoriFiyatlari.AddRange(
            // Akademisyen için (m2 bazlı kira)
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btKira.Id, BirimDeger = 350, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btOrtak.Id, BirimDeger = 1500, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btPortal.Id, BirimDeger = 500, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkademisyen.Id, BorcTipiId = btDepozito.Id, BirimDeger = 10000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 },

            // Akademisyen Olmayan için (sabit kira)
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btKira.Id, BirimDeger = 450, HesaplamaYontemi = HesaplamaYontemi.M2, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btOrtak.Id, BirimDeger = 2500, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btPortal.Id, BirimDeger = 750, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 20 },
            new TasinmazKiraciKategoriFiyat { TasinmazId = teknokent.Id, KiraciKategoriId = katAkadOlmayan.Id, BorcTipiId = btDepozito.Id, BirimDeger = 25000, HesaplamaYontemi = HesaplamaYontemi.Sabit, KdvOrani = 0 }
        );

        await _ctx.SaveChangesAsync();
    }

    public async Task SeedTahakkuklarAsync()
    {
        // Geriye dönük uyumluluk için (UretSozlesmeIcinAsync SeedDomainDataAsync içinde çağrılıyor)
        if (await _ctx.KiraTahakkuklar.AnyAsync()) return;
        var aktifSozlesmeler = await _ctx.Sozlesmeler.Where(s => s.Durum == SozlesmeDurumu.Aktif).ToListAsync();
        foreach (var s in aktifSozlesmeler) await _tahakkukUretim.UretSozlesmeIcinAsync(s.Id);
    }

    private async Task SeedTahakkuklarVeOdemelerAsync(List<KiraSozlesmesi> sozlesmeler)
    {
        try
        {
            var adminUser = await _ctx.Users.FirstOrDefaultAsync();
            var adminId = adminUser?.Id ?? "admin-id-missing";

            // 1. Manuel Borçlar ve İptaller
            var manuelBorcTipi = await _ctx.BorcTipleri.FirstOrDefaultAsync(b => b.Kod == "MANUEL");
            if (manuelBorcTipi != null)
            {
                var targetSozlesme = sozlesmeler.First();
                _ctx.KiraTahakkuklar.Add(new KiraTahakkuk
                {
                    KiraSozlesmesiId = targetSozlesme.Id,
                    DonemBaslangic = DateTime.Today.AddDays(-5), DonemBitis = DateTime.Today, VadeTarihi = DateTime.Today.AddDays(15),
                    BeklenenTutar = 2500m, KdvTutari = 500m, ToplamTutar = 3000m, OdenenTutar = 0m,
                    Durum = TahakkukDurumu.Bekleniyor, KaynakTipi = TahakkukKaynakTipi.Manuel, OlusturmaTarihi = DateTime.Now,
                    Kalemler = new List<TahakkukKalemi> { new TahakkukKalemi { BorcTipiId = manuelBorcTipi.Id, Aciklama = "Ekstra Temizlik Bedeli", BirimDeger = 2500m, Carpan = 1m, Tutar = 2500m, KdvOrani = 20m, KdvTutari = 500m, ToplamTutar = 3000m } }
                });

                // İptal Edilen Kayıt
                _ctx.KiraTahakkuklar.Add(new KiraTahakkuk
                {
                    KiraSozlesmesiId = targetSozlesme.Id,
                    DonemBaslangic = DateTime.Today.AddMonths(-1), DonemBitis = DateTime.Today.AddMonths(-1), VadeTarihi = DateTime.Today.AddMonths(-1),
                    BeklenenTutar = 500m, KdvTutari = 100m, ToplamTutar = 600m, OdenenTutar = 0m,
                    Durum = TahakkukDurumu.IptalEdildi, KaynakTipi = TahakkukKaynakTipi.Manuel, OlusturmaTarihi = DateTime.Now, IptalNotu = "Hatalı giriş nedeniyle iptal edildi.",
                    Kalemler = new List<TahakkukKalemi> { new TahakkukKalemi { BorcTipiId = manuelBorcTipi.Id, Aciklama = "Yanlış Borç Kaydı", BirimDeger = 500m, Tutar = 500m, KdvOrani = 20m, ToplamTutar = 600m } }
                });
            }

            await _ctx.SaveChangesAsync();

            // 2. Geçmiş Yıl Ödemeleri (%90 ve %95 oranları)
            await SeedGecmisYilOdemeleriAsync(2024, 0.90, adminId);
            await SeedGecmisYilOdemeleriAsync(2025, 0.95, adminId);

            // 3. 2026 Cari Yıl Ödemeleri
            await SeedGecmisYilOdemeleriAsync(2026, 0.60, adminId);

            // 4. Kısmi Ödemeler
            await SeedKismiOdemelerAsync(adminId);

            await _ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in SeedTahakkuklarVeOdemelerAsync: {ex.Message}");
            throw;
        }
    }

    private async Task SeedGecmisYilOdemeleriAsync(int yil, double oran, string adminId)
    {
        var query = _ctx.KiraTahakkuklar
            .Where(t => t.DonemBaslangic.Year == yil && t.Durum == TahakkukDurumu.Bekleniyor);

        // Eğer cari yıl ise (2026), sadece bugünü ve geçmiş ayları öde (Gerçekçilik için)
        if (yil == DateTime.Today.Year)
        {
            query = query.Where(t => t.DonemBaslangic <= DateTime.Today);
        }

        var tahakkuklar = await query.ToListAsync();

        if (!tahakkuklar.Any()) return;

        int odenecekAdet = (int)Math.Round(tahakkuklar.Count * oran);
        var secilenler = tahakkuklar.OrderBy(x => Guid.NewGuid()).Take(odenecekAdet).ToList();

        foreach (var t in secilenler)
        {
            bool gecikmis = Random.Shared.Next(1, 10) > 7; // %30 ihtimalle gecikmiş ödeme
            bool kismiMi = Random.Shared.Next(1, 100) <= 30; // %30 ihtimalle kısmi ödeme
            
            decimal odemeTutari = t.ToplamTutar;
            if (kismiMi)
            {
                // %25 ile %75 arasında rastgele bir tutar ödensin
                var kismiOran = Random.Shared.Next(25, 76) / 100m;
                odemeTutari = Math.Round(t.ToplamTutar * kismiOran, 2);
            }

            var odeme = new KiraOdeme
            {
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                KiraTahakkukId = t.Id,
                OdemeTarihi = gecikmis ? t.VadeTarihi.AddDays(Random.Shared.Next(15, 45)) : t.VadeTarihi.AddDays(Random.Shared.Next(-5, 5)),
                Tutar = odemeTutari,
                OdemeKanali = (OdemeKanali)Random.Shared.Next(1, 5),
                Durum = OdemeDurumu.Onaylandi,
                Aciklama = (kismiMi ? "Kısmi " : "") + (gecikmis ? "gecikmeli seed ödemesi" : "zamanında seed ödemesi"),
                GirenUserId = adminId
            };

            t.OdenenTutar = odemeTutari;
            t.Durum = kismiMi ? TahakkukDurumu.KismenOdendi : TahakkukDurumu.TamOdendi;
            _ctx.KiraOdemeler.Add(odeme);
        }
    }

    private async Task SeedKismiOdemelerAsync(string adminId)
    {
        var bekleyenler = await _ctx.KiraTahakkuklar
            .Where(t => t.Durum == TahakkukDurumu.Bekleniyor)
            .Take(3)
            .ToListAsync();

        foreach (var t in bekleyenler)
        {
            var kismiTutar = Math.Round(t.ToplamTutar / 2, 2);
            var odeme = new KiraOdeme
            {
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                KiraTahakkukId = t.Id,
                OdemeTarihi = DateTime.Today.AddDays(-2),
                Tutar = kismiTutar,
                OdemeKanali = OdemeKanali.EFT,
                Durum = OdemeDurumu.Onaylandi,
                Aciklama = "Seed kısmi ödeme",
                GirenUserId = adminId
            };
            t.OdenenTutar = kismiTutar;
            t.Durum = TahakkukDurumu.KismenOdendi;
            _ctx.KiraOdemeler.Add(odeme);
        }
    }

    private async Task SeedRezervasyonlarAsync()
    {
        var salon = await _ctx.Birimler.FirstOrDefaultAsync(b => b.Ad == "Ana Toplantı Salonu");
        var kiraci = await _ctx.Kiraciler.FirstOrDefaultAsync();
        if (salon == null || kiraci == null) return;

        // Geçmiş Rezervasyon (Tamamlandı)
        _ctx.ToplantiSalonuRezervasyonlari.Add(new ToplantiSalonuRezervasyon
        {
            BirimId = salon.Id, KiraciId = kiraci.Id,
            BaslangicTarihi = DateTime.Today.AddDays(-10).AddHours(10), BitisTarihi = DateTime.Today.AddDays(-10).AddHours(13),
            ToplamSureDakika = 180, UcretsizSureDakika = 60, UcretliSureDakika = 120, BirimUcret = 500, UcretTutar = 1000, ToplamTutar = 1200, KdvOrani = 20,
            Durum = RezervasyonDurumu.Tamamlandi, OlusturmaTarihi = DateTime.Now.AddDays(-15)
        });

        // Gelecek Rezervasyon (Planlandı)
        _ctx.ToplantiSalonuRezervasyonlari.Add(new ToplantiSalonuRezervasyon
        {
            BirimId = salon.Id, KiraciId = kiraci.Id,
            BaslangicTarihi = DateTime.Today.AddDays(3).AddHours(14), BitisTarihi = DateTime.Today.AddDays(3).AddHours(17),
            ToplamSureDakika = 180, UcretsizSureDakika = 60, UcretliSureDakika = 120, BirimUcret = 500, UcretTutar = 1000, ToplamTutar = 1200, KdvOrani = 20,
            Durum = RezervasyonDurumu.Planlandi, OlusturmaTarihi = DateTime.Now
        });

        await _ctx.SaveChangesAsync();
    }

    private async Task SeedBankaHareketleriAsync()
    {
        var admin = await _ctx.Users.FirstOrDefaultAsync();
        var adminId = admin?.Id ?? "";

        // Eşleşmiş Hareket
        _ctx.BankaHareketleri.Add(new BankaHareketi
        {
            HareketTarihi = DateTime.Today.AddDays(-1), Tutar = 1500, Aciklama = "KİRA ÖDEMESİ - TEKNOKENT",
            KarsiUnvan = "Yapay Zeka Çözümleri A.Ş.", BankaKodu = "TR01", EslesmeDurumu = BankaEslesmeDurumu.Eslesti,
            ImportBatchId = Guid.NewGuid(), ImportEdenUserId = adminId
        });

        // Eşleşmemiş (Açıkta) Hareket
        _ctx.BankaHareketleri.Add(new BankaHareketi
        {
            HareketTarihi = DateTime.Today.AddDays(-2), Tutar = 5000, Aciklama = "HAVALE - BİLİNMEYEN",
            KarsiHesap = "TR123456789...", BankaKodu = "TR01", EslesmeDurumu = BankaEslesmeDurumu.Eslestirilmedi,
            ImportBatchId = Guid.NewGuid(), ImportEdenUserId = adminId
        });

        await _ctx.SaveChangesAsync();
    }


    private static Kiraci Kiraci(string kiraciNo, KiraciTuru tur, int kategoriId, int sektorId, string ad, string? soyad = null,
        string? tcNo = null, string? vergiNo = null, string? vergiDairesi = null,
        string? ticaretSicilNo = null, string? mersisNo = null,
        string telefon = "", string email = "", string? adres = null) => new()
    {
        KiraciNo = kiraciNo, KiraciTuru = tur, KiraciKategoriId = kategoriId, SektorId = sektorId,
        Ad = ad, Soyad = soyad, TcKimlikNo = tcNo, VergiNo = vergiNo, VergiDairesi = vergiDairesi,
        TicaretSicilNo = ticaretSicilNo, MersisNo = mersisNo, Telefon = telefon, Email = email, Adres = adres,
        KayitTarihi = DateTime.Now.AddMonths(-Random.Shared.Next(6, 36))
    };

    private static KiraSozlesmesi MakeSozlesme(Birim birim, Kiraci kiraci,
        DateTime baslangic, DateTime bitis, decimal bedel, KiraPeriyodu periyot,
        decimal? depozito, bool kdv, decimal kdvOrani = 20, string? notlar = null) => new()
    {
        Birim = birim, BirimId = birim.Id, Kiraci = kiraci, KiraciId = kiraci.Id,
        BaslangicTarihi = baslangic, BitisTarihi = bitis, KiraBedeli = bedel, Periyot = periyot,
        Depozito = depozito, Notlar = notlar, Durum = SozlesmeDurumu.Aktif,
        KdvUygulanacakMi = kdv, KdvOrani = kdv ? kdvOrani : 0,
        IslemGecmisi = [new SozlesmeIslemGecmisi
        {
            IslemTipi = SozlesmeIslemTipi.Olusturma, IslemTarihi = baslangic,
            Aciklama = "Sözleşme oluşturuldu.", YeniKiraBedeli = bedel
        }]
    };
}
