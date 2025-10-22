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
                    (Version current, Version supported) when current < supported => throw new ToxiproxyConnectionException($"Toxiproxy server version is not supported. Minimum supported version is {MinimumSupportedVersion}."),
                    (Version current, _) => current.ToString()
                };
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException($"Unable to connect to Toxiproxy server at {baseUrl}.", ex);
            }

            return new ToxiproxyClient(baseUrl, version);
        }

        public string BaseUrl { get; private set; }
        public string ServerVersion { get; private set; }

        /// <summary>
        /// <see cref="https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines"/>
        /// </summary>
        internal static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public Task<Proxy> ConfigureProxy(Action<ProxyConfiguration> builder, CancellationToken cancellationToken = default)
        {
            var config = new ProxyConfiguration();
            builder(config);
            EnsureProxyConfigurationIsValid(config);
            return AddProxyAsync(config, cancellationToken);
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
                    throw new ToxiproxyException($"Proxy with name '{configuration.Name}' already exists");
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

        public async Task<Proxy[]> GetProxiesAsync(CancellationToken cancellationToken = default)
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

                return result.ToArray();
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException("Failed to retrieve proxies", ex);
            }
        }

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
                throw new ToxiproxyConnectionException($"Failed to get proxy '{name}'", ex);
            }
        }

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
                throw new ToxiproxyConnectionException($"Failed to delete proxy '{name}'", ex);
            }
        }

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

        private static void EnsureProxyConfigurationIsValid(ProxyConfiguration configuration)
        {
            Regex ipHostnamePortRegex = new("^(?:(?:\\d{1,3}\\.){3}\\d{1,3}|[a-zA-Z0-9.-]+):\\d{1,5}$");

            if (string.IsNullOrWhiteSpace(configuration.Name))
            {
                throw new ProxyConfigurationException(nameof(configuration.Name), "Proxy must have a name.");
            }

            if (string.IsNullOrWhiteSpace(configuration.Listen) || !ipHostnamePortRegex.IsMatch(configuration.Listen))
            {
                throw new ProxyConfigurationException(nameof(configuration.Listen), "You must set a listening address in the form [ip address]:[port].");
            }

            if (string.IsNullOrWhiteSpace(configuration.Upstream) || !ipHostnamePortRegex.IsMatch(configuration.Listen))
            {
                throw new ProxyConfigurationException(nameof(configuration.Upstream), "You must set an upstream address to proxy for, in the form [ip address/hostname]:[port].");
            }
        }
    }
}
