namespace KiraTakip.Services.Interfaces.Payments;

public interface IStoreAccountCredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
