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
        private ProxyConfiguration _configuration = new();

        internal Proxy(ToxiproxyClient client, string name, string listen, string upstream, bool enabled = true)
        {
            _configuration.Name = name;
            _configuration.Listen = listen;
            _configuration.Upstream = upstream;
            _configuration.Enabled = enabled;
            _client = client;
        }

        internal Proxy(ToxiproxyClient client, ProxyConfiguration config)
            : this(client, config.Name, config.Listen, config.Upstream, config.Enabled)
        { }

        internal ToxiproxyClient Client => _client;

        // Core Proxy Properties
        public string Name => _configuration.Name;
        public string Listen => _configuration.Listen;
        public string Upstream => _configuration.Upstream;
        public bool Enabled => _configuration.Enabled;

        // Proxy Management Methods
        public async Task UpdateUpstreamAsync(string upstream, CancellationToken cancellationToken = default)
        {
            var configUpdate = _configuration with { Upstream = upstream };
            await UpdateProxyAsync(configUpdate, cancellationToken);
        }

        public async Task EnableAsync(CancellationToken cancellationToken = default)
        {
            var configUpdate = _configuration with { Enabled = true };
            await UpdateProxyAsync(configUpdate, cancellationToken);
        }

        public async Task DisableAsync(CancellationToken cancellationToken = default)
        {
            var configUpdate = _configuration with { Enabled = false };
            await UpdateProxyAsync(configUpdate, cancellationToken);
        }

        private async Task UpdateProxyAsync(ProxyConfiguration data, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(data, JsonOptions.Default);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            if (ServerSupportsHttpPatchForProxyUpdates)
            {
                response = await ToxiproxyClient.HttpClient.PatchAsync($"{_client.BaseUrl}/proxies/{Name}", content, cancellationToken);
            }
            else
            {
                response = await ToxiproxyClient.HttpClient.PostAsync($"{_client.BaseUrl}/proxies/{Name}", content, cancellationToken);
            }

            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var updatedProxyConfiguration = JsonSerializer.Deserialize<ProxyConfiguration>(responseJson, JsonOptions.Default);
            if (updatedProxyConfiguration is null)
            {
                throw new JsonException("Failed to deserialize the updated proxy data.");
            }
            
            _configuration = updatedProxyConfiguration;
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
                if (ServerSupportsHttpPatchForProxyUpdates)
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

        public async Task RemoveAllToxicsAsync(CancellationToken cancellationToken = default)
        {
            var toxics = await GetToxicsAsync(cancellationToken);
            var removeToxicsTasks = toxics.Select(toxic => RemoveToxicAsync(toxic.Name));
            await Task.WhenAll(removeToxicsTasks);
        }

        public async Task<LatencyToxic> AddLatencyToxicAsync(string name, ToxicDirection direction, float toxicity, int latency, int jitter, CancellationToken cancellationToken = default)
        {
            var toxicData = new ToxicConfiguration
            {
                Name = name,
                Type = "latency",
                Stream = direction.ToString().ToLowerInvariant(),
                Toxicity = toxicity,
                Attributes = new Dictionary<string, object>
                {
                    {"latency", latency},
                    {"jitter", jitter}
                }
            };

            return (LatencyToxic)await CreateToxicAsync(toxicData, cancellationToken);
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
        /// This method helps checking the server version so to allow using the preferred update HTTP method 
        /// according to the server version, and we can support both ways without breaking.
        /// <see href="https://github.com/Shopify/toxiproxy/blob/main/CHANGELOG.md#260---2023-08-22">See Toxiproxy changelog.</see>
        /// </summary>
        /// <returns><see cref="true"/> if server version is 2.6.0 or above.</returns>
        private bool ServerSupportsHttpPatchForProxyUpdates 
        { 
            get
            {
                const string supportsPatchMethodForUpdates = "2.6.0";
                return string.Compare(_client.ServerVersion, supportsPatchMethodForUpdates, StringComparison.Ordinal) >= 0;
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
        public bool Enabled { get; set; }
    }
}

