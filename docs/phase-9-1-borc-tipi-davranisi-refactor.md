# BorcTipiDavranisi Refactor

## Sorun
`ManuelTetiklemeli(3)` hem kullanıcı girişini hem rezervasyon akışını kapsıyor → çift kayıt riski + hard-coded `Kod="TOPLANTI"` bağımlılığı.

## Değişiklik

### Enum (`Models/Enums.cs`)
```
AylikSabit        = 1  // otomatik, her ay          — değişmez
IlkAyTekSeferlik  = 2  // otomatik, sözleşme başı   — değişmez
KullaniciManuel   = 3  // kullanıcı UI → ManuelBorc  — rename (int korundu)
RezervasyonOzel   = 4  // yalnızca rezervasyon akışı — YENİ
```

### Seed (`SeedDataService.cs`)
| Kod | Eski Davranis | Yeni Davranis | Sistem |
|---|---|---|---|
| MANUEL | ManuelTetiklemeli(3) | KullaniciManuel(3) | false |
| TOPLANTI | ManuelTetiklemeli(3) | RezervasyonOzel(4) | **true** |

### Migration SQL
```sql
UPDATE BorcTipleri SET Davranis = 4, Sistem = 1 WHERE Kod = 'TOPLANTI';
```

## Güncellenmesi Gereken Dosyalar

### Filtre değişmeyenler (sadece rename etkisi)
`!= ManuelTetiklemeli` → `!= KullaniciManuel && != RezervasyonOzel`
- `Services/TahakkukUretimService.cs` × 2
- `Services/TasinmazFiyatService.cs`
- `Services/TarifeHiyerarsiService.cs`
- `Controllers/AdminTarifeController.cs` × 2
- `Controllers/BirimController.cs`
- `Controllers/SozlesmeController.cs`
- `Services/SeedDataService.cs` (tahakkuk üretim kısmı)

### Kritik değişenler
| Dosya | Eski | Yeni |
|---|---|---|
| `Services/ManuelBorcService.cs` | BorcTipi filtresiz | `Davranis == KullaniciManuel` |
| `Controllers/ManuelBorcController.cs` | `Where(b => b.Aktif)` | + `&& b.Davranis == KullaniciManuel` |
| `Services/RezervasyonService.cs` | `b.Kod == "TOPLANTI"` | `b.Davranis == RezervasyonOzel && b.Aktif` |
| `Services/SeedDataService.cs` (seed) | `TOPLANTI: Sistem=false` | `Sistem=true`, `Davranis=RezervasyonOzel` |

### View/switch güncellemeleri
- `Views/AdminBorcTipi/Index.cshtml` — switch label + filtre butonu
- `Views/AdminBorcTipi/Edit.cshtml` — `<option>` metni
- `Views/AdminBorcTipi/Create.cshtml` — `<option>` metni

## Uygulama Sırası
1. `Enums.cs` — enum genişlet
2. `SeedDataService.cs` — TOPLANTI satırı güncelle
3. Migration oluştur + SQL uygula
4. Tüm `!= ManuelTetiklemeli` → çift filtre (derleyici hataları kılavuz olur)
5. `ManuelBorcController` + `ManuelBorcService` — `KullaniciManuel` filtresi
6. `RezervasyonService` — hard-coded Kod araması kaldır
7. View switch/option metinleri
