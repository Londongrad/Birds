using Birds.Application.DTOs;
using System.Collections;

namespace Birds.UI.Views.Helpers
{
    /// <summary>
    /// Класс-компаратор для сортировки птиц в пользовательском интерфейсе.
    /// </summary>
    /// <remarks>
    /// Выполняет сортировку по нескольким критериям:
    /// 1) Птицы без даты отправления (Departure == null) идут первыми.
    /// 2) Затем по дате отправления (новее выше).
    /// 3) Затем по признаку жизни (живые выше мёртвых).
    /// 4) Затем по дате прибытия (новее выше).
    /// 5) Затем по имени (по возрастанию).
    /// </remarks>
    public sealed class BirdComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is not BirdDTO a || y is not BirdDTO b) return 0;

            // 1) Departure: null первыми
            if (a.Departure is null && b.Departure is not null) return -1;
            if (a.Departure is not null && b.Departure is null) return 1;

            // 2) Если оба имеют Departure — по дате убыв. (новее выше)
            if (a.Departure is not null && b.Departure is not null)
            {
                int c = b.Departure.Value.CompareTo(a.Departure.Value);
                if (c != 0) return c;
            }

            // 3) IsAlive: true выше false
            {
                int c = b.IsAlive.CompareTo(a.IsAlive);
                if (c != 0) return c;
            }

            // 4) Arrival: убыв. (новее выше)
            {
                int c = b.Arrival.CompareTo(a.Arrival);
                if (c != 0) return c;
            }

            // 5) Name: по возрастанию
            return string.Compare(a.Name, b.Name, StringComparison.CurrentCulture);
        }
    }

}
