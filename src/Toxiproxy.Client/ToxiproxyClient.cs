using System.Net;
using System.Text;
using System.Text.Json;

namespace Toxiproxy.Client
{
    /// <summary>
    /// Client that interacts with a Toxiproxy server.
    /// </summary>
    /// <remarks>
    /// Supports Toxiproxy servers from version 2.0.0 onwards.
    /// </remarks>
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
        /// <returns>The <see cref="ToxiproxyClient"/> instance that interacts with the connected server.</returns>
        /// <exception cref="JsonException">Thrown if there are issues in detecting the server version.</exception>
        /// <exception cref="ToxiproxyException">Thrown if the connection to the Toxiproxy server fails or if the server version is not supported.</exception>
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
                        when current < supported => throw new ToxiproxyException($"Toxiproxy server version is not supported. Minimum supported version is {MinimumSupportedVersion}."),
                    (Version current, _) => current.ToString()
                };
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyException($"Unable to connect to Toxiproxy server at {baseUrl}.", ex);
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
        /// <param name="builder">Proxy configuration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The configured proxy.</returns>
        public async Task<Proxy> ConfigureProxyAsync(Action<Proxy> builder, CancellationToken cancellationToken = default)
        {
            var newProxy = new Proxy(this);
            builder(newProxy);
            newProxy.EnsureConfigurationIsValid();

            try
            {
                var json = JsonSerializer.Serialize(newProxy.GetConfiguration(), JsonOptions.Default);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync($"{BaseUrl}/proxies", content, cancellationToken);
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new ToxiproxyException($"Proxy with name '{newProxy.Name}' already exists");
                }
                response.EnsureSuccessStatusCode();

                return newProxy;
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyException($"Failed to create proxy '{newProxy.Name}'", ex);
            }
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

                return proxies.Select(kvp => new Proxy(this, kvp.Value)).ToArray();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyException($"Failed to retrieve proxies from server {BaseUrl}.", ex);
            }
        }

        /// <summary>
        /// Retrieves a proxy from the server.
        /// </summary>
        /// <param name="name">The name of the proxy to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Proxy"/> object representing the proxy configuration if found, or <see langword="null"/> if the proxy does not exist.</returns>
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

                var json = await response.Content.ReadAsStringAsync();
                var proxyConfiguration = JsonSerializer.Deserialize<ProxyConfiguration>(json, JsonOptions.Default);

                return proxyConfiguration is null 
                    ? throw new JsonException("Failed to deserialize proxy data.") 
                    : new Proxy(this, proxyConfiguration);
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyException($"Failed to retrieve proxy '{name}' from server {BaseUrl}", ex);
            }
        }

        /// <summary>
        /// Deletes the specified proxy from the server.
        /// </summary>
        /// <param name="name">The name of the proxy to delete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public Task DeleteProxyAsync(string name, CancellationToken cancellationToken = default)
        {
            return HttpClient.DeleteAsync($"{BaseUrl}/proxies/{name}", cancellationToken);
        }

        /// <summary>
        /// Resets the Toxiproxy server calling the designated endpoint.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ToxiproxyConnectionException">Thrown if the connection to the Toxiproxy server fails.</exception>
        /// <remarks>
        /// "Reset" means enable all active proxies and remove all active toxics.
        /// </remarks>
        public async Task ResetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await HttpClient.PostAsync($"{BaseUrl}/reset", null, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyException($"Failed to reset Toxiproxy server at {BaseUrl}", ex);
            }
        }
    }
}
