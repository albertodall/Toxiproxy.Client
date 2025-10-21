using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    public enum ToxicDirection
    {
        Upstream,
        Downstream
    }

    public sealed class Proxy
    {
        private readonly ToxiproxyClient _client;
        private readonly ProxyConfiguration _configuration = new();

        internal Proxy(ToxiproxyClient client, ProxyConfiguration config)
        { 
            _client = client;
            _configuration = config;
            Toxics = config.Toxics.Select(ToxicFactory.CreateToxic).ToArray();
        }

        // Core Proxy Properties
        public string Name => _configuration.Name;
        public string Listen => _configuration.Listen;
        public string Upstream => _configuration.Upstream;
        public bool Enabled => _configuration.Enabled;
        public IReadOnlyCollection<Toxic> Toxics { get; private set; }

        public async Task SetUpstreamAsync(string upstream, CancellationToken cancellationToken = default)
        {
            _configuration.Upstream = upstream;
            await UpdateAsync(cancellationToken);
        }

        public async Task SetListeningAddressAsync(string listeningAddress, CancellationToken cancellationToken = default)
        {
            _configuration.Listen = listeningAddress;
            await UpdateAsync(cancellationToken);
        }

        public async Task EnableAsync(CancellationToken cancellationToken = default)
        {
            _configuration.Enabled = true;
            await UpdateAsync(cancellationToken);
        }

        public async Task DisableAsync(CancellationToken cancellationToken = default)
        {
            _configuration.Enabled = false;
            await UpdateAsync(cancellationToken);
        }

        public async Task UpdateAsync(CancellationToken cancellationToken = default)
        {
            _configuration.EnsureConfigurationIsValid();

            try
            {
                var json = JsonSerializer.Serialize(_configuration, JsonOptions.Default);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (ServerRequiresHttpPatchForProxyUpdates)
                {
                    response = await ToxiproxyClient.HttpClient.PatchAsync($"{_client.BaseUrl}/proxies/{Name}", content, cancellationToken);
                }
                else
                {
                    response = await ToxiproxyClient.HttpClient.PostAsync($"{_client.BaseUrl}/proxies/{Name}", content, cancellationToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Failed to update proxy '{Name}'", ex);
            }
        }

        public async Task<T?> GetToxicAsync<T>(string name, CancellationToken cancellationToken = default) where T : Toxic
        {
            var toxic = (await GetToxicsAsync(cancellationToken)).FirstOrDefault(t => t.Name == name);
            if (toxic is not null)
            {
                return ToxicFactory.CreateToxic<T>(toxic.Configuration);
            }

            return null;
        }

        public async Task<Toxic[]> GetToxicsAsync(CancellationToken cancellationToken = default)
        {
            var response = await ToxiproxyClient.HttpClient.GetAsync($"{_client.BaseUrl}/proxies/{Name}/toxics", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var toxicsData = JsonSerializer.Deserialize<ToxicConfiguration[]>(json, JsonOptions.Default);
            if (toxicsData is null)
            {
                throw new JsonException("Failed to deserialize toxics data.");
            }

            return toxicsData.Select(ToxicFactory.CreateToxic).ToArray();
        }

        public async Task UpdateToxicAsync(Toxic toxic, CancellationToken cancellationToken = default)
        {
            try
            {
                var toxicConfig = JsonSerializer.Serialize(toxic.Configuration, JsonOptions.Default);
                var content = new StringContent(toxicConfig, Encoding.UTF8, "application/json");

                HttpResponseMessage response;
                if (ServerRequiresHttpPatchForProxyUpdates)
                {
                    response = await ToxiproxyClient.HttpClient.PatchAsync($"{_client.BaseUrl}/proxies/{Name}/toxics/{toxic.Name}", content, cancellationToken);
                }
                else
                {
                    response = await ToxiproxyClient.HttpClient.PostAsync($"{_client.BaseUrl}/proxies/{Name}/toxics/{toxic.Name}", content, cancellationToken);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Failed to update toxic '{toxic.Name}' on proxy '{Name}'", ex);
            }
        }

        public async Task RemoveToxicAsync(string name, CancellationToken cancellationToken = default)
        {
            var response = await ToxiproxyClient.HttpClient.DeleteAsync($"{_client.BaseUrl}/proxies/{Name}/toxics/{name}", cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<LatencyToxic> AddLatencyToxicAsync(string name, ToxicDirection direction, Action<LatencyToxic> builder, CancellationToken cancellationToken = default)
        {
            var toxic = new LatencyToxic(new ToxicConfiguration()
            {
                Name = name,
                Stream = Enum.GetName(typeof(ToxicDirection), direction)
            });

            builder(toxic);
            toxic.EnsureConfigurationIsValid();
            return (LatencyToxic)await CreateToxicAsync(toxic.Configuration, cancellationToken);
        }

        private async Task<Toxic> CreateToxicAsync(ToxicConfiguration config, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(config, JsonOptions.Default);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await ToxiproxyClient.HttpClient.PostAsync($"{_client.BaseUrl}/proxies/{Name}/toxics", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var createdToxic = JsonSerializer.Deserialize<ToxicConfiguration>(responseJson, JsonOptions.Default);
            if (createdToxic is null)
            {
                throw new JsonException("Failed to deserialize the toxic data.");
            }

            return ToxicFactory.CreateToxic(createdToxic);
        }

        /// <summary>
        /// Starting from version 2.6.0, Toxiproxy server supports HTTP PATCH method for proxy updates, 
        /// and started deprecating updates using HTTP POST.
        /// This property checks the server version so to allow choosing the preferred update HTTP method 
        /// according to the detected version, and we can support both ways without breaking.
        /// <see href="https://github.com/Shopify/toxiproxy/blob/main/CHANGELOG.md#260---2023-08-22">See Toxiproxy changelog.</see>
        /// </summary>
        /// <returns>Returns <see cref="true"/> if server version is 2.6.0 or above.</returns>
        private bool ServerRequiresHttpPatchForProxyUpdates 
        { 
            get
            {
                return !(new Version(_client.ServerVersion).CompareTo(new Version("2.6.0")) < 0);
            }
        }
    }

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
                throw new ProxyConfigurationException(nameof(Name), $"Proxy must have a name.");
            }

            if (string.IsNullOrWhiteSpace(Listen))
            {
                throw new ProxyConfigurationException(nameof(Listen), "You must set a listening address as [ip address]:[port].");
            }

            if (string.IsNullOrWhiteSpace(Upstream))
            {
                throw new ProxyConfigurationException(nameof(Upstream), "You must set an upstream address to proxy for as [ip address]:[port].");
            }
        }
    }
}

