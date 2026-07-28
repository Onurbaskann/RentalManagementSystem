using KiraTakip.Models.Dtos;

namespace KiraTakip.Models.ViewModels;

public static class PropertyViewModelMapper
{
    public static EditPropertyViewModel ToViewModel(this PropertyEditDto dto)
        => new()
        {
            Id = dto.PropertyId,
            Name = dto.Name,
            PropertyTypeId = dto.PropertyTypeId,
            UnitStructure = dto.UnitStructure,
            CanChangeUnitStructure = dto.CanChangeUnitStructure,
            SingleUnitTypeId = dto.SingleUnitTypeId,
            City = dto.City,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            Address = dto.Address,
            OpenArea = dto.OpenArea,
            ClosedArea = dto.ClosedArea,
            FloorCount = dto.FloorCount,
            Description = dto.Description,
            Units = dto.Units.Select(unit => new PropertyUnitEditViewModel
            {
                Id = unit.Id,
                UnitNo = unit.UnitNo,
                FloorNo = unit.FloorNo,
                Name = unit.Name,
                Area = unit.Area,
                Description = unit.Description,
                UnitTypeId = unit.UnitTypeId,
                HasActiveLease = unit.Id.HasValue && dto.ActiveLeaseUnitIds.Contains(unit.Id.Value)
            }).ToList(),
            ReservationAreas = dto.ReservationAreas.Select(area => new ReservationAreaEditViewModel
            {
                Id = area.Id,
                UnitNo = area.UnitNo,
                Name = area.Name,
                Area = area.Area,
                UnitTypeId = area.UnitTypeId,
                Description = area.Description,
                FreeDurationMinutes = area.FreeDurationMinutes,
                HourlyRate = area.HourlyRate,
                VatRate = area.VatRate,
                HasActiveReservation = area.Id.HasValue
                    && dto.ActiveReservationUnitIds.Contains(area.Id.Value)
            }).ToList()
        };

    public static CreatePropertyInput ToInput(this CreatePropertyViewModel viewModel)
    {
        var input = CopyTo(new CreatePropertyInput(), viewModel);
        input.PricingMatrix = viewModel.PricingMatrix.ToSaveInput(0);
        return input;
    }

    public static UpdatePropertyInput ToInput(
        this EditPropertyViewModel viewModel,
        IReadOnlyCollection<int>? accessiblePropertyIds = null)
    {
        var input = new UpdatePropertyInput
        {
            PropertyId = viewModel.Id,
            Name = viewModel.Name,
            PropertyTypeId = viewModel.PropertyTypeId,
            UnitStructure = viewModel.UnitStructure,
            SingleUnitTypeId = viewModel.SingleUnitTypeId,
            City = viewModel.City,
            District = viewModel.District,
            Neighborhood = viewModel.Neighborhood,
            Address = viewModel.Address,
            OpenArea = viewModel.OpenArea,
            ClosedArea = viewModel.ClosedArea,
            FloorCount = viewModel.FloorCount,
            Description = viewModel.Description
        };
        input.AccessiblePropertyIds = accessiblePropertyIds;
        input.PricingMatrix = viewModel.PricingMatrix.ToSaveInput(viewModel.Id);
        input.Units = viewModel.Units.Select(ToDto).ToList();
        input.ReservationAreas = viewModel.ReservationAreas.Select(ToDto).ToList();
        return input;
    }

    private static TInput CopyTo<TInput>(TInput input, CreatePropertyViewModel viewModel)
        where TInput : CreatePropertyInput
    {
        input.Name = viewModel.Name;
        input.PropertyTypeId = viewModel.PropertyTypeId;
        input.UnitStructure = viewModel.UnitStructure;
        input.SingleUnitTypeId = viewModel.SingleUnitTypeId;
        input.City = viewModel.City;
        input.District = viewModel.District;
        input.Neighborhood = viewModel.Neighborhood;
        input.Address = viewModel.Address;
        input.OpenArea = viewModel.OpenArea;
        input.ClosedArea = viewModel.ClosedArea;
        input.FloorCount = viewModel.FloorCount;
        input.Description = viewModel.Description;
        input.Units = viewModel.Units.Select(ToDto).ToList();
        input.ReservationAreas = viewModel.ReservationAreas.Select(ToDto).ToList();
        return input;
    }

    private static PropertyUnitInputDto ToDto(PropertyUnitInputViewModel viewModel)
        => new()
        {
            UnitNo = viewModel.UnitNo,
            FloorNo = viewModel.FloorNo,
            Name = viewModel.Name,
            Area = viewModel.Area,
            Description = viewModel.Description,
            UnitTypeId = viewModel.UnitTypeId
        };

    private static PropertyUnitInputDto ToDto(PropertyUnitEditViewModel viewModel)
        => new()
        {
            Id = viewModel.Id,
            UnitNo = viewModel.UnitNo,
            FloorNo = viewModel.FloorNo,
            Name = viewModel.Name,
            Area = viewModel.Area,
            Description = viewModel.Description,
            UnitTypeId = viewModel.UnitTypeId
        };

    private static ReservationAreaInputDto ToDto(ReservationAreaInputViewModel viewModel)
        => new()
        {
            UnitNo = viewModel.UnitNo,
            Name = viewModel.Name,
            Area = viewModel.Area,
            UnitTypeId = viewModel.UnitTypeId,
            Description = viewModel.Description,
            FreeDurationMinutes = viewModel.FreeDurationMinutes,
            HourlyRate = viewModel.HourlyRate,
            VatRate = viewModel.VatRate
        };

    private static ReservationAreaInputDto ToDto(ReservationAreaEditViewModel viewModel)
        => new()
        {
            Id = viewModel.Id,
            UnitNo = viewModel.UnitNo,
            Name = viewModel.Name,
            Area = viewModel.Area,
            UnitTypeId = viewModel.UnitTypeId,
            Description = viewModel.Description,
            FreeDurationMinutes = viewModel.FreeDurationMinutes,
            HourlyRate = viewModel.HourlyRate,
            VatRate = viewModel.VatRate
        };
}
