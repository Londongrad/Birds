using Birds.Application.DTOs;

namespace Birds.UI.Services.Statistics.Interfaces;

public interface IBirdStatisticsCalculator
{
    BirdStatisticsSnapshot Calculate(IEnumerable<BirdDTO> birds, int? selectedYear);
}