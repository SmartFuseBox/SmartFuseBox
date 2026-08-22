using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerControlHubApp.Converters
{
    // csharp
    public sealed class SingleOrArrayConverter<T> : JsonConverter<T[]>
    {
        public override T[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
                return JsonSerializer.Deserialize<T[]>(ref reader, options)!;

            T single = JsonSerializer.Deserialize<T>(ref reader, options);
            return [single!];
        }

        public override void Write(Utf8JsonWriter writer, T[] value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
