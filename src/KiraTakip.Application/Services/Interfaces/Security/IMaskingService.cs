using KiraTakip.Domain.Auditing;

namespace KiraTakip.Services.Interfaces.Security;

public interface IMaskingService
{
    string? Mask(string? value, MaskType maskType);
}
