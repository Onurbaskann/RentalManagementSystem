# Açık Ofis v2 — Yeni Oturum Devir Notu (2026-11-18)

## Durum
- "Paylaşımlı ve açık birim" refactor'ü ([uygulama planı](refactor-paylasimli-acik-birim-uygulama-plani.md)) F1–F9 tamamlandı.
- F9 değişiklikleri **commit edilmedi**:
  - Uzatmada birim kontrolü (`Lease.UnitRentalModeChanged`, `Lease.UnitAreaChanged`)
  - Plan, PROGRESS ve kaynak doküman güncellemeleri
- F9 madde 6 (açık notların değerlendirilmesi) yarım kaldı. Alınan kararlar:
  - **Not 1:** Grup sözleşmesinde `Lease/UpdateDueDate` kapatılacak (buton gizle + servis guard).
  - **Not 2:** "Dolmak üzere" eşiği tüm ekranlarda `LeaseExpiringSoonStatusDays` ayarını okuyacak.
  - **Not 3:** `Property/Details.cshtml` birim filtresine "Kısmen Dolu" seçeneği eklenecek.
    - Yan bulgu: `:307` ve `:517`'de refactor öncesinden kalma `filter= = ='bos'` bozuk sözdizimi var, kullanıcıya sorulacak.
  - **Not 4:** Değişiklik yok.
  - **Not 5 ve 6:** Açık ofis v2 işine devredildi.
  - **Not 7–14:** Henüz konuşulmadı.
- Notların 1–3. kararları henüz uygulanmadı.

## Asıl yeni iş: Açık ofis yapısının yeniden kurulması
**Tespit edilen tasarım hatası:**
- Refactor'de fiziksel m² (F, `LeaseArea.AllocatedArea`) sözleşmede sabit bırakıldı.
- Ürün sahibi en başından beri F'nin kontenjan değişince değişebilmesini istiyordu.
- Ayrıca zorunlu "boş kontenjan başına ≥ Min alan" kontrolü (`UnitCapacityPolicy.GetViolations` → `NormBelowMinimum`) dolu bir açık ofiste kontenjan artışını tamamen engelliyor.

**Belgeler (kullanıcının Downloads klasöründe, okuma sırası önemli):**
1. `C:\Users\onur.baskan\Downloads\teknopark_acik_ofis_v2_karar_ozeti_ve_claude_devir_dokumani.md` — **GÜNCEL ve esas belge.** Kesin iş kuralları, S-1…S-7 / K-1…K-6 kararları, T01–T18 senaryoları ve 13. bölümde istenen inceleme görevi bu belgede.
2. `C:\Users\onur.baskan\Downloads\teknopark_acik_ofis_v2_degerlendirme_ve_kararlar.md` — Claude'un önceki değerlendirmesi (mevcut kod envanteri, dosya/satır referanslı).
3. `teknopark_acik_ofis_guncel_kararlar_ve_optimizasyon_v2.md` ve `teknopark_acik_ofis_fiziksel_alan_optimizasyonu_analiz.md` — Arka plan. Yalnız gerekirse oku.

## Sıradaki oturumun görevi
1. 1 numaralı belgenin **13. bölümündeki inceleme görevini** yap. **Kod, migration, DTO ve teste dokunma.**
2. Kullanıcının onayladığı plan:
   - **2 okuma ajanı (Explore / sonnet, paralel):**
     - **A, kapasite ve yaşam döngüsü:**
       - Taslak, onay (`LeaseApplicationRules.ActivateAsync`), uzatma ve revizyon yollarında F'nin nerede yazıldığı; onayda yeniden yazılıp yazılmadığı.
       - `LeaseRepository.GetOpenLeaseAreasForUnitAsync` ile `UnitRepository.GetCapacityOccupantsAsync` arasındaki sayım farkı (ikincisi `LeaseArea != null` filtreliyor).
       - `UnitCapacityHistory`'nin nerede yazılıp okunduğu.
       - `GetEvaluationDays` / `Measure` davranışı.
     - **B, altyapı:**
       - `TransactionInterceptor` / `ITransactionalService` yapısı (kilit nereye konabilir).
       - `LeaseMinimumUsageArea` ayarının nereden ve nasıl kaydedildiği.
       - Kontenjanın düzenlendiği akış (`PropertyService.ValidateCapacityAsync`, Property edit ekranı).
       - İlgili izinler (`PermissionCatalog`) ve mevcut testler (`UnitCapacityPolicyTests`, `CapacityLeaseServiceTests`, `UnitCapacityChangeTests`).
   - **Sentez ana oturumda (Opus):** 13. bölümdeki 6 başlıklı düzende, dosya/metot kanıtlı rapor.
   - **Çıktı:** `C:\Users\onur.baskan\Downloads\teknopark_acik_ofis_v2_kod_incelemesi.md` + kullanıcıya kısa, basit dilli özet.
3. Belgenin kendisinin açık bıraktığı karar (6. bölüm, K-4):
   - Min artışında uyumsuz bir taslak varsa ayar kaydı mı engellensin, yoksa yalnız o taslağın onayı mı?
   - Gerekçeli öneriyle sor.

## Kullanıcıyla çalışma tercihleri
- Türkçe, kısa, sonuçla başla.
- Bir şeyi sorarken önce **basit dille ve sayısal örnekle** anlat. Kullanıcı soyut anlatımı anlamadığını birkaç kez belirtti.
- Kararları tek tek sor, önerini "(Önerilen)" diye işaretle.
- **Commit atma.** Commit mesajı istenirse sade Türkçe yaz:
  - Faz veya test lafı etme.
  - Scratchpad'e kaydet.
- Mevcut kodu doğru kabul etme. Mevcut sistem mantık hatası içeriyor; hangi yaklaşımın hatayı çözdüğünü söyle.
