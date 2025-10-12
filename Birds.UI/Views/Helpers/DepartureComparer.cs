using Birds.Application.DTOs;
using System.Collections;

namespace Birds.UI.Views.Helpers
{
    /// <summary>
    /// A comparer class used for sorting birds in the user interface.
    /// </summary>
    /// <remarks>
    /// Performs sorting based on multiple criteria:
    /// 1) Birds without a departure date (<see cref="BirdDTO.Departure"/> == null) come first.
    /// 2) Then by departure date (newer first).
    /// 3) Then by alive status (alive above dead).
    /// 4) Then by arrival date (newer first).
    /// 5) Finally, by name (ascending order).
    /// </remarks>
    public sealed class BirdComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is not BirdDTO a || y is not BirdDTO b)
                return 0;

            // 1) Departure: null comes first
            if (a.Departure is null && b.Departure is not null) return -1;
            if (a.Departure is not null && b.Departure is null) return 1;

            // 2) If both have Departure — sort by date descending (newer first)
            if (a.Departure is not null && b.Departure is not null)
            {
                int c = b.Departure.Value.CompareTo(a.Departure.Value);
                if (c != 0) return c;
            }

            // 3) IsAlive: true before false
            {
                int c = b.IsAlive.CompareTo(a.IsAlive);
                if (c != 0) return c;
            }

            // 4) Arrival: descending (newer first)
            {
                int c = b.Arrival.CompareTo(a.Arrival);
                if (c != 0) return c;
            }

            // 5) Name: ascending order
            return string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
        }
    }
}
