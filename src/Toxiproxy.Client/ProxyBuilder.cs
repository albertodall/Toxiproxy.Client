using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    public sealed record ProxyConfiguration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("listen")]
        public string Listen { get; set; } = string.Empty;

        [JsonPropertyName("upstream")]
        public string Upstream { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true; // Proxies are enabled by default

        [JsonPropertyName("toxics")]
        public ToxicConfiguration[] Toxics { get; set; } = Array.Empty<ToxicConfiguration>();

        public void EnsureConfigurationIsValid()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ProxyConfigurationException(nameof(Name));
            }

            if (string.IsNullOrWhiteSpace(Listen))
            {
                throw new ProxyConfigurationException(nameof(Listen));
            }

            if (string.IsNullOrWhiteSpace(Upstream))
            {
                throw new ProxyConfigurationException(nameof(Upstream));
            }
        }
    }

    //public sealed class ProxyBuilder
    //{
    //    private readonly ToxiproxyClient _client;
    //    private readonly ProxyConfiguration _configuration = new();

    //    internal ProxyBuilder(ToxiproxyClient client)
    //    {
    //        _client = client;
    //    }

    //    public ProxyBuilder WithName(string name)
    //    {
    //        _configuration.Name = name;
    //        return this;
    //    }

    //    public ProxyBuilder WithListeningAddress(string listeningAddress)
    //    {
    //        _configuration.Listen = listeningAddress;
    //        return this;
    //    }

    //    public ProxyBuilder WithUpstream(string upstream)
    //    {
    //        _configuration.Upstream = upstream;
    //        return this;
    //    }

    //    public Proxy Build()
    //    {
    //        return new Proxy(_client, _configuration);
    //    }
    //}
}

