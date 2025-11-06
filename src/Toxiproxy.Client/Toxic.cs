using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    /// <summary>
    /// Represents a toxic that can be applied to a proxy.
    /// </summary>
    public abstract class Toxic
    {
        private string _name = string.Empty;
        private float _toxicity = 1.0f;

        protected Toxic(string toxicType)
        {
            Name = $"{toxicType}_{Stream}";
            Type = toxicType;
        }

        internal Toxic(ToxicConfiguration configuration)
            : this(configuration.Type)
        {
            Name = configuration.Name;
            Stream = configuration.Stream;
            Toxicity = configuration.Toxicity;
            Attributes = configuration.Attributes;
        }

        /// <summary>
        /// Name of the toxic.
        /// </summary>
        /// <remarks>
        /// The name must be unique per proxy.
        /// </remarks>
        public string Name 
        {
            get { return _name; }
            set 
            { 
                if (string.IsNullOrEmpty(value))
                {
                    throw new ToxicConfigurationException(nameof(Name), "You must provide a name for the toxic.");
                }

                _name = value;
            }
        }

        /// <summary>
        /// Type of toxic.
        /// It defines the behavior of the toxic. Examples include "latency", "bandwidth", "timeout", etc.
        /// </summary>
        /// <remarks>
        /// The toxic type cannot be changed after creation.
        /// </remarks>
        public string Type { get; private set; }

        /// <summary>
        /// Toxic working direction: upstream or downstream.
        /// </summary>
        public ToxicDirection Stream { get; set; } = ToxicDirection.Downstream;

        /// <summary>
        /// Toxicity value of the toxic.
        /// This value represents the probability of the toxic being applied to a link (defaults to 1.0, 100%).
        /// </summary>
        public float Toxicity
        {
            get => _toxicity;
            set
            {
                if (value < 0.0f || value > 1.0f)
                {
                    throw new ToxicConfigurationException(nameof(Toxicity), "Toxicity must be a value between 0.0 and 1.0.");
                }

                _toxicity = value;
            } 
        }

        /// <summary>
        /// Map of attributes/parameters applied to the toxic.
        /// </summary>
        public Dictionary<string, object> Attributes { get; private set; } = new();

        /// <summary>
        /// Reads a single attribute from the Attributes dictionary.
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

        public override string ToString()
        {
            var attributes = string.Join(", ", Attributes.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return $"{Name}: type={Type} stream={Stream} toxicity={Toxicity:F2} attributes=[{attributes}]";
        }
    }

    /// <summary>
    /// Set of parameters needed to configure a toxic.
    /// Helper class for serialization/deserialization of toxic data.
    /// </summary>
    internal sealed record ToxicConfiguration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public string Stream { get; set; } = ToxicDirection.Downstream;

        [JsonPropertyName("toxicity")]
        public float Toxicity { get; set; } = 1.0f;

        [JsonPropertyName("attributes")]
        public Dictionary<string, object> Attributes { get; set; } = [];
    }
}
