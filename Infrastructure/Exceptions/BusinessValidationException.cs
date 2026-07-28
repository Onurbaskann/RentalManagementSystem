namespace KiraTakip.Infrastructure.Exceptions;

/// <summary>
/// Kullanıcının aynı form üzerinde düzeltebileceği, mevcut veritabanı durumuna
/// bağlı iş kuralı ihlalini alan adıyla birlikte taşır.
/// </summary>
public sealed class BusinessValidationException : BusinessException
{
    public string Field { get; }

    public BusinessValidationException(string field, string message, string? code = null)
        : base(message, ErrorType.Failure, code)
    {
        Field = field;
    }
}
