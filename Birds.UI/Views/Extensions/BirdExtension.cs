using Birds.Domain.Entities;
using Birds.Domain.Enums;
using System.Windows.Markup;

namespace Birds.UI.Views.Extensions
{
    [MarkupExtensionReturnType(typeof(Bird))]
    public class BirdExtension : MarkupExtension
    {
        public Guid? Id { get; set; }
        public BirdsName Name { get; set; }
        public string? Description { get; set; }
        public DateOnly? Arrival { get; set; }
        public DateOnly? Departure { get; set; }
        public bool IsAlive { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var id = Id ?? Guid.NewGuid();
            var arrival = Arrival ?? DateOnly.FromDateTime(DateTime.Today);

            var bird = new Bird(id, Name, Description, arrival, IsAlive);

            if (Departure.HasValue)
                bird.SetDeparture(Departure.Value);

            return bird;
        }
    }
}
