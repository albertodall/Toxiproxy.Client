using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    /// <summary>
    /// Represents a proxy entry on the Toxiproxy server.
    /// </summary>
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

        /// <summary>
        /// Name of the <see cref="Proxy"/>."/>
        /// </summary>
        public string Name => _configuration.Name;

        /// <summary>
        /// Listening address and port of the <see cref="Proxy"/>, in [address]:[port] format.
        /// </summary>
        public string Listen => _configuration.Listen;

        /// <summary>
        /// Address or hostname of the upstream service of the <see cref="Proxy"/>."/>
        /// </summary>
        public string Upstream => _configuration.Upstream;

        /// <summary>
        /// Whether the <see cref="Proxy"/> is enabled or disabled.
        /// </summary>
        public bool Enabled => _configuration.Enabled;

        /// <summary>
        /// List of <see cref="Toxic"/>s configured on the <see cref="Proxy"/>."/>
        /// </summary>
        public IReadOnlyCollection<Toxic> Toxics { get; private set; }

        /// <summary>
        /// Sets a proxy as enabled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task EnableAsync(CancellationToken cancellationToken = default)
        {
            _configuration.Enabled = true;
            await UpdateProxyAsync(cancellationToken);
        }

        /// <summary>
        /// Sets a proxy as disabled.
        /// </summary>
        public async Task DisableAsync(CancellationToken cancellationToken = default)
        {
            _configuration.Enabled = false;
            await UpdateProxyAsync(cancellationToken);
        }

        /// <summary>
        /// Reads current configuration of a <see cref="Toxic"/>.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="Toxic"> we're interested in.</typeparam>
        /// <param name="name">Name of the <see cref="Toxic"/> we're interested in.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The instance of the <see cref="Toxic"/> configured on the server, or <see langword="null"> if the toxic with the specified name does not exist.</returns>
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

        /// <summary>
        /// Update the configuration of a <see cref="Toxic"/>.
        /// </summary>
        /// <param name="toxic"><see cref="Toxic"/> to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ToxiproxyConnectionException"></exception>
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

        /// <summary>
        /// Removes a <see cref="Toxic"/> from the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="name">Name of the <see cref="Toxic"/> we want to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ToxiproxyConnectionException"></exception>
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

        /// <summary>
        /// Add a <see cref="LatencyToxic"/> to the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="name">Name of the <see cref="Toxic"> to create.</param>
        /// <param name="direction">Working direction of the <see cref="Toxic"/> (upstream or downstream).</param>
        /// <param name="builder">Toxic configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="LatencyToxic"/>.</returns>
        public async Task<LatencyToxic> AddLatencyToxicAsync(string name, ToxicDirection direction, Action<LatencyToxic> builder, CancellationToken cancellationToken = default)
        {
            var toxic = new LatencyToxic(new ToxicConfiguration()
            {
                Name = name,
                Stream = direction,
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
        /// HTTP POST for updates won't be allowed anymore starting from version 3.0.0.
        /// This property allows choose the supported update HTTP method according to the detected version, 
        /// and we can support both ways without breaking.
        /// <see href="https://github.com/Shopify/toxiproxy/blob/main/CHANGELOG.md#260---2023-08-22">See Toxiproxy changelog.</see>
        /// </summary>
        /// <returns>Returns <see cref="true"/> if server version is 2.6.0 or above.</returns>
        /// <remarks>
        /// The version check is needed, because if you use HTTP PATCH method on servers below 2.6.0, you get a "405 Method Not Allowed" response.
        /// </remarks>
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
        /// Address or hostname of the service we are proxying.
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
        public IReadOnlyCollection<ToxicConfiguration> Toxics { get; set; } = [];
    }
}

