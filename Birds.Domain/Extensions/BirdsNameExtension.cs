using Birds.Domain.Enums;

namespace Birds.Domain.Extensions
{
    public static class BirdsNameExtension
    {
        public static string ToDisplayName(this BirdsName bird)
        => bird switch
        {
            // Hardcoded for now, but when localization is implemented, the data can be retrieved from resources.
            BirdsName.Воробей => "Воробей", // => Resources.Sparrow
            BirdsName.Щегол => "Щегол",
            BirdsName.Амадин => "Амадин",
            BirdsName.Дубонос => "Дубонос",
            BirdsName.Большак => "Большак",
            BirdsName.Гайка => "Гайка",
            BirdsName.Поползень => "Поползень",
            _ => "Неизвестно"
        };
    }
}