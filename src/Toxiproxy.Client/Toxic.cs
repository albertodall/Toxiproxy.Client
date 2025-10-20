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

        public string Name 
        { 
            get => _configuration.Name;
            set => _configuration.Name = value;
        }

        public string Type => _configuration.Type;

        public ToxicDirection Stream
        {
            get => Enum.Parse<ToxicDirection>(_configuration.Stream);
            set => _configuration.Stream = Enum.GetName(typeof(ToxicDirection), value);
        }

        public float Toxicity
        {
            get => _configuration.Toxicity;
            set => _configuration.Toxicity = value;
        }

        protected Dictionary<string, object> Attributes => _configuration.Attributes;

        protected T GetAttribute<T>(string key)
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
