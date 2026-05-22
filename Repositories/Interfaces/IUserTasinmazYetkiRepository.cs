namespace KiraTakip.Repositories.Interfaces;

public interface IUserTasinmazYetkiRepository
{
    Task<List<int>> GetYetkiliTasinmazIdsAsync(string userId);
    Task<bool> CanViewTasinmazAsync(string userId, int tasinmazId);
    Task<List<UserTasinmazYetki>> GetForUserAsync(string userId);
    Task RemoveRangeAsync(IEnumerable<UserTasinmazYetki> entities);
    Task AddRangeAsync(IEnumerable<UserTasinmazYetki> entities);
}
