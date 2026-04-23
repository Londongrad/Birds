using Birds.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Birds.Infrastructure.Persistence.Configurations;

internal static class UtcDateTimeConverters
{
    public static readonly ValueConverter<DateTime, DateTime> Required = new(
        value => UtcDateTimeStorage.NormalizeForStorage(value),
        value => UtcDateTimeStorage.FromStorage(value));

    public static readonly ValueConverter<DateTime?, DateTime?> Optional = new(
        value => UtcDateTimeStorage.NormalizeForStorage(value),
        value => UtcDateTimeStorage.FromStorage(value));
}
