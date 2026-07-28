using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public static class ReservationViewModelMapper
{
    public static CalculateReservationInput ToInput(
        this ReservationCalculationQueryViewModel viewModel,
        ReservationAccessScopeInput accessScope)
        => new(
            viewModel.UnitId,
            DateTime.Parse(viewModel.Start!),
            DateTime.Parse(viewModel.End!),
            accessScope);
}
