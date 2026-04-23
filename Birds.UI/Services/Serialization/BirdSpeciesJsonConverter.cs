using System.Text.Json;
using System.Text.Json.Serialization;
using Birds.Domain.Enums;

namespace Birds.UI.Services.Serialization;

public sealed class BirdSpeciesJsonConverter : JsonConverter<BirdSpecies>
{
    public override BirdSpecies Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => ReadString(reader.GetString()),
            JsonTokenType.Number when reader.TryGetInt32(out var value) && Enum.IsDefined(typeof(BirdSpecies), value)
                => (BirdSpecies)value,
            _ => throw new JsonException("Invalid bird species value.")
        };
    }

    public override void Write(Utf8JsonWriter writer, BirdSpecies value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(BirdSpeciesCodes.ToCode(value));
    }

    private static BirdSpecies ReadString(string? value)
    {
        return BirdSpeciesCodes.Parse(value)
               ?? throw new JsonException($"Unknown bird species '{value}'.");
    }
}
