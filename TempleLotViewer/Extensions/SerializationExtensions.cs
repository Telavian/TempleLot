using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace TempleLotViewer.Extensions
{
    public static class SerializationExtensions
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters = { new JsonStringEnumConverter() }
        };

        public static string SerializeToJson<T>(this T item)
        {
            return JsonSerializer.Serialize(item, _options);
        }

        public static T? DeserializeFromJson<T>(this string? json)
        {
            if (json == null) return default;
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        public static T? DeserializeFromJson<T>(this byte[]? json)
        {
            if (json == null) return default;
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        public static ValueTask<T?> DeserializeFromJsonAsync<T>(this Stream? json)
        {
            if (json == null) return ValueTask.FromResult<T?>(default);
            return JsonSerializer.DeserializeAsync<T>(json, _options);
        }
    }
}
