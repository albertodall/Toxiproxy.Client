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
        public ToxicDirection Stream => Enum.Parse<ToxicDirection>(_configuration.Stream);

        public float Toxicity
        {
            get => _configuration.Toxicity;
            set => _configuration.Toxicity = value;
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

        public virtual void EnsureConfigurationIsValid()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ToxicConfigurationException(nameof(Name), "You must prvide a name for the toxic.");
            }

            if (string.IsNullOrWhiteSpace(Type))
            {
                throw new ToxicConfigurationException(nameof(Type), "You must specify the type of toxic you are going to set up.");
            }

            if (Toxicity < 0.0f || Toxicity > 1.0f)
            {
                throw new ToxicConfigurationException(nameof(Toxicity), "Toxicity value must be between 0.0 and 1.0.");
            }
        }

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
