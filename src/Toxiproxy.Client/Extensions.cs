namespace Toxiproxy.Client
{
    internal static class Extensions
    {
        extension(Proxy proxy)
        {
            /// <summary>
            /// Returns a serializer-friendly configuration of a <see cref="Proxy"/>.
            /// </summary>
            public ProxyConfiguration GetConfiguration()
            {
                return new ProxyConfiguration()
                {
                    Name = proxy.Name,
                    Listen = proxy.Listen,
                    Upstream = proxy.Upstream,
                    Enabled = proxy.Enabled,
                    Toxics = [..proxy.Toxics.Select(t => t.GetConfiguration())]
                };
            }
        }

        extension(Toxic toxic)
        {
            /// <summary>
            /// Returns a serializer-friendly configuration of a <see cref="Toxic"/>.
            /// </summary>
            public ToxicConfiguration GetConfiguration()
            {
                return new ToxicConfiguration()
                {
                    Name = toxic.Name,
                    Type = toxic.Type,
                    Stream = toxic.Stream,
                    Toxicity = toxic.Toxicity,
                    Attributes = toxic.Attributes
                };
            }
        }

        private static readonly HttpMethod PatchHttpMethod = new("PATCH");

        extension(HttpClient httpClient)
        {
            public Task<HttpResponseMessage> PatchAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            {
                return httpClient.PatchAsync(new Uri(requestUri, UriKind.Absolute), content, cancellationToken);
            }

            private Task<HttpResponseMessage> PatchAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken = default)
            {
                HttpRequestMessage request = new(PatchHttpMethod, requestUri)
                {
                    Content = content
                };
                return httpClient.SendAsync(request, cancellationToken);
            }
        }
    }
}
