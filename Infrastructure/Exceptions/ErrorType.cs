namespace KiraTakip.Infrastructure.Exceptions;

/// <summary>
/// BusinessException'ın taşıdığı iş kuralı ihlali türü; global filter bunu
/// HTTP status koduna eşler. Input validation bu kapsamda değildir (bkz. ValidationResult).
/// </summary>
public enum ErrorType
{
    /// <summary>Kayıt bulunamadı → 404.</summary>
    NotFound,

    /// <summary>Mevcut durumla çelişen işlem (çakışma, geçersiz durum geçişi) → 409.</summary>
    Conflict,

    /// <summary>Yetkisiz işlem → 403.</summary>
    Forbidden,

    /// <summary>Genel iş kuralı ihlali → 400.</summary>
    Failure,
}
