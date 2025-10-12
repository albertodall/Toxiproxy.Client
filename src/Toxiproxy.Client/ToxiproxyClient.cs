using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Toxiproxy.Client
{
    public sealed class ToxiproxyClient
    {
        private readonly Lazy<Task<ServerVersion>> _serverVersion;

        public ToxiproxyClient(string hostName = "localhost", int port = 8474)
        {
            BaseUrl = $"http://{hostName}:{port}";
            _serverVersion = new Lazy<Task<ServerVersion>>(GetServerVersionAsync());
        }

        public string BaseUrl { get; private set; }

        /// <summary>
        /// <see cref="https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines"/>
        /// </summary>
        internal static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public async Task<Proxy> CreateProxyAsync(string name, string listen, string upstream, CancellationToken cancellationToken = default)
        {
            try
            {
                var newProxy = new ProxyConfiguration
                {
                    Name = name,
                    Listen = listen,
                    Upstream = upstream,
                    Enabled = true
                };


                var json = JsonSerializer.Serialize(newProxy, JsonOptions.Default);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await HttpClient.PostAsync($"{BaseUrl}/proxies", content, cancellationToken);

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new ToxiproxyException($"Proxy with name '{name}' already exists");
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
                throw new ToxiproxyConnectionException($"Failed to create proxy '{name}'", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new ToxiproxyConnectionException($"Request timeout while creating proxy '{name}'", ex);
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

        /// <summary>
        /// Get the Toxiproxy server version.
        /// </summary>
        /// <returns>The Toxiproxy server version.</returns>
        public async Task<string> GetVersionAsync()
        {
            return (await _serverVersion.Value).Version;
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
                throw new ToxiproxyConnectionException("Failed to reset Toxiproxy", ex);
            }
        }

        private async Task<ServerVersion> GetServerVersionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await HttpClient.GetAsync($"{BaseUrl}/version", cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var serverVersion = JsonSerializer.Deserialize<ServerVersion>(json, JsonOptions.Default);

                return serverVersion is null
                    ? throw new JsonException("Failed to deserialize server version data.")
                    : serverVersion;
            }
            catch (HttpRequestException ex)
            {
                throw new ToxiproxyConnectionException("Failed to get Toxiproxy version", ex);
            }
        }
    }

    public sealed record ServerVersion
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }
}
