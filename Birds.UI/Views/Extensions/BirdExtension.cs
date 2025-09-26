using Birds.Application.DTOs;
using System.Globalization;
using System.Windows.Markup;

namespace Birds.UI.Views.Extensions
{
    [MarkupExtensionReturnType(typeof(BirdDTO))]
    public class BirdExtension : MarkupExtension
    {
        public string Name { get; set; } = "Птица";
        public string? Description { get; set; }
        public string? Arrival { get; set; }
        public string? Departure { get; set; }
        public bool IsAlive { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var arrival = string.IsNullOrWhiteSpace(Arrival)
                ? DateOnly.FromDateTime(DateTime.Today)
                : DateOnly.Parse(Arrival, CultureInfo.InvariantCulture);

            DateOnly? departure = string.IsNullOrWhiteSpace(Departure)
                ? null
                : DateOnly.Parse(Departure!, CultureInfo.InvariantCulture);

            return new BirdDTO(
                Guid.NewGuid(),
                Name,
                Description,
                arrival,
                departure,
                IsAlive
            );
        }
    }
}
