using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    public abstract class Toxic
    {
        private readonly ToxicConfiguration _configuration;

        protected Toxic(string toxicType, ToxicConfiguration configuration)
        {
            if (string.IsNullOrEmpty(configuration.Name))
            {
                throw new ToxicConfigurationException(nameof(configuration.Name), "You must provide a name for the toxic.");
            }

            if (configuration.Toxicity < 0.0f || configuration.Toxicity > 1.0f)
            {
                throw new ToxicConfigurationException(nameof(configuration.Toxicity), "Toxicity must be a value between 0.0 and 1.0.");
            }

            _configuration = configuration;
            _configuration.Type = toxicType;
        }

        /// <summary>
        /// Name of the toxic.
        /// </summary>
        /// <remarks>
        /// The name must be unique per proxy, and it cannot be changed after creation.
        /// </remarks>
        public string Name => _configuration.Name;

        /// <summary>
        /// Type of toxic.
        /// It defines the behavior of the toxic. Examples include "latency", "bandwidth", "timeout", etc.
        /// </summary>
        /// <remarks>
        /// The toxic type cannot be changed after creation.
        /// </remarks>
        public string Type => _configuration.Type;

        /// <summary>
        /// Toxic working direction: upstream or downstream.
        /// </summary>
        /// <remarks>
        /// The stream direction cannot be changed after creation.
        /// </remarks>
        public ToxicDirection Stream => _configuration.Stream;

        public float Toxicity
        {
            get => _configuration.Toxicity;
            set
            {
                if (value < 0.0f || value > 1.0f)
                {
                    throw new ToxicConfigurationException(nameof(Toxicity), "Toxicity must be a value between 0.0 and 1.0.");
                }

                _configuration.Toxicity = value;
            } 
        }

        protected Dictionary<string, object> Attributes => _configuration.Attributes;

        /// <summary>
        /// Read a single attribute from the Attributes dictionary.
        /// </summary>
        /// <typeparam name="T">The expected type of the attribute's value.</typeparam>
        /// <param name="key">The attribute name.</param>
        /// <returns>The value of the attribute stored in the dictionary.</returns>
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
        public string Stream { get; set; } = "downstream"; // Toxics work downstream by default

        [JsonPropertyName("toxicity")]
        public float Toxicity { get; set; } = 1.0f;

        [JsonPropertyName("attributes")]
        public Dictionary<string, object> Attributes { get; set; } = [];
    }
}
