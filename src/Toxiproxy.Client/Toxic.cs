using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    public abstract class Toxic
    {
        private readonly ToxicConfiguration _configuration;

        protected Toxic(ToxicConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string Name => _configuration.Name;
        public string Type => _configuration.Type;
        public string Stream => _configuration.Stream;
        public float Toxicity => _configuration.Toxicity;
        protected Dictionary<string, object> Attributes => _configuration.Attributes;

        public T GetAttribute<T>(string key)
        {
            if (Attributes.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is JsonElement element)
                    {
                        var result = JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions.Default);
                        return result is not null ? result : default!;
                    }
                    return (T)Convert.ChangeType(value, typeof(T), CultureInfo.CurrentCulture);
                }
                catch
                {
                    return default!;
                }
            }

            return default!;
        }

        internal ToxicConfiguration Configuration => _configuration;

        public override string ToString()
        {
            var attributes = string.Join(", ", Attributes.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return $"{Name}: type={Type} stream={Stream} toxicity={Toxicity:F2} attributes=[{attributes}]";
        }
    }

    public sealed record ToxicConfiguration
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public string Stream { get; set; } = string.Empty;

        [JsonPropertyName("toxicity")]
        public float Toxicity { get; set; } = 1.0f;

        [JsonPropertyName("attributes")]
        public Dictionary<string, object> Attributes { get; set; } = [];
    }
}
