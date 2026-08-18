using KiraTakip.Models.Dtos;

namespace KiraTakip.Services.Interfaces;

public interface IReservationCompletionService
{
    Task<List<int>> FindCandidatesAsync(FindReservationCompletionCandidatesInput input);
    Task<bool> CompleteAsync(CompleteReservationInput input);
}
