namespace KiraTakip.Infrastructure.Validation;

/// <summary>
/// Tek bir input validation hatasını temsil eder. Field, ModelState'e ve
/// ön yüzdeki asp-validation-for eşlemesine karşılık gelir.
/// </summary>
public sealed class ValidationError
{
    public string? Field { get; }
    public string Message { get; }
    public string? Code { get; }

    public ValidationError(string message, string? field = null, string? code = null)
    {
        Message = message;
        Field = field;
        Code = code;
    }
}
