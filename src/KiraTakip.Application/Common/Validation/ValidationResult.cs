namespace KiraTakip.Infrastructure.Validation;

/// <summary>
/// Servis metotlarındaki input validation kontrollerinin sonucu. Fırlatılmaz;
/// çağıran (controller) Errors listesini ModelState'e basıp View'ı yeniden render eder.
/// İş kuralı (business rule) ihlalleri bu tipin kapsamı dışındadır — bkz. BusinessException.
/// </summary>
public sealed class ValidationResult
{
    private static readonly ValidationResult ValidInstance = new(Array.Empty<ValidationError>());

    public IReadOnlyList<ValidationError> Errors { get; }
    public bool IsValid => Errors.Count == 0;

    private ValidationResult(IReadOnlyList<ValidationError> errors)
    {
        Errors = errors;
    }

    public static ValidationResult Valid() => ValidInstance;

    public static ValidationResult Invalid(IEnumerable<ValidationError> errors)
        => new([.. errors]);

    public static ValidationResult Invalid(ValidationError error)
        => new([error]);

    /// <summary>Birden fazla validasyon sonucunu tek bir sonuçta birleştirir.</summary>
    public ValidationResult Combine(ValidationResult other)
    {
        if (IsValid) return other;
        if (other.IsValid) return this;
        return new ValidationResult([.. Errors, .. other.Errors]);
    }
}
