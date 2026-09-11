# Faz 18 — Kısa Test Rehberi

Bu rehber rezervasyon sistemini kullanıcı gözüyle anlamak ve temel akışları denemek içindir.
Testleri gerçek üretim verisi yerine test verileriyle yapın.

## Hazırlık

İki kullanıcı gerekir:

- **Kiracı kullanıcısı:** Rezervasyon talebi oluşturur ve kendi kayıtlarını görür.
- **İç kullanıcı:** Talepleri onaylar, reddeder, düzenler ve tahakkuka aktarır.

Ayrıca rezervasyona açık bir birim ile bu birime ait ücretli veya ücretsiz bir tarife bulunmalıdır.

## Test sırası

1. **Kiracı talep oluştursun**
   - Gerekirse başka bir Firma Yetkilisiyle `/Tenant/Users` ekranından test kullanıcısını
     düzenleyin. Alternatif olarak `System.User.Edit` yetkili iç kullanıcıyla kiracı detayındaki
     kullanıcı listesine girin. “Tüm Birimlere Erişim” verin veya kullanacağı rezervasyon
     birimini seçin.
   - `/Tenant/MyReservations` ekranına girin.
   - “Talep Oluştur” ile uygun bir tarih ve saat seçin.
   - Beklenen: Kayıt **Onay Bekliyor** durumunda oluşur.

2. **İç kullanıcı talebi onaylasın**
   - `/Reservation` ekranına girip talebin detayını açın.
   - Talebi onaylayın.
   - Beklenen: Durum **Onaylandı** olur ve saat artık dolu görünür.

3. **Çakışmayı deneyin**
   - Aynı birim ve saat için başka bir talep oluşturup onaylamayı deneyin.
   - Beklenen: İkinci onay engellenir; ilk rezervasyon korunur.

4. **Ret ve iptali deneyin**
   - Yeni bir talebi gerekçe yazarak reddedin.
   - Başka bir bekleyen talebi kiracı hesabından iptal edin.
   - Beklenen: Kiracı detayında ret gerekçesi ve tarihi görünür; iç kullanıcı detayında
     bunlara ek olarak reddeden kişi görünür. Bu kayıtlar saati bloke etmez.

5. **Güncellemeyi deneyin**
   - Onaylı bir rezervasyonun saatini uygun başka bir saate taşıyın.
   - Ardından dolu bir saate taşımayı deneyin.
   - Beklenen: Uygun saat kabul edilir, çakışan saat reddedilir.

6. **Gizliliği kontrol edin**
   - Katılımcı, açıklama, not ve iç not girin.
   - Kiracı ekranından detayı açın.
   - Beklenen: Kiracı kendi bilgilerini görür; **iç notu göremez**.

7. **Tahakkuku kontrol edin**
   - Ücretli, onaylı rezervasyonu tahakkuka aktarın ve tekrar aktarmayı deneyin.
   - Beklenen: Yalnız bir tahakkuk oluşur. Ücretsiz rezervasyonda tahakkuk oluşmaz.

8. **Otomatik tamamlamayı kontrol edin**
   - Bitiş zamanı geçmiş onaylı bir test rezervasyonu hazırlayın.
   - Beklenen: Bitişten 15 dakika ve worker kontrol süresi geçince durum kalıcı olarak
     **Tamamlandı** olur.

## Test sırasında özellikle bakılacaklar

- Hata olduğunda formda girilen bilgiler kaybolmamalı.
- Başarı mesajı sayfanın üstünde, hata mesajı modalda görünmeli.
- Kiracı başka kiracının rezervasyon detayını görememeli.
- Yetkisiz kullanıcı onay, ret, düzenleme veya tahakkuk işlemi yapamamalı.
- Takvim, liste ve detay ekranındaki durumlar birbiriyle aynı olmalı.

Bir hata bulursanız kullandığınız kullanıcıyı, adresi, yaptığınız işlemi, beklenen sonucu ve
gördüğünüz sonucu birlikte kaydedin.
