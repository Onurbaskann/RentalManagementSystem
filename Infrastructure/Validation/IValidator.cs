namespace KiraTakip.Infrastructure.Validation;

/// <summary>
/// Bir input tipi (genellikle ViewModel) için input validation kurallarını tanımlar.
/// ValidationActionFilter, action argümanı için DI'da kayıtlı IValidator&lt;T&gt; bulursa
/// otomatik olarak çalıştırır. Projedeki TEK input validation mekanizmasıdır (DataAnnotations
/// kullanılmaz) — DB/repository sorgusu GEREKTİRMEYEN, yalnızca gönderilen veriye bakan tüm
/// kontroller burada yer alır: zorunluluk, biçim, aralık, aynı-nesne-içi çapraz-alan ve
/// koşullu kurallar dahil. DB'ye bakan veya mevcut sistem durumuna bağlı kontroller (benzersizlik,
/// "bulunamadı", "silinemez" gibi) bu kapsamda DEĞİLDİR — bkz. BusinessException/Guard.
/// </summary>
public interface IValidator<T>
{
    ValidationResult Validate(T input);
}
