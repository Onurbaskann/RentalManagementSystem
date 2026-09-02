using KiraTakip.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace KiraTakip.Services;

public class StoreAccountCredentialProtector : IStoreAccountCredentialProtector
{
    private const string Purpose = "KiraTakip.Payments.StoreAccountCredentials.v1";
    private readonly IDataProtector _protector;

    public StoreAccountCredentialProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedValue) => _protector.Unprotect(protectedValue);
}
