using KiraTakip.Models;
using KiraTakip.Models.Common;

namespace KiraTakip.Repositories.Interfaces;

public interface ITahakkukRepository
{
    /// <summary>
    /// Tüm tahakkukları getirir. yetkiliTasinmazIds null ise filtreleme yapılmaz.
    /// </summary>
    Task<List<KiraTahakkuk>> GetAllAsync(int? sozlesmeId, List<int>? yetkiliTasinmazIds);

    /// <summary>
    /// Sayfalama, arama ve filtreleme destekli tahakkuk listesi.
    /// </summary>
    Task<PagedResult<KiraTahakkuk>> GetPagedAsync(TableQuery q, int? sozlesmeId, List<int>? yetkiliTasinmazIds);

    /// <summary>
    /// Tek bir tahakkuğu tüm ilişkileriyle (eager loading) getirir.
    /// </summary>
    Task<KiraTahakkuk?> GetByIdAsync(int id);

    /// <summary>
    /// Tahakkuk oluşturulacak sözleşmenin temel bilgilerini getirir.
    /// </summary>
    Task<KiraSozlesmesi?> GetSozlesmeAsync(int sozlesmeId);

    /// <summary>
    /// Belirli bir sözleşme + dönem kombinasyonu için otomatik tahakkuk var mı?
    /// </summary>
    Task<bool> ExistsForDonemAsync(int sozlesmeId, DateTime donemIlkGunu);

    /// <summary>
    /// Vadesi geçmiş, henüz "Gecikti" olarak işaretlenmemiş tahakkukları getirir.
    /// </summary>
    Task<List<KiraTahakkuk>> GetGeciktirileceklerAsync(DateTime bugun);

    /// <summary>
    /// Belirli bir tahakkuk için onaylı ödemelerin toplam tutarını döner.
    /// </summary>
    Task<decimal> GetOdenenTutarAsync(int tahakkukId);

    /// <summary>
    /// Belirli bir tahakkuku id ile getirir (sadece ana entity, include yok).
    /// </summary>
    Task<KiraTahakkuk?> FindAsync(int id);

    /// <summary>
    /// Yeni bir tahakkuku takip listesine ekler.
    /// </summary>
    Task AddAsync(KiraTahakkuk tahakkuk);

    /// <summary>
    /// Beklemedeki tüm değişiklikleri veritabanına yazar.
    /// </summary>
    Task SaveChangesAsync();
}
