using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Toxiproxy.Client
{
    /// <summary>
    /// Represents a proxy entry on the Toxiproxy server.
    /// </summary>
    public sealed class Proxy
    {
        private readonly ToxiproxyClient _client;

        internal Proxy(ToxiproxyClient client)
        { 
            _client = client;
        }

        internal Proxy(ToxiproxyClient client, ProxyConfiguration config)
            : this(client)
        {
            Name = config.Name;
            Listen = config.Listen;
            Upstream = config.Upstream;
            Enabled = config.Enabled;
            Toxics = [..config.Toxics.Select(ToxicFactory.CreateToxic)];
        }

        /// <summary>
        /// Name of the <see cref="Proxy"/>.
        /// </summary>
        /// <remarks>
        /// Once created, the name of the proxy cannot be changed.
        /// </remarks>
        public string Name
        {
            get => field;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ProxyConfigurationException(nameof(Name), "You must supply a name for this proxy.");
                }
                else if (!string.IsNullOrEmpty(Name))
                {
                    throw new ProxyConfigurationException(nameof(Name), "You cannot change the name of an existing proxy.");
                }
                field = value;
            }
        } = string.Empty;

        /// <summary>
        /// Listening address and port of the <see cref="Proxy"/>, in [address]:[port] format.
        /// </summary>
        public string Listen
        {
            get => field;
            set
            {
                if (string.IsNullOrEmpty(value) || !IsListeningAddressValid(value))
                {
                    throw new ProxyConfigurationException(nameof(Listen), "You must supply a valid listening address in the form [address]:[port].");
                }
                field = value;
            }
        } = string.Empty;

        /// <summary>
        /// Address or hostname of the upstream service of the <see cref="Proxy"/>.
        /// </summary>
        public string Upstream
        {
            get => field;
            set
            {
                if (string.IsNullOrEmpty(value) || !IsListeningAddressValid(value))
                {
                    throw new ProxyConfigurationException(nameof(Upstream), "You must supply a valid upstream address to proxy for, in the form [ip address/hostname]:[port].");
                }
                field = value;
            }
        } = string.Empty;

        /// <summary>
        /// Whether the <see cref="Proxy"/> is enabled or disabled.
        /// </summary>
        /// <remarks>
        /// When created, proxies are enabled by default.
        /// </remarks>
        public bool Enabled { get; private set; } = true;

        /// <summary>
        /// List of <see cref="Toxic"/>s configured on the <see cref="Proxy"/>.
        /// </summary>
        public IReadOnlyCollection<Toxic> Toxics { get; private set; } = [];

        /// <summary>
        /// Sets a proxy as enabled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task EnableAsync(CancellationToken cancellationToken = default)
        {
            Enabled = true;
            await UpdateProxyAsync(cancellationToken);
        }

        /// <summary>
        /// Sets a proxy as disabled.
        /// </summary>
        public async Task DisableAsync(CancellationToken cancellationToken = default)
        {
            Enabled = false;
            await UpdateProxyAsync(cancellationToken);
        }

        /// <summary>
        /// Reads current configuration of a <see cref="Toxic"/>.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="Toxic"/> we're interested in.</typeparam>
        /// <param name="name">Name of the <see cref="Toxic"/> we're interested in.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The instance of the <see cref="Toxic"/> configured on the server, or <see langword="null"/> if the toxic with the specified name does not exist.</returns>
        public async Task<T?> GetToxicAsync<T>(string name, CancellationToken cancellationToken = default) where T : Toxic
        {
            var response = await ToxiproxyClient.HttpClient.GetAsync($"{_client.BaseUrl}/proxies/{Name}/toxics/{name}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var toxicData = JsonSerializer.Deserialize<ToxicConfiguration>(json, JsonOptions.Default);
            return ToxicFactory.CreateToxic<T>(toxicData!);
        }

        /// <summary>
        /// Update the configuration of a <see cref="Toxic"/>.
        /// </summary>
        /// <param name="toxic"><see cref="Toxic"/> to update.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ToxiproxyException"></exception>
        public async Task UpdateToxicAsync(Toxic toxic, CancellationToken cancellationToken = default)
        {
            try
            {
                var toxicConfig = JsonSerializer.Serialize(toxic.GetConfiguration(), JsonOptions.Default);
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
                throw new ToxiproxyException($"Failed to update toxic '{toxic.Name}' on proxy '{Name}'", ex);
            }
        }

        /// <summary>
        /// Removes a <see cref="Toxic"/> from the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="name">Name of the <see cref="Toxic"/> to remove.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ToxiproxyException"></exception>
        public async Task RemoveToxicAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                await ToxiproxyClient.HttpClient.DeleteAsync($"{_client.BaseUrl}/proxies/{Name}/toxics/{name}", cancellationToken);
                await GetActiveToxicsAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyException($"Failed to remove toxic '{name}' on proxy '{Name}'", ex);
            }
        }

        /// <summary>
        /// Configures a <see cref="LatencyToxic"/> on the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="config">Toxic configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="LatencyToxic"/>.</returns>
        public async Task<LatencyToxic> AddLatencyToxicAsync(Action<LatencyToxic> config, CancellationToken cancellationToken = default)
        {
            LatencyToxic toxic = new();
            config(toxic);
            return (LatencyToxic)await CreateToxicAsync(toxic, cancellationToken);
        }

        /// <summary>
        /// Configures a <see cref="BandwidthToxic"/> on the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="config">Toxic configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="BandwidthToxic"/>.</returns>
        public async Task<BandwidthToxic> AddBandwidthToxicAsync(Action<BandwidthToxic> config, CancellationToken cancellationToken = default)
        {
            BandwidthToxic toxic = new();
            config(toxic);
            return (BandwidthToxic)await CreateToxicAsync(toxic, cancellationToken);
        }

        /// <summary>
        /// Configures a <see cref="TimeoutToxic"/> on the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="config">Toxic configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="TimeoutToxic"/>.</returns>
        public async Task<TimeoutToxic> AddTimeoutToxicAsync(Action<TimeoutToxic> config, CancellationToken cancellationToken = default)
        {
            TimeoutToxic toxic = new();
            config(toxic);
            return (TimeoutToxic)await CreateToxicAsync(toxic, cancellationToken);
        }

        /// <summary>
        /// Configures a <see cref="SlowCloseToxic"/> on the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="config">Toxic configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="SlowCloseToxic"/>.</returns>
        public async Task<SlowCloseToxic> AddSlowCloseToxicAsync(Action<SlowCloseToxic> config, CancellationToken cancellationToken = default)
        {
            SlowCloseToxic toxic = new();
            config(toxic);
            return (SlowCloseToxic)await CreateToxicAsync(toxic, cancellationToken);
        }

        /// <summary>
        /// Configures a <see cref="SlicerToxic"/> on the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="config">Toxic configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="SlicerToxic"/>.</returns>
        public async Task<SlicerToxic> AddSlicerToxicAsync(Action<SlicerToxic> config, CancellationToken cancellationToken = default)
        {
            SlicerToxic toxic = new();
            config(toxic);
            return (SlicerToxic)await CreateToxicAsync(toxic, cancellationToken);
        }

        /// <summary>
        /// Configures a <see cref="LimitDataToxic"/> on the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="config">Toxic configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="LimitDataToxic"/>.</returns>
        public async Task<LimitDataToxic> AddLimitDataToxicAsync(Action<LimitDataToxic> config, CancellationToken cancellationToken = default)
        {
            LimitDataToxic toxic = new();
            config(toxic);
            return (LimitDataToxic)await CreateToxicAsync(toxic, cancellationToken);
        }

        /// <summary>
        /// Configures a <see cref="ResetPeerToxic"/> on the <see cref="Proxy"/>.
        /// </summary>
        /// <param name="config">Toxic configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="ResetPeerToxic"/>.</returns>
        public async Task<ResetPeerToxic> AddResetPeerToxicAsync(Action<ResetPeerToxic> config, CancellationToken cancellationToken = default)
        {
            ResetPeerToxic toxic = new();
            config(toxic);
            return (ResetPeerToxic)await CreateToxicAsync(toxic, cancellationToken);
        }

        /// <summary>
        /// Checks all proxy parameters to ensure they are valid.
        /// </summary>
        /// <exception cref="ProxyConfigurationException"></exception>
        public void EnsureConfigurationIsValid()
        {
            if (string.IsNullOrEmpty(Name))
            {
                throw new ProxyConfigurationException(nameof(Name), "You must supply a name for this proxy.");
            }

            if (string.IsNullOrEmpty(Listen) || !IsListeningAddressValid(Listen))
            {
                throw new ProxyConfigurationException(nameof(Listen), "You must supply a valid listening address in the form [address]:[port].");
            }

            if (string.IsNullOrEmpty(Upstream) || !IsListeningAddressValid(Upstream))
            {
                throw new ProxyConfigurationException(nameof(Upstream), "You must supply a valid upstream address to proxy for, in the form [ip address/hostname]:[port].");
            }
        }

        private async Task UpdateProxyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(this.GetConfiguration(), JsonOptions.Default);
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
                throw new ToxiproxyException($"Failed to update proxy '{Name}'", ex);
            }
        }

        private async Task GetActiveToxicsAsync(CancellationToken cancellationToken = default)
        {
            var response = await ToxiproxyClient.HttpClient.GetAsync($"{_client.BaseUrl}/proxies/{Name}/toxics", cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var toxicsData = JsonSerializer.Deserialize<ToxicConfiguration[]>(json, JsonOptions.Default);
            if (toxicsData is null)
            {
                throw new JsonException("Failed to deserialize toxics data.");
            }
            Toxics = [..toxicsData.Select(ToxicFactory.CreateToxic)];
        }

        private async Task<Toxic> CreateToxicAsync(Toxic toxic, CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(toxic.GetConfiguration(), JsonOptions.Default);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await ToxiproxyClient.HttpClient.PostAsync($"{_client.BaseUrl}/proxies/{Name}/toxics", content, cancellationToken);
                response.EnsureSuccessStatusCode();
                await GetActiveToxicsAsync(cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyException($"Failed to create toxic '{toxic.Name}' proxy '{Name}'", ex);
            }

            return ToxicFactory.CreateToxic(toxic.GetConfiguration());
        }

        private static bool IsListeningAddressValid(string address)
        {
            Regex listeningAddressRegex = new(@"^(?<host>[^:]+):(?<port>\d+)$", RegexOptions.Compiled);
            Regex looksLikeAnIPAddressRegex = new(@"^[\d.]+$", RegexOptions.Compiled);

            var match = listeningAddressRegex.Match(address);
            if (match.Success)
            {
                string host = match.Groups["host"].Value;
                string portStr = match.Groups["port"].Value;

                // Validate port range
                if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
                {
                    return false;
                }

                // Validate IP address
                if (looksLikeAnIPAddressRegex.IsMatch(host))
                {
                    if (!IPAddress.TryParse(host, out IPAddress addr))
                    {
                        return false;
                    }

                    // Reject things like "20.2" or "10.11.12"
                    return addr.ToString() == host;
                }

                // Parse hostname
                return Uri.CheckHostName(host) != UriHostNameType.Unknown;
            }

            return false;
        }

        /// <summary>
        /// Starting from version 2.6.0, Toxiproxy server supports HTTP PATCH method for proxy updates, 
        /// and started deprecating updates using HTTP POST. 
        /// HTTP POST for updates won't be allowed anymore starting from version 3.0.0.
        /// This property allows choosing the supported update HTTP method according to the detected version, 
        /// and we can support both ways without breaking.
        /// <see href="https://github.com/Shopify/toxiproxy/blob/main/CHANGELOG.md#260---2023-08-22">See Toxiproxy changelog.</see>
        /// </summary>
        /// <returns>Returns <see langword="true"/> if server version is 2.6.0 or above.</returns>
        /// <remarks>
        /// The version check is needed, because if you use HTTP PATCH method on servers below 2.6.0, you get a "405 Method Not Allowed" response.
        /// </remarks>
        private bool ServerRequiresHttpPatchForProxyUpdates => !(new Version(_client.ServerVersion).CompareTo(new Version("2.6.0")) < 0);
    }

    /// <summary>
    /// Represents the configuration settings for a proxy.
    /// Helper class for serialization/deserialization of proxy data.
    /// </summary>
    internal sealed record ProxyConfiguration
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("listen")]
        public string Listen { get; set; } = string.Empty;

        [JsonPropertyName("upstream")]
        public string Upstream { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("toxics")]
        public IReadOnlyCollection<ToxicConfiguration> Toxics { get; set; } = [];
    }
}

