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

        public async Task EnableAsync(CancellationToken cancellationToken = default)
        {
            _configuration.Enabled = true;
            await UpdateProxyAsync(cancellationToken);
        }

        public async Task DisableAsync(CancellationToken cancellationToken = default)
        {
            _configuration.Enabled = false;
            await UpdateProxyAsync(cancellationToken);
        }

        public async Task<T?> GetToxicAsync<T>(string name, CancellationToken cancellationToken = default) where T : Toxic
        {
            var response = await ToxiproxyClient.HttpClient.GetAsync($"{_client.BaseUrl}/proxies/{Name}/toxics/{name}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var toxicData = JsonSerializer.Deserialize<ToxicConfiguration>(json, JsonOptions.Default);
            if (toxicData is not null)
            {
                return ToxicFactory.CreateToxic<T>(toxicData);
            }

            return null;
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
            try
            {
                var response = await ToxiproxyClient.HttpClient.DeleteAsync($"{_client.BaseUrl}/proxies/{Name}/toxics/{name}", cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Failed to delete toxic '{name}' on proxy '{Name}'", ex);
            }
        }

        public async Task<LatencyToxic> AddLatencyToxicAsync(string name, ToxicDirection direction, Action<LatencyToxic> builder, CancellationToken cancellationToken = default)
        {
            var toxic = new LatencyToxic(new ToxicConfiguration()
            {
                Name = name,
                Stream = Enum.GetName(typeof(ToxicDirection), direction).ToLowerInvariant()
            });

            builder(toxic);
            return (LatencyToxic)await CreateToxicAsync(toxic.Configuration, cancellationToken);
        }

        private async Task UpdateProxyAsync(CancellationToken cancellationToken = default)
        {
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
        /// Official deprecation will start with version 3.0.0.
        /// This property allows choose the supported update HTTP method according to the detected version, 
        /// and we can support both ways without breaking.
        /// <see href="https://github.com/Shopify/toxiproxy/blob/main/CHANGELOG.md#260---2023-08-22">See Toxiproxy changelog.</see>
        /// </summary>
        /// <returns>Returns <see cref="true"/> if server version is 2.6.0 or above.</returns>
        private bool ServerRequiresHttpPatchForProxyUpdates => !(new Version(_client.ServerVersion).CompareTo(new Version("2.6.0")) < 0);
    }

    /// <summary>
    /// Represents the configuration settings for a proxy.
    /// </summary>
    public sealed record ProxyConfiguration
    {
        /// <summary>
        /// Name of the proxy.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Address, expressed as address:port, where the proxy listens for incoming connections.
        /// </summary>
        [JsonPropertyName("listen")]
        public string Listen { get; set; } = string.Empty;

        /// <summary>
        /// Address or hostname of the service we are proxying to.
        /// </summary>
        [JsonPropertyName("upstream")]
        public string Upstream { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the proxy is enabled or disabled.
        /// </summary>
        /// <remarks>
        /// When created, proxies are enabled by default.
        /// </remarks>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// List of toxics configured on the proxy.
        /// </summary>
        [JsonPropertyName("toxics")]
        public ToxicConfiguration[] Toxics { get; set; } = Array.Empty<ToxicConfiguration>();
    }
}

