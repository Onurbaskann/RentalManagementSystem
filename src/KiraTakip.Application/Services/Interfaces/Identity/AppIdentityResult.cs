namespace KiraTakip.Services.Interfaces.Identity;

public sealed class AppIdentityResult
{
    private static readonly AppIdentityResult SuccessResult = new() { Succeeded = true, Errors = [] };

    public bool Succeeded { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static AppIdentityResult Success() => SuccessResult;

    public static AppIdentityResult Failed(IEnumerable<string> errors) =>
        new() { Succeeded = false, Errors = errors.ToList() };

    public static AppIdentityResult Failed(params string[] errors) =>
        new() { Succeeded = false, Errors = errors };
}