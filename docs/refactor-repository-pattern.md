# Refactor: Repository Pattern + Projection (Pilot: TahakkukRepository & TahakkukService)

## Amaç

Repository pattern altyapısını kurmak ve **örnek bir uygulama** olarak `TahakkukRepository` ile `TahakkukService` üzerinde uçtan uca projeksiyon-temelli okuma yaklaşımını hayata geçirmek. Diğer repo/servisler bu örneği takip ederek **sonraki refaktörlerde** dönüştürülecek.

## Kapsam

### Yapılacaklar

- `IBaseRepository<T>` ve `BaseRepository<T>` (yeni — temel altyapı, tüm repository'ler için)
- `ITahakkukRepository` ve `TahakkukRepository` (mevcut, pilot olarak refaktör)
- `ITahakkukService` ve `TahakkukService` (mevcut, pilot olarak refaktör)
- `TahakkukListItemDto` ve `TahakkukDetayDto` (yeni — projeksiyon hedef tipleri)
- `TahakkukService`'in consumer'larında (controller'lar) imza güncellemeleri

### Yapılmayacaklar

- Diğer repository sınıflarına dokunulmayacak (`TasinmazService`, `KiraciService`, `SozlesmeService`, `OdemeService` vb. olduğu gibi kalacak — `IUnitOfWork` ve `BaseRepository` altyapısı kurulduktan sonra ayrı refaktörlerde dönüştürülecek)
- AutoMapper eklenmeyecek (YAGNI — gerçek ihtiyaç ortaya çıkana kadar)
- ReadOnly varyant metotları eklenmeyecek (projeksiyon metotları zaten `AsNoTracking` kullanır)
- Migration regenerate edilmeyecek (şema değişmiyor)
- View'larda kapsamlı UI değişikliği yapılmayacak — sadece tip uyumsuzluğu olan yerler düzeltilecek (entity property erişimi → DTO property erişimi)

## Tasarım Kararları

### 1. Entity sadece CRUD/business logic için döner

Repository'den entity dönen metotlar **yalnızca** update, delete veya business kuralı kontrolü için kullanılır. Okuma senaryolarında **her zaman projeksiyon** yapılır.

**Gerekçe:** Pratikte entity'nin tüm property'lerine ihtiyaç duyulan okuma senaryosu çok nadirdir. Listeleme, detay, özet gibi UI senaryolarında sadece ekrana gidecek alanlar yeterlidir. Tüm entity'yi memory'e çekmek hem SQL hem network maliyeti getirir.

### 2. Projeksiyon: Method syntax + navigation property

LINQ query syntax (`from x in ctx.X join y in ctx.Y ...`) yerine **method syntax + navigation property projection** standart olarak kullanılır:

```csharp
_dbSet.Where(...).Select(t => new TahakkukDetayDto {
    KiraciAd = t.KiraSozlesmesi.Kiraci.Ad,
    ...
})
```

**Gerekçe:** Mevcut modelde navigation property'ler kurulu. Method syntax daha kısa, refactor-friendly ve EF Core 7+'da önerilen idiomatic yaklaşımdır.

### 3. AutoMapper eklenmiyor

`Select` projeksiyonu zaten mapping işini SQL seviyesinde yapıyor. Tekrar tekrar yazılan mapping ortaya çıkmadıkça eklenmez.

### 4. UpdateAsync no-op olarak korunuyor

`UpdateAsync` `Task.CompletedTask` döner. EF change tracker tracked entity'lerdeki property değişikliklerini otomatik algılar — `_dbSet.Update(entity)` çağırmak tüm kolonları "modified" olarak işaretler ki bu istenmez. `UpdateAsync` sadece kod okurluğu için (servis kodunda "burada güncelleme yapılıyor" sinyali) duruyor.

### 5. SaveChanges → IUnitOfWork üzerinden

Repository `SaveChangesAsync` çağırmaz. Service `IUnitOfWork` enjekte eder ve transaction sınırını kendi yönetir.

### 6. ReadOnly varyantları kaldırıldı

Önceki tasarımda `GetReadonlyAsync`, `GetAllReadonlyAsync` vb. metotlar vardı. Bu refaktörde **kaldırılıyor**. Projeksiyon metotları zaten `AsNoTracking` kullanır; entity dönen metotlar yalnızca CRUD için olduğundan tracking gerekir.

## Final API Yüzeyi

### `IBaseRepository<T>`

```csharp
using System.Linq.Expressions;

namespace KiraTakip.Repositories.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    // Entity dönen metotlar — sadece CRUD / business logic için (tracked)
    Task<T?> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? include = null);
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? include = null);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null);

    // Projeksiyon metotları — okuma için (AsNoTracking)
    Task<TResult?> GetByIdAsync<TResult>(int id, Expression<Func<T, TResult>> selector);
    Task<TResult?> GetAsync<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector);
    Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<T, bool>>? filter, Expression<Func<T, TResult>> selector);

    // Yardımcı metotlar
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);

    // Yazma metotları
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);   // No-op — kod okurluğu için sinyal
    Task DeleteAsync(int id, bool hardDelete = false);
}
```

### `BaseRepository<T>` (abstract, virtual metotlar)

```csharp
using System.Linq.Expressions;
using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _ctx;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(ApplicationDbContext ctx)
    {
        _ctx   = ctx;
        _dbSet = ctx.Set<T>();
    }

    // ── Entity dönen (tracked) ────────────────────────────────────────────
    public virtual async Task<T?> GetByIdAsync(int id, Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> q = _dbSet;
        if (include != null) q = include(q);
        return await q.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<T?> GetAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> q = _dbSet;
        if (include != null) q = include(q);
        return await q.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null)
    {
        IQueryable<T> q = _dbSet;
        if (include != null) q = include(q);
        if (filter != null) q = q.Where(filter);
        return await q.ToListAsync();
    }

    // ── Projeksiyon (AsNoTracking) ────────────────────────────────────────
    public virtual async Task<TResult?> GetByIdAsync<TResult>(int id, Expression<Func<T, TResult>> selector)
    {
        return await _dbSet.AsNoTracking()
                           .Where(e => e.Id == id)
                           .Select(selector)
                           .FirstOrDefaultAsync();
    }

    public virtual async Task<TResult?> GetAsync<TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector)
    {
        return await _dbSet.AsNoTracking()
                           .Where(predicate)
                           .Select(selector)
                           .FirstOrDefaultAsync();
    }

    public virtual async Task<List<TResult>> GetAllAsync<TResult>(Expression<Func<T, bool>>? filter, Expression<Func<T, TResult>> selector)
    {
        IQueryable<T> q = _dbSet.AsNoTracking();
        if (filter != null) q = q.Where(filter);
        return await q.Select(selector).ToListAsync();
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────
    public virtual Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        => _dbSet.AsNoTracking().AnyAsync(predicate);

    public virtual Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
        => filter == null
            ? _dbSet.AsNoTracking().CountAsync()
            : _dbSet.AsNoTracking().CountAsync(filter);

    // ── Yazma ─────────────────────────────────────────────────────────────
    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    /// <summary>
    /// EF Change Tracker tracked entity'deki değişiklikleri otomatik algılar.
    /// Bu metot yalnızca kod okurluğu için (servis kodunda güncelleme niyetini belgelemek) bulunur.
    /// </summary>
    public Task UpdateAsync(T entity) => Task.CompletedTask;

    public async Task DeleteAsync(int id, bool hardDelete = false)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return;

        if (hardDelete)
            _dbSet.Remove(entity);
        else
        {
            entity.IsDeleted = true;
            entity.IsActive  = false;
        }
    }
}
```

### `ITahakkukRepository`

```csharp
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;

namespace KiraTakip.Repositories.Interfaces;

public interface ITahakkukRepository : IBaseRepository<KiraTahakkuk>
{
    // ── Okuma (DTO döner) ─────────────────────────────────────────────────
    Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds);
    Task<PagedResult<TahakkukListItemDto>> GetPagedListAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds);
    Task<TahakkukDetayDto?> GetDetayAsync(int id);

    // ── Business logic için entity dönen domain sorguları ─────────────────
    /// <summary>Vade tarihi geçmiş, henüz "Gecikti" işaretlenmemiş tahakkukları döner (tracked).</summary>
    Task<List<KiraTahakkuk>> GetGeciktirileceklerAsync(DateTime bugun);

    // ── Hesaplamalar ──────────────────────────────────────────────────────
    Task<decimal> GetOdenenTutarAsync(int tahakkukId);
}
```

**Notlar:**
- `GetByIdAsync` (entity dönen) override edilmiyor — `BaseRepository`'deki base implementasyon yeterli; eğer business operation öncesi entity'nin tüm child collection'larıyla gelmesi gerekiyorsa `include` parametresi servis tarafında verilir.
- `GetAllAsync(int?, List<int>?)`, `GetPagedAsync(TableQuery, int?, List<int>?)` ve `GetSozlesmeAsync`, `ExistsForDonemAsync` gibi domain metotları **DTO dönen** karşılıklarıyla değiştiriliyor (`GetListAsync`, `GetPagedListAsync`).
- `GetSozlesmeAsync` ve `ExistsForDonemAsync` `KiraSozlesmesi` üzerinde sorgular yapıyordu — bunlar `TahakkukRepository`'de değil, ileride `SozlesmeRepository`'de yer almalı. Şimdilik bu refaktörden çıkarıldılar; çağrıldıkları yerler varsa servis içinden farklı bir mekanizmayla (örn. `ISozlesmeService`) çözülmeli.

### `TahakkukRepository`

```csharp
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace KiraTakip.Repositories;

public class TahakkukRepository : BaseRepository<KiraTahakkuk>, ITahakkukRepository
{
    public TahakkukRepository(ApplicationDbContext ctx) : base(ctx) { }

    // ── Listeleme (DTO) ───────────────────────────────────────────────────
    public async Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds)
    {
        IQueryable<KiraTahakkuk> q = _dbSet.AsNoTracking();

        if (sozlesmeId.HasValue)
            q = q.Where(t => t.KiraSozlesmesiId == sozlesmeId.Value);

        if (yetkiliTasinmazIds != null)
            q = q.Where(t => yetkiliTasinmazIds.Contains(t.KiraSozlesmesi.Birim.TasinmazId));

        return await q
            .OrderByDescending(t => t.DonemBaslangic)
            .Select(t => new TahakkukListItemDto
            {
                Id              = t.Id,
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                KiraciAd        = t.KiraSozlesmesi.Kiraci.Ad,
                KiraciSoyad     = t.KiraSozlesmesi.Kiraci.Soyad,
                TasinmazAd      = t.KiraSozlesmesi.Birim.Tasinmaz.Ad,
                DonemBaslangic  = t.DonemBaslangic,
                VadeTarihi      = t.VadeTarihi,
                ToplamTutar     = t.ToplamTutar,
                OdenenTutar     = t.OdenenTutar,
                Durum           = t.Durum,
                KaynakTipi      = t.KaynakTipi
            })
            .ToListAsync();
    }

    // ── Sayfalı listeleme (DTO) ───────────────────────────────────────────
    public async Task<PagedResult<TahakkukListItemDto>> GetPagedListAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds)
    {
        IQueryable<KiraTahakkuk> query = _dbSet.AsNoTracking();

        if (sozlesmeId.HasValue)
            query = query.Where(t => t.KiraSozlesmesiId == sozlesmeId.Value);

        if (yetkiliTasinmazIds != null)
            query = query.Where(t => yetkiliTasinmazIds.Contains(t.KiraSozlesmesi.Birim.TasinmazId));

        // Filtreler (mevcut TahakkukRepository.GetPagedAsync'teki tüm Q, From, To, Min, Max, TasinmazId, BirimId, KiraciId, Yil, Kaynak, Durum filtreleri buraya taşınacak)
        if (!string.IsNullOrWhiteSpace(q.Q))
        {
            var s = q.Q.Trim();
            query = query.Where(t =>
                EF.Functions.Like(t.KiraSozlesmesi.Kiraci.Ad, $"%{s}%") ||
                (t.KiraSozlesmesi.Kiraci.Soyad != null && EF.Functions.Like(t.KiraSozlesmesi.Kiraci.Soyad, $"%{s}%")) ||
                EF.Functions.Like(t.KiraSozlesmesi.Birim.Tasinmaz.Ad, $"%{s}%"));
        }
        if (q.From.HasValue)        query = query.Where(t => t.DonemBaslangic >= q.From.Value);
        if (q.To.HasValue)          query = query.Where(t => t.DonemBaslangic <= q.To.Value);
        if (q.Min.HasValue)         query = query.Where(t => t.ToplamTutar    >= q.Min.Value);
        if (q.Max.HasValue)         query = query.Where(t => t.ToplamTutar    <= q.Max.Value);
        if (q.TasinmazId.HasValue)  query = query.Where(t => t.KiraSozlesmesi.Birim.TasinmazId == q.TasinmazId.Value);
        if (q.BirimId.HasValue)     query = query.Where(t => t.KiraSozlesmesi.BirimId          == q.BirimId.Value);
        if (q.KiraciId.HasValue)    query = query.Where(t => t.KiraSozlesmesi.KiraciId         == q.KiraciId.Value);
        if (q.Yil.HasValue)         query = query.Where(t => t.DonemBaslangic.Year             == q.Yil.Value);

        if (!string.IsNullOrWhiteSpace(q.Kaynak))
        {
            TahakkukKaynakTipi? kt = q.Kaynak.ToLower() switch
            {
                "manuel"       => TahakkukKaynakTipi.Manuel,
                "sozlesme"     => TahakkukKaynakTipi.Sozlesme,
                "rezervasyon"  => TahakkukKaynakTipi.Rezervasyon,
                _              => null
            };
            if (kt.HasValue) query = query.Where(t => t.KaynakTipi == kt.Value);
        }

        if (!string.IsNullOrWhiteSpace(q.Durum) && q.Durum != "tum")
        {
            TahakkukDurumu? d = q.Durum.ToLower() switch
            {
                "bekliyor"   => TahakkukDurumu.Bekleniyor,
                "kismi"      => TahakkukDurumu.KismenOdendi,
                "tamodendi"  => TahakkukDurumu.TamOdendi,
                "gecikti"    => TahakkukDurumu.Gecikti,
                _            => null
            };
            if (d.HasValue) query = query.Where(t => t.Durum == d.Value);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.DonemBaslangic)
            .Skip(q.Skip).Take(q.Take)
            .Select(t => new TahakkukListItemDto
            {
                Id              = t.Id,
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                KiraciAd        = t.KiraSozlesmesi.Kiraci.Ad,
                KiraciSoyad     = t.KiraSozlesmesi.Kiraci.Soyad,
                TasinmazAd      = t.KiraSozlesmesi.Birim.Tasinmaz.Ad,
                DonemBaslangic  = t.DonemBaslangic,
                VadeTarihi      = t.VadeTarihi,
                ToplamTutar     = t.ToplamTutar,
                OdenenTutar     = t.OdenenTutar,
                Durum           = t.Durum,
                KaynakTipi      = t.KaynakTipi
            })
            .ToListAsync();

        return new PagedResult<TahakkukListItemDto>
        {
            Items = items,
            Total = total,
            Page  = Math.Max(1, q.Page),
            Size  = q.SafeSize
        };
    }

    // ── Detay (DTO) ───────────────────────────────────────────────────────
    public async Task<TahakkukDetayDto?> GetDetayAsync(int id)
    {
        return await _dbSet.AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TahakkukDetayDto
            {
                Id               = t.Id,
                KiraSozlesmesiId = t.KiraSozlesmesiId,
                KiraciAd         = t.KiraSozlesmesi.Kiraci.Ad,
                KiraciSoyad      = t.KiraSozlesmesi.Kiraci.Soyad,
                TasinmazAd       = t.KiraSozlesmesi.Birim.Tasinmaz.Ad,
                BirimAd          = t.KiraSozlesmesi.Birim.Ad,
                DonemBaslangic   = t.DonemBaslangic,
                DonemBitis       = t.DonemBitis,
                VadeTarihi       = t.VadeTarihi,
                ToplamTutar      = t.ToplamTutar,
                OdenenTutar      = t.OdenenTutar,
                Durum            = t.Durum,
                KaynakTipi       = t.KaynakTipi,
                Kalemler         = t.Kalemler.Select(k => new TahakkukKalemDto
                {
                    Id          = k.Id,
                    BorcTipiAd  = k.BorcTipi.Ad,
                    Tutar       = k.Tutar,
                    Aciklama    = k.Aciklama
                }).ToList(),
                Odemeler         = t.Odemeler.Select(o => new TahakkukOdemeDto
                {
                    Id            = o.Id,
                    OdemeTarihi   = o.OdemeTarihi,
                    Tutar         = o.Tutar,
                    Durum         = o.Durum
                }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    // ── Business logic için entity dönen sorgu ────────────────────────────
    public async Task<List<KiraTahakkuk>> GetGeciktirileceklerAsync(DateTime bugun)
    {
        return await _dbSet.Where(t => t.Durum != TahakkukDurumu.TamOdendi &&
                                       t.Durum != TahakkukDurumu.IptalEdildi &&
                                       t.VadeTarihi < bugun)
                           .ToListAsync();
    }

    // ── Hesaplama ─────────────────────────────────────────────────────────
    public async Task<decimal> GetOdenenTutarAsync(int tahakkukId)
    {
        return await _ctx.KiraOdemeler
                         .AsNoTracking()
                         .Where(o => o.KiraTahakkukId == tahakkukId && o.Durum == OdemeDurumu.Onaylandi)
                         .SumAsync(o => (decimal?)o.Tutar) ?? 0m;
    }
}
```

**Property isimleri**: `TahakkukKalemDto.BorcTipiAd`, `TahakkukOdemeDto.OdemeTarihi` gibi alan adları varsayımdır. `KiraOdeme` ve `KiraTahakkukKalemi` entity'lerinin gerçek property isimleri kontrol edilip DTO ve projeksiyon buna göre düzeltilmelidir.

### `ITahakkukService`

```csharp
using KiraTakip.Models;
using KiraTakip.Models.Common;

namespace KiraTakip.Services.Interfaces;

public interface ITahakkukService
{
    // Listeleme (DTO döner)
    Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId = null, string? userId = null);
    Task<PagedResult<TahakkukListItemDto>> GetPagedAsync(TableQuery q, int? sozlesmeId = null, string? userId = null);
    Task<TahakkukDetayDto?> GetDetayAsync(int id);

    // Business operations
    Task GecikmeleriGuncelleAsync();
    Task OdenenTutarGuncelleAsync(int tahakkukId);
}
```

### `TahakkukService`

```csharp
using KiraTakip.Data;
using KiraTakip.Models;
using KiraTakip.Models.Common;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class TahakkukService : ITahakkukService
{
    private readonly ITahakkukRepository  _repo;
    private readonly IUnitOfWork          _uow;
    private readonly UserTasinmazYetkiService _yetkiService;

    public TahakkukService(ITahakkukRepository repo, IUnitOfWork uow, UserTasinmazYetkiService yetkiService)
    {
        _repo         = repo;
        _uow          = uow;
        _yetkiService = yetkiService;
    }

    // ── Listeleme ────────────────────────────────────────────────────────
    public async Task<List<TahakkukListItemDto>> GetListAsync(int? sozlesmeId = null, string? userId = null)
    {
        var yetkiliIds = await ResolveYetkiAsync(userId);
        return await _repo.GetListAsync(sozlesmeId, yetkiliIds);
    }

    public async Task<PagedResult<TahakkukListItemDto>> GetPagedAsync(TableQuery q, int? sozlesmeId = null, string? userId = null)
    {
        var yetkiliIds = await ResolveYetkiAsync(userId);
        return await _repo.GetPagedListAsync(q, sozlesmeId, yetkiliIds);
    }

    public Task<TahakkukDetayDto?> GetDetayAsync(int id) => _repo.GetDetayAsync(id);

    // ── Business: Gecikme Güncelleme ─────────────────────────────────────
    public async Task GecikmeleriGuncelleAsync()
    {
        var gecikmisBekleyenler = await _repo.GetGeciktirileceklerAsync(DateTime.Today);
        if (gecikmisBekleyenler.Count == 0) return;

        foreach (var t in gecikmisBekleyenler)
        {
            t.Durum = TahakkukDurumu.Gecikti;
            await _repo.UpdateAsync(t); // No-op marker
        }

        await _uow.SaveChangesAsync();
    }

    // ── Business: Ödenen Tutar Güncelleme ────────────────────────────────
    public async Task OdenenTutarGuncelleAsync(int tahakkukId)
    {
        var tahakkuk = await _repo.GetByIdAsync(tahakkukId);
        if (tahakkuk == null) return;

        var odenenTutar = await _repo.GetOdenenTutarAsync(tahakkukId);
        tahakkuk.OdenenTutar = odenenTutar;

        tahakkuk.Durum = odenenTutar >= tahakkuk.ToplamTutar
            ? TahakkukDurumu.TamOdendi
            : odenenTutar > 0
                ? TahakkukDurumu.KismenOdendi
                : DateTime.Today > tahakkuk.VadeTarihi
                    ? TahakkukDurumu.Gecikti
                    : TahakkukDurumu.Bekleniyor;

        await _repo.UpdateAsync(tahakkuk); // No-op marker
        await _uow.SaveChangesAsync();
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────
    private async Task<List<int>?> ResolveYetkiAsync(string? userId) =>
        userId == null ? null : await _yetkiService.GetYetkiliTasinmazIdsAsync(userId);
}
```

## DTO'lar

### `Models/Dtos/TahakkukListItemDto.cs`

```csharp
using KiraTakip.Models;

namespace KiraTakip.Models.Dtos;

public class TahakkukListItemDto
{
    public int                Id               { get; set; }
    public int                KiraSozlesmesiId { get; set; }
    public string             KiraciAd         { get; set; } = string.Empty;
    public string?            KiraciSoyad      { get; set; }
    public string             TasinmazAd       { get; set; } = string.Empty;
    public DateTime           DonemBaslangic   { get; set; }
    public DateTime           VadeTarihi       { get; set; }
    public decimal            ToplamTutar      { get; set; }
    public decimal            OdenenTutar      { get; set; }
    public TahakkukDurumu     Durum            { get; set; }
    public TahakkukKaynakTipi KaynakTipi       { get; set; }
}
```

### `Models/Dtos/TahakkukDetayDto.cs`

```csharp
using KiraTakip.Models;

namespace KiraTakip.Models.Dtos;

public class TahakkukDetayDto
{
    public int                Id               { get; set; }
    public int                KiraSozlesmesiId { get; set; }
    public string             KiraciAd         { get; set; } = string.Empty;
    public string?            KiraciSoyad      { get; set; }
    public string             TasinmazAd       { get; set; } = string.Empty;
    public string             BirimAd          { get; set; } = string.Empty;
    public DateTime           DonemBaslangic   { get; set; }
    public DateTime           DonemBitis       { get; set; }
    public DateTime           VadeTarihi       { get; set; }
    public decimal            ToplamTutar      { get; set; }
    public decimal            OdenenTutar      { get; set; }
    public TahakkukDurumu     Durum            { get; set; }
    public TahakkukKaynakTipi KaynakTipi       { get; set; }
    public List<TahakkukKalemDto>  Kalemler    { get; set; } = new();
    public List<TahakkukOdemeDto>  Odemeler    { get; set; } = new();
}

public class TahakkukKalemDto
{
    public int     Id          { get; set; }
    public string  BorcTipiAd  { get; set; } = string.Empty;
    public decimal Tutar       { get; set; }
    public string? Aciklama    { get; set; }
}

public class TahakkukOdemeDto
{
    public int          Id          { get; set; }
    public DateTime     OdemeTarihi { get; set; }
    public decimal      Tutar       { get; set; }
    public OdemeDurumu  Durum       { get; set; }
}
```

## Dosya Yolları

### Yeni dosyalar

| Yol | İçerik |
|---|---|
| `KiraTakip/Repositories/Interfaces/IBaseRepository.cs` | `IBaseRepository<T>` interface |
| `KiraTakip/Repositories/BaseRepository.cs` | `BaseRepository<T>` abstract class |
| `KiraTakip/Models/Dtos/TahakkukListItemDto.cs` | Liste DTO'su |
| `KiraTakip/Models/Dtos/TahakkukDetayDto.cs` | Detay + alt DTO'lar (`TahakkukKalemDto`, `TahakkukOdemeDto`) |

### Güncellenecek dosyalar

| Yol | Değişiklik |
|---|---|
| `KiraTakip/Repositories/Interfaces/ITahakkukRepository.cs` | Yeni imzalar (DTO döndüren), `IBaseRepository<KiraTahakkuk>` extend ediliyor |
| `KiraTakip/Repositories/TahakkukRepository.cs` | `BaseRepository<KiraTahakkuk>` inherit, projeksiyon metotları |
| `KiraTakip/Services/Interfaces/ITahakkukService.cs` | DTO dönen imzalar |
| `KiraTakip/Services/TahakkukService.cs` | `IUnitOfWork` eklendi, DTO dönen metotlar |

**`IUnitOfWork` ve `UnitOfWork`** zaten oluşturuldu (`KiraTakip/Data/`), `Program.cs`'te DI kayıtlı.

## Consumer Etki Analizi

`ITahakkukService`'in return type'ları değiştiği için aşağıdaki dosyalar **mutlaka güncellenmeli**:

### Controller'lar

| Dosya | Etkilenen Satırlar | Yapılacak |
|---|---|---|
| `Controllers/HomeController.cs` | 109, 110 | `GetAllAsync` → `GetListAsync`, return tipi `List<TahakkukListItemDto>` — view veya ViewModel buna uyarlanmalı |
| `Controllers/RaporController.cs` | 26, 30 | Aynı — `GetAllAsync` → `GetListAsync` |
| `Controllers/TahakkukController.cs` | 29, 32, 68 | `GetPagedAsync` artık `PagedResult<TahakkukListItemDto>` döner; `GetByIdAsync` → `GetDetayAsync` ve `TahakkukDetayDto?` döner |
| `Controllers/OdemeController.cs` | 76, 96 | `GetByIdAsync` → `GetDetayAsync`. ViewModel'deki `Tahakkuk` property'si entity yerine DTO tutmalı |
| `Controllers/SozlesmeController.cs` | 121, 122 | `GetAllAsync` → `GetListAsync`. ViewModel `Tahakkuklar` property'si `List<TahakkukListItemDto>` |

### Başka servisler

| Dosya | Etkilenen Satırlar | Yapılacak |
|---|---|---|
| `Services/OdemeService.cs` | 140, 153 | `OdenenTutarGuncelleAsync` çağırıyor — imza değişmiyor, **dokunma gerekmez** |

### View'lar

Tahakkuk listesi/detayı gösteren view'larda model tipi `KiraTahakkuk` ise `TahakkukListItemDto` / `TahakkukDetayDto` olarak değişecek. Navigation property erişimleri (`Model.KiraSozlesmesi.Kiraci.Ad`) flat property erişimine dönüşecek (`Model.KiraciAd`).

**Etkilenen view'lar (tahmini):**
- `Views/Tahakkuk/Index.cshtml`
- `Views/Tahakkuk/Detay.cshtml` (veya `Details.cshtml`)
- `Views/Home/Index.cshtml` (dashboard'da tahakkuk listesi varsa)
- `Views/Rapor/*.cshtml`
- `Views/Sozlesme/Detay.cshtml` (sözleşme detayında tahakkuk listesi)
- `Views/Odeme/*.cshtml`

İmplementasyonu yapan model bu view'ları açıp model tipi ve property erişimlerini güncellemelidir.

## İmplementasyon Sıralaması

1. **DTO'lar** (`Models/Dtos/TahakkukListItemDto.cs`, `Models/Dtos/TahakkukDetayDto.cs`) — bağımlılık yok, ilk yazılır
2. **Base altyapı** (`IBaseRepository.cs`, `BaseRepository.cs`) — DTO'ya bağımlı değil ama önce hazır olmalı
3. **`ITahakkukRepository` ve `TahakkukRepository`** — base + DTO'ya bağımlı
4. **`ITahakkukService` ve `TahakkukService`** — repo'ya bağımlı
5. **Consumer'lar** (controller'lar + view'lar) — service imzaları kesinleştikten sonra
6. **Derleme + manuel doğrulama**:
   - Tahakkuk Index sayfası açılıyor mu, liste geliyor mu
   - Detay sayfası açılıyor mu, kalemler/ödemeler görünüyor mu
   - Bir ödeme ekle/onayla → tahakkuk durumu güncelleniyor mu
   - Gecikmiş tahakkuk: vade tarihi geçmiş bir tahakkuk için durum "Gecikti" olarak güncelleniyor mu

## Migration / Çalıştırma Notları

- **Migration regenerate edilmez.** Domain modeli değişmiyor; yalnızca repository/service katmanı refactor ediliyor.
- `Program.cs`'te `IUnitOfWork` zaten DI'ye kayıtlı (`builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();`).
- `IBaseRepository<>` için DI kaydı **gerekli değil** — generic olduğundan tek başına resolve edilmez; her concrete repo (`TahakkukRepository`) zaten kendi interface'iyle kayıtlı.

## Notlar / Bilinmesi Gerekenler

- DTO'larda alt collection (`Kalemler`, `Odemeler`) ihtiyaç görüldüğü kadar tutuldu. View'lar gerçekten her detay sayfasında bunları gösteriyorsa bu yapı tutar; sadece bazı sayfalarda gerekiyorsa ayrı `TahakkukOzetDto` eklemek bir sonraki refaktörde değerlendirilebilir.
- `OdemeController` ViewModel'inde `Tahakkuk` property'si muhtemelen entity tipindeydi — DTO'ya çevrilecek. ViewModel sınıfının ismi ve property tipi güncellenmeli.
- `ITahakkukService.GetByIdAsync` imzası kaldırılıyor, yerine `GetDetayAsync` geliyor. Eski adı korumak istenirse alias eklenebilir ama yeniden adlandırma niyetin daha açık olur.
- DTO projeksiyonlarındaki property isimleri (`Soyad`, `BorcTipi.Ad`, `OdemeTarihi`, `Tutar`, `Aciklama`, `Durum`) gerçek entity'lerle karşılaştırılıp uyumsuzluklar düzeltilmelidir.
