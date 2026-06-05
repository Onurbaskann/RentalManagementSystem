# 🏢 Rental Management System (Kira Takip Sistemi)

Rental Management System, gayrimenkul portföylerini, kiracı ilişkilerini ve kira sözleşmelerini dijital ortamda yönetmek için geliştirilmiş modern bir web uygulamasıdır. .NET 8 MVC mimarisi üzerine inşa edilen bu proje, hem bireysel hem de kurumsal taşınmaz yönetim ihtiyaçlarını karşılamak üzere tasarlanmıştır.

## 🚀 Öne Çıkan Özellikler

- **Gelişmiş Dashboard:** Toplam gelir, doluluk oranları, bekleyen sözleşmeler, manuel borç ve rezervasyon metrikleri için gerçek zamanlı istatistikler.
- **Taşınmaz ve Birim Yönetimi:**
    - Bina, Arazi, Tarla, Depo, Otomat, Bankamatik, Kantin gibi yapılandırılabilir taşınmaz tipleri.
    - Birim bazlı yönetim (kat, birim no, birim türü takibi).
    - Taşınmaz bazlı metrekare ve konum bilgileri.
- **Kiracı Yönetimi:**
    - Gerçek ve Tüzel (Firma) kişi desteği.
    - Kimlik, vergi dairesi, kategori, sektör ve iletişim bilgileri takibi.
    - KVKK onayı kaydı.
- **Sözleşme ve Çok Kalemli Tahakkuk:**
    - Dinamik kira periyotları; pro-rata ilk ay hesabı.
    - Borç tipi bazlı çok kalemli aylık tahakkuk (Kira, Ortak Gider, Portal vb.).
    - Taşınmaz × Kiracı Kategorisi bazlı dinamik fiyatlandırma matrisi.
    - KDV dahil/hariç hesaplama seçenekleri.
    - Sözleşme uzatma / fesih / yeniden üretim akışları ve işlem geçmişi.
- **Ödeme Takip ve Banka Eşleştirme:**
    - Manuel ödeme kaydı ve dekont yönetimi.
    - CSV formatında banka hareketi import ve otomatik eşleştirme.
    - Manuel borç oluşturma ve iptal akışı.
- **Toplantı Salonu Rezervasyonu:**
    - Çakışma kontrolü ile rezervasyon oluşturma ve iptal.
    - Birim türü bazlı ücret kuralları; rezervasyondan tahakkuka otomatik aktarım.
- **Mail Bildirim ve Ödeme Portalı:**
    - Kiracılara toplu borç hatırlatma maili (MailKit / SMTP).
    - HMAC imzalı, kiracıya özel ödeme portalı linki.
- **Güvenlik ve Yetkilendirme:**
    - ASP.NET Core Identity tabanlı kullanıcı yönetimi.
    - Rol (Admin / Yönetici / Görüntüleyici) + Permission katmanlı yetki sistemi.
    - Taşınmaz bazlı satır düzeyi erişim kısıtlaması (Row-Level Authorization).

## 🛠 Kullanılan Teknolojiler

- **Backend:** .NET 9.0 (ASP.NET Core MVC)
- **Database:** Entity Framework Core & SQL Server
- **Security:** ASP.NET Core Identity + Claims tabanlı yetki sistemi
- **Frontend:**
    - Tailwind CSS
    - Alpine.js (Dinamik UI Bileşenleri)
- **Test:** xUnit (.NET 10.0)

## 📦 Kurulum

Projeyi yerel ortamınızda çalıştırmak için aşağıdaki adımları izleyebilirsiniz:

1. Proje dizinine gidin:
   ```bash
   cd KiraTakip
   ```
2. Bağımlılıkları yükleyin:
   ```bash
   dotnet restore KiraTakip/KiraTakip.csproj
   ```
3. `appsettings.json` dosyasındaki `DefaultConnection` bağlantı dizesini kendi SQL Server örneğinize göre düzenleyin.
4. Veritabanını güncelleyin:
   ```bash
   dotnet ef database update --project KiraTakip --startup-project KiraTakip
   ```
5. Uygulamayı çalıştırın:
   ```bash
   dotnet run --project KiraTakip
   ```
