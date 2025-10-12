using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    public abstract class Toxic
    {
        protected Toxic(ToxicConfiguration config)
        {
            Name = config.Name;
            Type = config.Type;
            Stream = config.Stream;
            Toxicity = config.Toxicity;
            Attributes = config.Attributes;
        }

        public string Name { get; private set; } = string.Empty;
        public string Type { get; private set; } = string.Empty;
        public string Stream { get; private set; } = string.Empty;
        public float Toxicity { get; set; } = 1.0f;
        protected Dictionary<string, object> Attributes { get; private set; } = [];

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

        internal ToxicConfiguration Configuration => 
            new()
            {
                Name = Name,
                Type = Type,
                Stream = Stream,
                Toxicity = Toxicity,
                Attributes = Attributes
            };

        public override string ToString()
        {
            var attributes = string.Join(", ", Attributes.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return $"{Name}: type={Type} stream={Stream} toxicity={Toxicity:F2} attributes=[{attributes}]";
        }
    }

    public sealed class ToxicConfiguration
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
