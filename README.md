# 🏢 Rental Management System (Kira Takip Sistemi)

Rental Management System, gayrimenkul portföylerini, kiracı ilişkilerini ve kira sözleşmelerini dijital ortamda yönetmek için geliştirilmiş modern bir web uygulamasıdır. .NET 8 MVC mimarisi üzerine inşa edilen bu proje, hem bireysel hem de kurumsal taşınmaz yönetim ihtiyaçlarını karşılamak üzere tasarlanmıştır.

## 🚀 Öne Çıkan Özellikler

- **Gelişmiş Dashboard:** Toplam gelir, doluluk oranları, bekleyen sözleşmeler ve süresi dolmak üzere olan kira kontratları için gerçek zamanlı istatistikler.
- **Taşınmaz ve Birim Yönetimi:** 
    - Bina, Arazi, Tarla ve Depo gibi farklı taşınmaz tipleri.
    - Bina bazlı ofis/birim yönetimi (Kat ve ofis no takibi).
    - Taşınmaz bazlı metrekare ve konum bilgileri.
- **Kiracı Yönetimi:**
    - Gerçek ve Tüzel (Firma) kişi desteği.
    - Kimlik bilgileri, vergi dairesi ve iletişim bilgileri takibi.
- **Sözleşme ve Finans Yönetimi:**
    - Dinamik kira periyotları (Aylık/Yıllık).
    - TÜFE bazlı kira artış hesaplamaları.
    - KDV dahil/hariç hesaplama seçenekleri.
    - Depozito ve sözleşme geçmişi takibi.
- **Güvenlik ve Yetkilendirme:**
    - **ASP.NET Core Identity** tabanlı kullanıcı yönetimi.
    - **Taşınmaz Bazlı Görüntüleme Yetkisi:** "Görüntüleyici" rolündeki kullanıcılara sadece belirli taşınmazlar için veri erişimi kısıtlaması (Row-Level Authorization).

## 🛠 Kullanılan Teknolojiler

- **Backend:** .NET 8 (ASP.NET Core MVC)
- **Database:** Entity Framework Core & SQLite
- **Security:** ASP.NET Core Identity
- **Frontend:** 
    - Vanilla CSS (Özel Tasarım Sistemi)
    - Alpine.js (Dinamik UI Bileşenleri)
    - Lucide Icons & Google Fonts
- **Kütüphaneler:** ApexCharts (Grafikler)

## 📦 Kurulum

Projeyi yerel ortamınızda çalıştırmak için aşağıdaki adımları izleyebilirsiniz:

1. Repoyu klonlayın:
   ```bash
   git clone https://github.com/RentalManagementSystem.git
   ```
2. Proje dizinine gidin:
   ```bash
   cd KiraTakip
   ```
3. Bağımlılıkları yükleyin:
   ```bash
   dotnet restore
   ```
4. Veritabanını güncelleyin:
   ```bash
   dotnet ef database update
   ```
5. Uygulamayı çalıştırın:
   ```bash
   dotnet run
   ```
