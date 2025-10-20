using System.Text;
using System.Text.Json;

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
        public Toxic[] Toxics { get; private set; }

        internal ProxyConfiguration Configuration => _configuration;

        public async Task SetNameAsync(string name, CancellationToken cancellationToken = default)
        {
            _configuration.Name = name;
            await UpdateAsync(cancellationToken);
        }

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
                if (ServerSupportsHttpPatchForProxyUpdates)
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
            await Task.WhenAll(toxics.Select(toxic => RemoveToxicAsync(toxic.Name)));
        }

        public async Task<LatencyToxic> AddLatencyToxicAsync(Action<LatencyToxic> builder, CancellationToken cancellationToken = default)
        {
            var toxicConfiguration = new LatencyToxic(new ToxicConfiguration());
            builder(toxicConfiguration);
            return (LatencyToxic)await CreateToxicAsync(toxicConfiguration.Configuration, cancellationToken);
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
        private bool ServerSupportsHttpPatchForProxyUpdates 
        { 
            get
            {
                Version supportsPatchMethodForUpdates = new("2.6.0");
                return !(new Version(_client.ServerVersion).CompareTo(supportsPatchMethodForUpdates) < 0);
            }
        }
    }
}

