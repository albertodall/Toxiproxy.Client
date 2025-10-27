using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Toxiproxy.Client
{
    public sealed class ToxiproxyClient
    {
        private const string MinimumSupportedVersion = "2.0.0";

        private ToxiproxyClient(string baseUrl, string serverVersion)
        {
            BaseUrl = baseUrl;
            ServerVersion = serverVersion;
        }

        /// <summary>
        /// Connects to a Toxiproxy server at the specified address and port.
        /// </summary>
        /// <param name="hostName">The hostname of the Toxiproxy server. Defaults to "localhost".</param>
        /// <param name="port">The port number of the Toxiproxy server. Defaults to 8474.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="ToxiproxyClient"/> instance to interact with the connected server.</returns>
        /// <exception cref="JsonException">Thrown if there are issues in detecting the server version.</exception>
        /// <exception cref="ToxiproxyConnectionException">Thrown if the connection to the Toxiproxy server fails or if the server version is not supported.</exception>
        public static async Task<ToxiproxyClient> ConnectAsync(string hostName = "localhost", int port = 8474, CancellationToken cancellationToken = default)
        {
            string baseUrl = $"http://{hostName}:{port}";
            string version;

            try
            {
                var response = await HttpClient.GetAsync($"{baseUrl}/version", cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var serverVersion = JsonSerializer.Deserialize<Version>(json, JsonOptions.Default);

                version = (serverVersion, Version.Parse(MinimumSupportedVersion)) switch
                {
                    (null, _) => throw new JsonException("Failed to deserialize server version data."),
                    (Version current, Version supported) 
                        when current < supported => throw new ToxiproxyConnectionException($"Toxiproxy server version is not supported. Minimum supported version is {MinimumSupportedVersion}."),
                    (Version current, _) => current.ToString()
                };
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Unable to connect to Toxiproxy server at {baseUrl}.", ex);
            }

            return new ToxiproxyClient(baseUrl, version);
        }

        /// <summary>
        /// URL of the Toxiproxy server we are connected to.
        /// </summary>
        public string BaseUrl { get; private set; }

        /// <summary>
        /// Version of the Toxiproxy server we are connected to.
        /// </summary>
        public string ServerVersion { get; private set; }

        /// <summary>
        /// HttpClient instance used for all communications with the Toxiproxy server.
        /// </summary>
        /// <remarks>
        /// <see cref="https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines"/>
        /// </remarks>
        internal static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// Configures a new proxy on the Toxiproxy server.
        /// </summary>
        /// <returns>The proxy configured.</returns>
        public Task<Proxy> ConfigureProxy(Action<ProxyConfiguration> builder, CancellationToken cancellationToken = default)
        {
            var config = new ProxyConfiguration();
            builder(config);
            EnsureProxyConfigurationIsValid(config);
            return AddProxyAsync(config, cancellationToken);
        }

        /// <summary>
        /// Retrieves the list of proxies configured on the server.
        /// </summary>
        /// <returns>An list of <see cref="Proxy"/> objects representing the proxies retrieved from the server.</returns>
        /// <exception cref="JsonException">Thrown if there are response deserialization issues.</exception>
        /// <exception cref="ToxiproxyConnectionException">Thrown if there is an error while connecting to the server.</exception>
        public async Task<IReadOnlyList<Proxy>> GetProxiesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await HttpClient.GetAsync($"{BaseUrl}/proxies", cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var proxies = JsonSerializer.Deserialize<Dictionary<string, ProxyConfiguration>>(json, JsonOptions.Default);
                if (proxies is null)
                {
                    throw new JsonException("Failed to deserialize proxies data.");
                }

                var result = new List<Proxy>();
                foreach (var kvp in proxies)
                {
                    result.Add(new Proxy(this, kvp.Value));
                }

                return result.AsReadOnly();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Failed to retrieve proxies from server {BaseUrl}.", ex);
            }
        }

        /// <summary>
        /// Retrieves a proxy by its name from the server.
        /// </summary>
        /// <param name="name">The name of the proxy to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Proxy"/> object representing the proxy configuration if found; otherwise, <see langword="null"/> if the proxy does not exist.</returns>
        /// <exception cref="JsonException">Thrown if there are response deserialization issues.</exception>
        /// <exception cref="ToxiproxyConnectionException">Thrown if there is an error while connecting to the server.</exception>
        public async Task<Proxy?> GetProxyAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await HttpClient.GetAsync($"{BaseUrl}/proxies/{name}", cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var proxyConfiguration = JsonSerializer.Deserialize<ProxyConfiguration>(json, JsonOptions.Default);

                return proxyConfiguration is null 
                    ? throw new JsonException("Failed to deserialize proxy data.") 
                    : new Proxy(this, proxyConfiguration);
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Failed to retrieve proxy '{name}' from server {BaseUrl}", ex);
            }
        }

        /// <summary>
        /// Deletes the specified proxy from the server.
        /// </summary>
        /// <param name="name">The name of the proxy to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ProxyNotFoundException">Thrown if the specified proxy does not exist.</exception>
        /// <exception cref="ToxiproxyConnectionException">Thrown if there is a failure in connecting to the server or deleting the proxy.</exception>
        public async Task DeleteProxyAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"{BaseUrl}/proxies/{name}", cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ProxyNotFoundException(name);
                }

                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Failed to delete proxy '{name}' from server {BaseUrl}", ex);
            }
        }

        /// <summary>
        /// Resets the Toxiproxy server calling the designated endpoint.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ToxiproxyConnectionException">Thrown if the connection to the Toxiproxy server fails.</exception>
        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await HttpClient.PostAsync($"{BaseUrl}/reset", null, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Failed to reset Toxiproxy server at {BaseUrl}", ex);
            }
        }

        private async Task<Proxy> AddProxyAsync(ProxyConfiguration configuration, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(configuration, JsonOptions.Default);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync($"{BaseUrl}/proxies", content, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new ProxyConfigurationException(nameof(configuration.Name), $"Proxy with name '{configuration.Name}' already exists");
                }

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                var newProxyConfiguration = JsonSerializer.Deserialize<ProxyConfiguration>(result, JsonOptions.Default);

                return newProxyConfiguration is null
                    ? throw new JsonException("Failed to deserialize the created proxy data.")
                    : new Proxy(this, newProxyConfiguration);
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Failed to create proxy '{configuration.Name}'", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new ToxiproxyConnectionException($"Request timeout while creating proxy '{configuration.Name}'", ex);
            }
        }

        private static void EnsureProxyConfigurationIsValid(ProxyConfiguration configuration)
        {
            Regex ipHostnamePortRegex = new("^(?:(?:\\d{1,3}\\.){3}\\d{1,3}|[a-zA-Z0-9.-]+):\\d{1,5}$");

            if (string.IsNullOrWhiteSpace(configuration.Name))
            {
                throw new ProxyConfigurationException(nameof(configuration.Name), "Configured proxy must have a name.");
            }

            if (string.IsNullOrWhiteSpace(configuration.Listen) || !IsListeningAddressValid(configuration.Listen))
            {
                throw new ProxyConfigurationException(nameof(configuration.Listen), "You must set a listening address in the form <ip address>:<port>.");
            }

            if (string.IsNullOrWhiteSpace(configuration.Upstream) || !IsListeningAddressValid(configuration.Upstream))
            {
                throw new ProxyConfigurationException(nameof(configuration.Upstream), "You must set an upstream address to proxy for, in the form <ip address/hostname>:<port>.");
            }
        }

        private static bool IsListeningAddressValid(string address)
        {
            Regex listeningAddressRegex = new(@"^(?<host>[^:]+):(?<port>\d+)$", RegexOptions.Compiled);
            Regex looksLikeAnIPAddressRegex = new (@"^[\d.]+$", RegexOptions.Compiled);

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
    }
}
