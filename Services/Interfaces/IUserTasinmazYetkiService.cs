namespace KiraTakip.Services.Interfaces;

public interface IUserTasinmazYetkiService
{
    Task<List<int>> GetYetkiliTasinmazIdsAsync(string userId);
    Task<bool> CanViewTasinmazAsync(string userId, int tasinmazId);
    Task SetUserTasinmazYetkileriAsync(string userId, List<int> tasinmazIds, string atayanUserId);
}
