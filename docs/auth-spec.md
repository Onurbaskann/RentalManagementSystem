# Authentication & Authorization Spec

> **GÜNCELLİK NOTU:** Bu dosya tarihsel gelişim sürecini içerir. Güncel mimari kararlar için **MASTER-PLAN.md** ve **PROGRESS.md** dosyaları esas alınmalıdır; **PROGRESS-HISTORY.md** tarihsel arşivdir.
> - SQLite yerine **SQL Server** kullanılmaktadır.
> - DummyDataService yerine **EF Core + SeedDataService** kullanılmaktadır.
> - Role-based yetkilendirme yerine **Claims-based Permission** altyapısı (Faz 4+) geçerlidir.

## Amaç

Uygulamaya giriş yapmadan ana sayfalara erişilememelidir.

Korunacak alanlar:
- Dashboard
- Taşınmazlar
- Kiracılar
- Sözleşmeler

Anonim erişime açık alanlar:
- Login
- Logout
- AccessDenied

## Teknoloji

- ASP.NET Core Identity
- EF Core
- SQLite
- Role-based authorization

Domain verileri şimdilik DummyDataService içinde kalacaktır.
Sadece kullanıcı, rol ve login verileri SQLite üzerinde tutulacaktır.

## Roller

### Admin
Tüm yetkilere sahiptir.

### Yonetici
Taşınmaz, kiracı ve sözleşme işlemlerini yapabilir.
Kullanıcı yönetimi yapamaz.

### Goruntuleyici
Sadece listeleme ve detay sayfalarını görebilir.
Ekleme, düzenleme ve silme işlemleri yapamaz.

## Seed Kullanıcılar

| Email | Şifre | Rol |
|---|---|---|
| admin@kiratakip.local | Admin123! | Admin |
| yonetici@kiratakip.local | Yonetici123! | Yonetici |
| viewer@kiratakip.local | Viewer123! | Goruntuleyici |

## UI Kuralları

- Login sayfası Bootstrap Identity UI gibi görünmemeli.
- Mevcut Tailwind/Fraunces/Inter tasarım dili korunmalı.
- Sidebar kullanıcı kartı gerçek kullanıcı bilgisini göstermeli.
- Logout kullanıcı menüsünde yer almalı.
- Yetkisiz kullanıcılara işlem butonları gösterilmemeli.

## Yetki Kuralları

| İşlem | Admin | Yonetici | Goruntuleyici |
|---|---:|---:|---:|
| Dashboard görüntüleme | Evet | Evet | Evet |
| Taşınmaz liste/detay | Evet | Evet | Evet |
| Taşınmaz ekleme | Evet | Evet | Hayır |
| Kiracı liste/detay | Evet | Evet | Evet |
| Kiracı ekleme | Evet | Evet | Hayır |
| Sözleşme liste/detay | Evet | Evet | Evet |
| Sözleşme ekleme | Evet | Evet | Hayır |
| Sözleşme sonlandırma | Evet | Hayır | Hayır |
| Kullanıcı yönetimi | Evet | Hayır | Hayır |

## Değiştirilecek Dosyalar

- Program.cs
- appsettings.json
- Controllers/HomeController.cs
- Controllers/TasinmazController.cs
- Controllers/KiraciController.cs
- Controllers/SozlesmeController.cs
- Views/Shared/_Layout.cshtml
- Views/Shared/_Sidebar.cshtml

## Yeni Dosyalar

- Data/ApplicationDbContext.cs
- Models/ApplicationUser.cs
- Infrastructure/Seeding/IdentitySeedService.cs
- Controllers/AccountController.cs
- Views/Account/Login.cshtml
- Views/Account/AccessDenied.cshtml

## Kabul Kriterleri

- Giriş yapmadan dashboard açılamaz.
- Login başarılı olunca dashboard’a yönlenir.
- Logout çalışır.
- Admin tüm işlemleri yapabilir.
- Yonetici kullanıcı yönetimine erişemez.
- Goruntuleyici ekleme/düzenleme/silme butonlarını göremez.
- Yetkisiz route erişiminde AccessDenied sayfası açılır.
- Mevcut DummyDataService yapısı bozulmaz.
- Mevcut UI tasarım dili korunur.

## Admin Kullanıcı Yönetimi

Admin rolündeki kullanıcılar sistem kullanıcılarını yönetebilmelidir.

### Amaç

Admin kullanıcı, uygulamaya yeni kullanıcı ekleyebilmeli, mevcut kullanıcıların rollerini güncelleyebilmeli ve kullanıcıları pasif hale getirebilmelidir.

Public register sayfası bulunmayacaktır. Kullanıcı oluşturma işlemi sadece Admin panelinden yapılacaktır.

### Kullanıcı Durumu

ApplicationUser modeline kullanıcı aktiflik durumu eklenecektir:

- `IsActive: bool`
- Varsayılan değer: `true`

Pasif kullanıcılar sisteme giriş yapamamalıdır.

### Admin Yetkileri

Admin aşağıdaki işlemleri yapabilir:

- Kullanıcıları listeleme
- Yeni kullanıcı oluşturma
- Kullanıcıya rol atama
- Mevcut kullanıcının rolünü değiştirme
- Kullanıcıyı pasif hale getirme
- Pasif kullanıcıyı tekrar aktif hale getirme

### Kısıtlar

- Admin kendi hesabını pasif hale getirememelidir.
- Admin kendi rolünü değiştirememelidir.
- Sistemde en az bir aktif Admin kalmalıdır.
- Public register sayfası eklenmemelidir.
- Kullanıcı silme fiziksel delete olarak yapılmamalıdır; bunun yerine kullanıcı pasif hale getirilmelidir.

### Roller

Kullanıcıya atanabilecek roller:

- Admin
- Yonetici
- Goruntuleyici

Bir kullanıcı başlangıç için tek role sahip olacaktır.

### Route Yapısı

Admin kullanıcı yönetimi için aşağıdaki route’lar oluşturulacaktır:

| Controller | Action | Route | Açıklama |
|---|---|---|---|
| `AdminUserController` | `Index` | `/Admin/Kullanicilar` | Kullanıcı listesi |
| `AdminUserController` | `Create` | `/Admin/Kullanicilar/Ekle` | Yeni kullanıcı oluşturma |
| `AdminUserController` | `Edit` | `/Admin/Kullanicilar/Duzenle/{id}` | Kullanıcı rol/durum güncelleme |
| `AdminUserController` | `ToggleActive` | `/Admin/Kullanicilar/DurumDegistir/{id}` | Aktif/pasif değiştirme |

Tüm Admin kullanıcı yönetimi action’ları sadece `Admin` rolüne açık olmalıdır.

```csharp
[Authorize(Roles = "Admin")]

---

## Taşınmaz Bazlı Görüntüleme Yetkisi

Goruntuleyici rolündeki kullanıcılar artık sistemdeki tüm taşınmazları göremez.

Admin, her Goruntuleyici kullanıcı için hangi taşınmazları görebileceğini belirleyebilmelidir.

---

### Amaç

Goruntuleyici rolündeki kullanıcı sadece kendisine atanmış taşınmazları görebilmelidir.

Bu taşınmazlara bağlı aşağıdaki veriler de görüntülenebilir olmalıdır:

- Birimler
- Kiracılar
- Sözleşmeler
- Dashboard istatistikleri

Goruntuleyici kullanıcı kendisine atanmamış taşınmazlara, bu taşınmazlara bağlı kiracılara ve sözleşmelere erişememelidir.

---

### Yetki Mantığı

| Rol | Taşınmaz Görme Kapsamı |
|---|---|
| Admin | Tüm taşınmazlar |
| Yonetici | Tüm taşınmazlar |
| Goruntuleyici | Sadece kendisine atanmış taşınmazlar |

Admin ve Yonetici rolleri mevcut davranışını korur.

Goruntuleyici rolü ise taşınmaz bazlı filtrelenir.

---

### Veri Modeli

Kullanıcı-taşınmaz yetkisi Identity/SQLite tarafında tutulacaktır.

Domain verileri yine `DummyDataService` içinde kalacaktır.

Yeni model oluşturulacaktır:

```csharp
public class UserTasinmazYetki
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int TasinmazId { get; set; }

    public DateTime AtanmaTarihi { get; set; }

    public string? AtayanUserId { get; set; }
}
```

`ApplicationDbContext` içine eklenecektir:

```csharp
public DbSet<UserTasinmazYetki> UserTasinmazYetkileri { get; set; }
```

---

### Admin Kullanıcı Yönetimi Güncellemesi

Admin kullanıcı düzenleme ekranında, Goruntuleyici rolündeki kullanıcılar için taşınmaz atama alanı bulunmalıdır.

Bu alan sadece kullanıcı rolü `Goruntuleyici` ise gösterilir.

Örnek UI:

```text
Taşınmaz Görüntüleme Yetkileri

[ ] Teknokent A Blok
[ ] Çamlık Kantini
[ ] Bornova Tarlası
[ ] Sanayi Sitesi B Blok
[ ] Buca Deposu
```

Kurallar:

- Bu bölüm sadece Goruntuleyici kullanıcılar için gösterilir.
- Admin ve Yonetici kullanıcılar için taşınmaz ataması gerekmez.
- Goruntuleyici kullanıcıya bir veya birden fazla taşınmaz atanabilir.
- Hiç taşınmaz atanmamış Goruntuleyici kullanıcı boş liste görmelidir.
- Admin, mevcut atamaları güncelleyebilmelidir.
- Atama değiştiğinde eski yetkiler kaldırılıp yeni seçilen taşınmazlar kaydedilebilir.

---

### Servis Yapısı

Taşınmaz bazlı yetki kontrolleri için ayrı bir servis oluşturulacaktır.

Önerilen servis:

```csharp
public class UserTasinmazYetkiService
{
    public Task<List<int>> GetYetkiliTasinmazIdsAsync(string userId);

    public Task<bool> CanViewTasinmazAsync(string userId, int tasinmazId);

    public Task SetUserTasinmazYetkileriAsync(
        string userId,
        List<int> tasinmazIds,
        string atayanUserId);
}
```

Bu servis:

- Kullanıcının yetkili olduğu taşınmaz ID listesini döndürür.
- Belirli bir taşınmazı görüp göremeyeceğini kontrol eder.
- Admin tarafından yapılan taşınmaz yetki atamalarını kaydeder.

---

### Dashboard Filtreleme

Goruntuleyici kullanıcı dashboard’da sadece kendisine atanmış taşınmazların istatistiklerini görmelidir.

Dashboard metrikleri şu kapsamda hesaplanır:

- Atanmış taşınmazlar
- Bu taşınmazlara bağlı birimler
- Bu birimlere bağlı sözleşmeler
- Bu sözleşmelerin kiracıları
- Bu sözleşmelerden gelen gelirler

Admin ve Yonetici dashboard’da tüm verileri görmeye devam eder.

---

### Taşınmaz Listeleme

`/Tasinmaz` sayfası rol bazlı filtrelenmelidir.

Kurallar:

- Admin tüm taşınmazları görür.
- Yonetici tüm taşınmazları görür.
- Goruntuleyici sadece kendisine atanmış taşınmazları görür.

Goruntuleyici için atanmış taşınmaz yoksa empty state gösterilmelidir.

---

### Taşınmaz Detay Yetkisi

`/Tasinmaz/Detay/{id}` sayfasında doğrudan URL erişimi kontrol edilmelidir.

Kurallar:

- Admin tüm taşınmaz detaylarına erişebilir.
- Yonetici tüm taşınmaz detaylarına erişebilir.
- Goruntuleyici yalnızca kendisine atanmış taşınmaz detayına erişebilir.
- Goruntuleyici atanmadığı taşınmaz detayına erişmeye çalışırsa AccessDenied sayfasına yönlendirilmelidir.

Örnek kontrol:

```csharp
if (User.IsInRole("Goruntuleyici"))
{
    var userId = _userManager.GetUserId(User);
    var canView = await _yetkiService.CanViewTasinmazAsync(userId, tasinmazId);

    if (!canView)
    {
        return Forbid();
    }
}
```

---

### Kiracı Listeleme

Goruntuleyici kullanıcı sadece kendisine atanmış taşınmazlarla ilişkili kiracıları görebilmelidir.

Kiracı görülebilirlik mantığı:

```text
Kiracının herhangi bir sözleşmesi var mı?
→ Sözleşme hangi birime bağlı?
→ Birim hangi taşınmaza bağlı?
→ Bu taşınmaz Goruntuleyici kullanıcıya atanmış mı?
```

Kurallar:

- Admin tüm kiracıları görür.
- Yonetici tüm kiracıları görür.
- Goruntuleyici sadece atanmış taşınmazlara bağlı sözleşmesi olan kiracıları görür.
- Atanmamış taşınmazlara ait kiracılar listelenmez.

---

### Kiracı Detay Yetkisi

`/Kiraci/Detay/{id}` sayfasında doğrudan URL erişimi kontrol edilmelidir.

Goruntuleyici bir kiracı detayını sadece şu durumda görebilir:

- Kiracının en az bir sözleşmesi vardır.
- Bu sözleşmenin bağlı olduğu birim, Goruntuleyici kullanıcıya atanmış bir taşınmaza aittir.

Aksi durumda AccessDenied gösterilmelidir.

---

### Sözleşme Listeleme

Goruntuleyici kullanıcı sadece kendisine atanmış taşınmazlara bağlı sözleşmeleri görebilmelidir.

Sözleşme görülebilirlik mantığı:

```text
Sözleşme hangi birime bağlı?
→ Birim hangi taşınmaza bağlı?
→ Bu taşınmaz Goruntuleyici kullanıcıya atanmış mı?
```

Kurallar:

- Admin tüm sözleşmeleri görür.
- Yonetici tüm sözleşmeleri görür.
- Goruntuleyici sadece atanmış taşınmazlara bağlı sözleşmeleri görür.

---

### Sözleşme Detay Yetkisi

`/Sozlesme/Detay/{id}` sayfasında doğrudan URL erişimi kontrol edilmelidir.

Goruntuleyici bir sözleşme detayını sadece şu durumda görebilir:

- Sözleşmenin bağlı olduğu birim, kendisine atanmış bir taşınmaza aittir.

Aksi durumda AccessDenied gösterilmelidir.

---

### Controller Etkileri

Aşağıdaki controller’larda filtreleme ve detay erişim kontrolleri yapılmalıdır:

- `HomeController`
- `TasinmazController`
- `KiraciController`
- `SozlesmeController`
- `AdminUserController`

---

### AdminUserController Güncellemesi

Admin kullanıcı düzenleme action’ı, Goruntuleyici kullanıcılar için taşınmaz yetkilerini de yönetmelidir.

Güncellenecek action’lar:

| Controller | Action | Amaç |
|---|---|---|
| `AdminUserController` | `Edit` GET | Kullanıcının mevcut rolünü ve taşınmaz yetkilerini gösterir |
| `AdminUserController` | `Edit` POST | Rol ve taşınmaz yetkilerini günceller |
| `AdminUserController` | `Create` POST | Yeni Goruntuleyici oluşturuluyorsa taşınmaz yetkilerini kaydeder |

---

### ViewModel Güncellemesi

Admin kullanıcı oluşturma/düzenleme ViewModel’lerine taşınmaz yetki alanları eklenmelidir.

Örnek:

```csharp
public class AdminUserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public List<int> SelectedTasinmazIds { get; set; } = new();

    public List<TasinmazYetkiCheckboxViewModel> Tasinmazlar { get; set; } = new();
}
```

```csharp
public class TasinmazYetkiCheckboxViewModel
{
    public int TasinmazId { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string Konum { get; set; } = string.Empty;

    public bool Selected { get; set; }
}
```

---

### UI Kuralları

Admin kullanıcı yönetimi ekranında:

- Kullanıcı rolü Goruntuleyici ise taşınmaz yetki alanı görünür.
- Rol Admin veya Yonetici ise taşınmaz yetki alanı gizlenir.
- Taşınmazlar checkbox listesi olarak gösterilebilir.
- Taşınmaz adı, il/ilçe bilgisiyle gösterilmelidir.
- UI mevcut Tailwind/Magic premium tasarım diliyle uyumlu olmalıdır.
- Bootstrap veya generic Identity UI görünümü kullanılmamalıdır.

Goruntuleyici kullanıcı arayüzünde:

- Sadece atanmış taşınmazlar görünür.
- Sadece atanmış taşınmazlara bağlı kiracılar görünür.
- Sadece atanmış taşınmazlara bağlı sözleşmeler görünür.
- Ekleme, düzenleme, silme, fesih, süre uzatma gibi işlem butonları görünmez.

---

### Migration Gereksinimi

`UserTasinmazYetki` modeli Identity/SQLite tarafında tutulacağı için migration gereklidir.

Örnek komutlar:

```bash
dotnet ef migrations add AddUserTasinmazYetki
dotnet ef database update
```

---

### Kabul Kriterleri

- [ ] Admin, Goruntuleyici kullanıcıya taşınmaz atayabilir.
- [ ] Admin, Goruntuleyici kullanıcının taşınmaz yetkilerini güncelleyebilir.
- [ ] Admin ve Yonetici tüm taşınmazları görmeye devam eder.
- [ ] Goruntuleyici sadece kendisine atanmış taşınmazları görür.
- [ ] Goruntuleyici atanmadığı taşınmaz detayına URL ile erişemez.
- [ ] Goruntuleyici sadece atanmış taşınmazlara bağlı kiracıları görür.
- [ ] Goruntuleyici atanmadığı taşınmaza ait kiracı detayına URL ile erişemez.
- [ ] Goruntuleyici sadece atanmış taşınmazlara bağlı sözleşmeleri görür.
- [ ] Goruntuleyici atanmadığı taşınmaza ait sözleşme detayına URL ile erişemez.
- [ ] Goruntuleyici dashboard’da sadece atanmış taşınmazlara ait metrikleri görür.
- [ ] Hiç taşınmaz atanmamış Goruntuleyici boş liste/empty state görür.
- [ ] Admin kullanıcı düzenleme ekranında Goruntuleyici için taşınmaz checkbox listesi görünür.
- [ ] Admin/Yonetici kullanıcı düzenlenirken taşınmaz checkbox listesi gizlenir.
- [ ] Kullanıcı-taşınmaz yetkileri Identity/SQLite tarafında tutulur.
- [ ] Domain verileri `DummyDataService` içinde kalır.
- [ ] Mevcut Authentication & Authorization yapısı bozulmaz.
- [ ] Mevcut Tailwind/Magic UI tasarım dili korunur.

---

### Claude Code İçin Kısa Görev Prompt’u

```text
docs/auth-spec.md dosyasındaki “Taşınmaz Bazlı Görüntüleme Yetkisi” bölümünü oku ve uygula.

Token verimliliği için:
- project-spec-canonical.md dosyasını baştan sona okuma.
- Gerekirse sadece controller yapısı, servis yapısı ve tasarım sistemi bölümlerine bak.
- kiraci-sozlesme-finans-spec.md ve bina-ofis-birim-spec.md dosyalarına dokunma.
- DummyDataService domain verilerini EF Core’a taşıma.
- Mevcut Identity/Auth yapısını bozma.

Görev:
Goruntuleyici rolündeki kullanıcılar artık tüm verileri değil, sadece Admin tarafından kendilerine atanmış taşınmazları ve bu taşınmazlara bağlı kiracı/sözleşmeleri görebilsin.

Beklentiler:
- UserTasinmazYetki modelini oluştur.
- ApplicationDbContext içine DbSet ekle.
- UserTasinmazYetkiService oluştur.
- Admin kullanıcı düzenleme ekranına Goruntuleyici kullanıcılar için taşınmaz atama checkbox listesi ekle.
- Admin, Goruntuleyici kullanıcının taşınmaz yetkilerini güncelleyebilsin.
- Goruntuleyici için Dashboard, Taşınmaz, Kiracı ve Sözleşme verilerini atanmış taşınmazlara göre filtrele.
- Detay sayfalarında URL ile yetkisiz erişimi engelle.
- Admin ve Yonetici tüm verileri görmeye devam etsin.
- Goruntuleyici ekleme/düzenleme/silme/fesih/süre uzatma butonlarını görmesin.
- Mevcut Tailwind/Magic UI tasarım dili korunsun.

İş sonunda kısa özet ver:
- Hangi modeller eklendi?
- Hangi controller/service/view dosyaları değişti?
- Goruntuleyici filtreleme mantığı nasıl çalışıyor?
- Migration/database için hangi komutlar gerekli?
- Smoke test için hangi senaryolar kontrol edilmeli?
```
