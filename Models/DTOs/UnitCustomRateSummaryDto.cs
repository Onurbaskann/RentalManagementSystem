namespace KiraTakip.Models.Dtos;

public class UnitCustomRateSummaryDto
{
    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string? UnitNo { get; set; }
    public List<UnitCustomRateDto> Rateler { get; set; } = [];
}
