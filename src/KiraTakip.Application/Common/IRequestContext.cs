namespace KiraTakip.Common;

public interface IRequestContext
{
    string? UserId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? BaseUrl { get; }
}