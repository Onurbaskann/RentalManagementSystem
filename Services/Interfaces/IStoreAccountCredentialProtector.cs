namespace KiraTakip.Services.Interfaces;

public interface IStoreAccountCredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
