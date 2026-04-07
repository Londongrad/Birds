using Birds.Domain.Enums;
using Birds.Shared.Localization;
using System.Globalization;

namespace Birds.Domain.Extensions
{
    public static class BirdsNameExtension
    {
        public static string ToDisplayName(this BirdsName bird, CultureInfo? culture = null)
            => bird switch
            {
                BirdsName.Воробей => AppText.Get("BirdName.Воробей", culture),
                BirdsName.Щегол => AppText.Get("BirdName.Щегол", culture),
                BirdsName.Амадин => AppText.Get("BirdName.Амадин", culture),
                BirdsName.Дубонос => AppText.Get("BirdName.Дубонос", culture),
                BirdsName.Большак => AppText.Get("BirdName.Большак", culture),
                BirdsName.Гайка => AppText.Get("BirdName.Гайка", culture),
                BirdsName.Поползень => AppText.Get("BirdName.Поползень", culture),
                _ => AppText.Get("BirdName.Unknown", culture)
            };
    }
}
