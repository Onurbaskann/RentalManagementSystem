namespace KiraTakip.Infrastructure.Exceptions;

/// <summary>
/// Servis katmanındaki iş kuralı kontrollerini tek tip yazmayı sağlayan yardımcı.
/// Koşul ihlal edildiğinde doğru ErrorType ile BusinessException fırlatır; mevcut
/// `if (kötüDurum) throw new InvalidOperationException(...)` deseninin yerini alır.
/// Koşul yönü mevcut kodla birebir: koşul DOĞRUYSA fırlatır.
///
/// Input validation (ValidationResult/IValidator) ile karıştırılmamalı — Guard yalnızca
/// mevcut duruma bağlı iş kuralları içindir (bulunamadı, çakışma, yetkisiz, genel ihlal).
/// </summary>
public static class Guard
{
    /// <summary>entity null ise 404 (NotFound) fırlatır; değilse entity'yi döner (zincirlenebilir).</summary>
    public static T NotFound<T>(T? entity, string message, string? code = null) where T : class
        => entity ?? throw new BusinessException(message, ErrorType.NotFound, code);

    /// <summary>condition doğruysa 409 (Conflict) fırlatır — çakışma / geçersiz durum geçişi.</summary>
    public static void Conflict(bool condition, string message, string? code = null)
    {
        if (condition) throw new BusinessException(message, ErrorType.Conflict, code);
    }

    /// <summary>condition doğruysa 403 (Forbidden) fırlatır.</summary>
    public static void Forbidden(bool condition, string message, string? code = null)
    {
        if (condition) throw new BusinessException(message, ErrorType.Forbidden, code);
    }

    /// <summary>condition doğruysa 400 (Failure) fırlatır — genel iş kuralı ihlali.</summary>
    public static void Against(bool condition, string message, string? code = null)
    {
        if (condition) throw new BusinessException(message, ErrorType.Failure, code);
    }

    /// <summary>condition doğruysa aynı formda düzeltilebilecek alan-bazlı ihlal fırlatır.</summary>
    public static void InvalidField(bool condition, string field, string message, string? code = null)
    {
        if (condition) throw new BusinessValidationException(field, message, code);
    }
}
