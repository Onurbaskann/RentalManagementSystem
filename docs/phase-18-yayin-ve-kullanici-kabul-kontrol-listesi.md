# Faz 18 — Yayın ve Kullanıcı Kabul Kontrol Listesi

> **Durum:** Faz 18 kapatıldı — kalan işaretlenmemiş maddeler production operasyon kontrolüdür  
> **Son güncelleme:** 2026-08-17  
> **Ana plan:** [`phase-18-rezervasyon-sisteminin-genisletilmesi.md`](phase-18-rezervasyon-sisteminin-genisletilmesi.md)  
> **Aktif ilerleme:** [`PROGRESS.md`](PROGRESS.md)  
> **Kısa kullanıcı rehberi:** [`phase-18-kisa-test-rehberi.md`](phase-18-kisa-test-rehberi.md)

## 1. Otomatik doğrulama kaydı

- [x] `KiraTakip/KiraTakip.slnx` hatasız derlendi.
- [x] Tam test projesi `KiraTakipDb_Test` SQL Server bağlantısıyla geçti.
- [x] Yaşam döngüsü, validator, business-rule, permission, kapsam, tenant izolasyonu,
  uygunluk, çakışma, eşzamanlı karar, katılımcı, tahakkuk ve worker testleri geçti.
- [x] Bütün migration'lar `KiraTakipDb_Test` bağlantısından türetilen boş ve geçici
  `KiraTakipDb_Test_Phase18_*` veritabanına uygulandı.
- [x] Son migration bir önceki migration'a geri alındı, yeniden uygulandı ve geçici
  veritabanı silindi.

İncelenen, ancak Faz 18 dışında kalan uyarılar:

- `MailKit 4.9.0` için orta seviyeli güvenlik bildirimi bulunuyor. Üretim yayını öncesinde
  paket güncellemesi ayrı ve kontrollü bir bakım işi olarak ele alınmalıdır.
- Yerel EF aracının sürümü `9.0.15`, uygulama runtime sürümü `9.0.19`. Migration provası
  başarılı olsa da üretim ortamındaki EF aracı runtime ile aynı patch sürümüne getirilmelidir.

## 2. Yayın öncesi hazırlık

- [ ] Yayın tarihi, uygulayacak kişi ve geri dönüş kararını verecek kişi belirlendi.
- [ ] Mevcut uygulama dosyaları ve üretim yapılandırması yedeklendi.
- [ ] SQL Server veritabanının tam yedeği alındı ve yedeğin geri yüklenebilir olduğu doğrulandı.
- [ ] Üretim connection string'i, Hashids salt'ı ve diğer gizli değerler kaynak kod dışında
  güvenli sunucu yapılandırmasına taşındı.
- [ ] Kullanıcı kabulünde kullanılacak tenant ve iç kullanıcı hesapları hazırlandı.
- [ ] Test edilecek rezervasyon birimi, ücretli/ücretsiz tarife ve test saatleri belirlendi.

## 3. Kontrollü yayın sırası

1. Uygulama havuzunu durdurun. Böylece web istekleri ve otomatik tamamlama worker'ı kesilir.
2. Uygulama dosyalarının ve veritabanının yedeğini alın.
3. Kaynak/build ortamında idempotent migration betiğini üretin:

   ```powershell
   dotnet ef migrations script --idempotent --project KiraTakip/KiraTakip.csproj --startup-project KiraTakip/KiraTakip.csproj --output phase18-migrations.sql
   ```

4. Betiği inceleyip yedek alındıktan sonra yetkili SQL Server aracıyla üretim DB'ye uygulayın.
5. Yeni publish çıktısını sunucuya kopyalayın; üretim yapılandırmasını koruyun.
6. Uygulama havuzunu başlatın ve uygulamanın açıldığını doğrulayın.
7. Worker başlangıç logunu ve SQL erişimini kontrol edin.
8. Aşağıdaki UAT listesini tamamlayın.
9. Sonuç uygunsa yayın onayını kaydedin; değilse geri dönüş adımlarını uygulayın.

Publish klasöründe kaynak proje ve EF aracı bulunmak zorunda değildir. SQL betiğine parola
yazılmamalı, betik erişimi sınırlandırılmalı ve test connection string'i üretimde
kullanılmamalıdır.

## 4. Windows Server ve worker ayarları

`ReservationCompletionBackgroundService` web uygulamasıyla aynı process içinde çalışır.
Web process durursa otomatik tamamlama da durur. IIS kullanılıyorsa:

- [ ] Application Pool `Start Mode = AlwaysRunning` olarak ayarlandı.
- [ ] Application Pool `Idle Time-out = 0` olarak ayarlandı.
- [ ] Site/Application `Preload Enabled = True` olarak ayarlandı.
- [ ] Windows Server'da IIS Application Initialization özelliği etkinleştirildi.
- [ ] Planlı recycle zamanı biliniyor; recycle sonrasında worker başlangıç logu kontrol edildi.
- [ ] Uygulama kimliğinin publish klasörünü okuma ve gerekli log hedefini yazma yetkisi var.

Birden fazla uygulama instance'ı çalışırsa rezervasyon bazlı SQL application lock aynı kaydın
iki kez tamamlanmasını engeller. Yine de bütün instance'lar aynı üretim veritabanına ve aynı
worker ayarlarına sahip olmalıdır.

## 5. Rezervasyon yapılandırması

Rezervasyon iş kuralları `SistemAyarlari` tablosundan okunur. Worker'ın teknik çalışma
ayarları `appsettings` yerine güvenli sunucu yapılandırmasından da verilebilir. Windows ortam
değişkenlerinde bölüm ayırıcı olarak çift alt çizgi kullanılır.

| Ayar | Varsayılan | Anlamı |
|---|---:|---|
| `Reservation.CompletionGraceMinutes` (`SistemAyarlari`) | `15` | Bitişten kaç dakika sonra tamamlanacağı |
| `ReservationCompletion:Enabled` | `true` | Worker'ın çalışıp çalışmayacağı |
| `ReservationCompletion:IntervalMinutes` | `5` | Worker kontrol aralığı |
| `ReservationCompletion:BatchSize` | `50` | Bir turda işlenecek en fazla kayıt |

Örnek ortam değişkeni adları:

```text
ReservationCompletion__Enabled
ReservationCompletion__IntervalMinutes
ReservationCompletion__BatchSize
```

`IntervalMinutes` ve `BatchSize` pozitif olmalıdır; aksi halde uygulama başlangıcında ayar
hatası oluşur. Acil durumda yalnız worker'ı durdurmak için
`ReservationCompletion__Enabled=false` kullanılabilir; değişiklikten sonra process yeniden
başlatılmalıdır.

## 6. Log ve sağlık kontrolleri

Uygulama özel bir dosya log hedefi tanımlamıyor; kayıtlar sunucuda yapılandırılan ASP.NET Core
log sağlayıcısından izlenmelidir. Yayından önce kalıcı log hedefi ve saklama süresi sunucuda
netleştirilmelidir.

Aranacak worker mesajları:

- Başlangıç: `Rezervasyon otomatik tamamlama görevi başladı.`
- Devre dışı: `Rezervasyon otomatik tamamlama görevi devre dışı.`
- Başarılı işlem: `Rezervasyon otomatik tamamlama turunda ... kayıt tamamlandı.`
- Kayıt hatası: `Rezervasyon ... otomatik tamamlanamadı; sonraki periyotta yeniden denenecek.`
- Batch hatası: `Rezervasyon otomatik tamamlama batch'i başarısız oldu; sonraki periyotta yeniden denenecek.`

Bu faz özel bir health-check endpoint'i eklemez. Operasyonel sağlık şu noktalarla doğrulanır:

- [ ] Uygulama ana sayfası ve yetkili rezervasyon listesi açılıyor.
- [ ] SQL Server bağlantısı üzerinden rezervasyon listesi okunabiliyor.
- [ ] Uygulama başlangıcından sonra worker başlangıç logu görülüyor.
- [ ] Kontrollü olarak süresi dolan bir test rezervasyonu kalıcı `Completed` oluyor.
- [ ] Worker hata logu ve uygulama hata logu için sunucuda alarm/izleme sorumlusu belirlendi.

Worker boş turda bilgi logu üretmez. Bu nedenle yalnız log yokluğunu başarı kabul etmeyin;
yayın kontrolünde kontrollü bir tamamlanma kaydıyla işleyişi doğrulayın.

## 7. Geri dönüş planı

1. Yeni işlem oluşmasını engellemek için uygulama havuzunu durdurun.
2. Gerekirse worker'ı ayrıca `ReservationCompletion__Enabled=false` yapın.
3. Yeni publish dosyalarını kaldırmadan önce hata loglarını ve olay zamanını kaydedin.
4. Önceki uygulama dosyalarını ve önceki üretim yapılandırmasını geri yükleyin.
5. Veri kaybı riski nedeniyle üretimde migration `Down` komutunu ilk tercih yapmayın.
6. Şema geri dönüşü gerekiyorsa yayın öncesi alınan doğrulanmış tam DB yedeğini geri yükleyin.
7. Uygulama havuzunu başlatın; ana sayfa, SQL erişimi ve önceki ana akışları kontrol edin.

Migration geri alma provası teknik olarak başarılıdır; ancak yeni alanlara üretimde veri
yazıldıktan sonra `Down` çalıştırmak bu veriyi silebilir. Bu nedenle gerçek geri dönüşün ana
güvencesi veritabanı yedeğidir.

## 8. Manuel kullanıcı kabul senaryoları

Her satırda kullanılan kullanıcı, birim/rezervasyon, sonuç ve varsa ekran görüntüsü kaydedilir.

- [ ] Tenant `/Tenant/MyReservations/Calendar` ekranında kapsamındaki uygun slotu görür.
- [ ] Tenant `/Tenant/MyReservations/Create` ekranında talep oluşturur; validasyon hatasında
  girdiği değerler kaybolmaz ve başarı merkezi sayfa alanında görünür.
- [ ] Tenant kapsam dışı birimi göremez; doğrudan URL denemesi de engellenir.
- [ ] Pending talep slotu bloke etmez; aynı saat için ikinci pending talep oluşturulabilir.
- [ ] İç kullanıcı `/Reservation` ekranında talebi görür, detayını inceler ve onaylar.
- [ ] Aynı slot için iki pending talepten yalnız biri onaylanabilir.
- [ ] İç kullanıcı talebi Türkçe gerekçeyle reddeder; tenant güvenli ret bilgisini görür.
- [ ] Tenant pending talebi iptal eder ve uygun başka bir talep oluşturabilir.
- [ ] Yetkili iç kullanıcı create+approve işlemini yapar; yetkisiz kullanıcı yapamaz.
- [ ] Onaylı rezervasyon izin verilen sürede güncellenir; çakışan saat reddedilir.
- [ ] 120 dakika sınırındaki güncelleme/iptal normal kullanıcı için engellenir; yetkili istisna
  gerekçe ile uygulanır.
- [ ] Katılımcılar ve paylaşılan not doğru görünür; `InternalNotes` tenant ekranında görünmez.
- [ ] Ücretli onaylı rezervasyon tek tahakkuk üretir; ikinci aktarım engellenir.
- [ ] Ücretsiz rezervasyon için tahakkuk oluşmaz.
- [ ] Onaylı ödemesi olan tahakkuka bağlı rezervasyon iptali engellenir.
- [ ] Ödenmemiş tahakkuka bağlı rezervasyon iptalinde finansal kayıt doğru duruma geçer.
- [ ] Bitiş + 15 dakika sonrasında worker rezervasyonu kalıcı `Completed` yapar.
- [ ] Uygulama recycle edildikten sonra worker tekrar başlar ve aynı kaydı ikinci kez işlemez.

## 9. Kapanış kaydı

- [ ] Kritik veya yüksek riskli açık hata kalmadı.
- [ ] Orta seviye paket uyarısı için kabul veya ayrı bakım görevi kaydedildi.
- [ ] Manuel UAT maddeleri tamamlandı.
- [ ] Windows Server worker koşulları gerçek sunucuda doğrulandı.
- [ ] Yayın/geri dönüş sorumluları sonucu onayladı.
- [x] Kullanıcı Faz 18 kapanışını 2026-08-17 tarihinde açıkça onayladı.

Kapanış notu: Production migrationları uygulanmış ve mevcut rezervasyon kayıtlarının korunduğu
doğrulanmıştır. İşaretlenmemiş yayın ve Windows Server maddeleri Faz 18 kapsamını yeniden açmaz;
production devreye alma sırasında operasyonel kontrol olarak yürütülür.
