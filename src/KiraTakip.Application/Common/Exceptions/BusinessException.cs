namespace KiraTakip.Infrastructure.Exceptions;

/// <summary>
/// Servis katmanının iş kuralı ihlallerinde fırlattığı istisna. Mevcut
/// "throw new InvalidOperationException(...)" deseninin yerini alır.
/// BusinessRuleExceptionFilter bunu yakalayıp ErrorType'a göre kullanıcıya
/// uygun hata gösterimine (modal/redirect veya HTTP status) çevirir.
/// </summary>
public class BusinessException : Exception
{
    public ErrorType ErrorType { get; }
    public string? Code { get; }

    public BusinessException(string message, ErrorType errorType = ErrorType.Failure, string? code = null)
        : base(message)
    {
        ErrorType = errorType;
        Code = code;
    }
}
