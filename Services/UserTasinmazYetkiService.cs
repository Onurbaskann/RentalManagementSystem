using KiraTakip.Data;
using KiraTakip.Repositories.Interfaces;
using KiraTakip.Services.Interfaces;

namespace KiraTakip.Services;

public class UserTasinmazYetkiService : IUserTasinmazYetkiService
{
    private readonly IUserTasinmazYetkiRepository _repo;
    private readonly IUnitOfWork _uow;

    public UserTasinmazYetkiService(IUserTasinmazYetkiRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<List<int>> GetYetkiliTasinmazIdsAsync(string userId)
        => await _repo.GetYetkiliTasinmazIdsAsync(userId);

    public async Task<bool> CanViewTasinmazAsync(string userId, int tasinmazId)
        => await _repo.CanViewTasinmazAsync(userId, tasinmazId);

    public async Task SetUserTasinmazYetkileriAsync(string userId, List<int> tasinmazIds, string atayanUserId)
    {
        var existing = await _repo.GetForUserAsync(userId);
        await _repo.RemoveRangeAsync(existing);
        await _uow.SaveChangesAsync();

        if (tasinmazIds != null && tasinmazIds.Any())
        {
            var newRecords = tasinmazIds.Select(tId => new UserTasinmazYetki
            {
                UserId = userId,
                TasinmazId = tId,
                AtanmaTarihi = DateTime.Now,
                AtayanUserId = atayanUserId
            });
            await _repo.AddRangeAsync(newRecords);
            await _uow.SaveChangesAsync();
        }
    }
}
