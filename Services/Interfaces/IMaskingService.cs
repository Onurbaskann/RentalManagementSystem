using KiraTakip.Infrastructure;

namespace KiraTakip.Services.Interfaces;

public interface IMaskingService
{
    string? Mask(string? value, MaskType maskType);
}
