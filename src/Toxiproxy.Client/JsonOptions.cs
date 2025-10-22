using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    internal static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new VersionConverter()
            }
        };
    }

    /// <summary>
    /// Deserializes the version information from the Toxiproxy server response in a <see cref="Version" object/>.
    /// This is useful for checking for the compatibility between the client API and the Toxiproxy server.
    /// </summary>
    /// <remarks>
    /// The version endpoint returns a JSON object in the format: { "version": "x.y.z" }.
    /// </remarks>
    internal sealed class VersionConverter : JsonConverter<Version>
    {
        public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            doc.RootElement.TryGetProperty("version", out var version);
            return Version.Parse(version.GetString());
        }

        public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
