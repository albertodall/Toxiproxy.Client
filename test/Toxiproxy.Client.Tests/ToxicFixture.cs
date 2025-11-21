namespace Toxiproxy.Client.Tests
{
    public sealed class ToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public ToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task Proxy_ShouldRemoveToxic_WhenToxicExists()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var latencyToxic = await proxy.AddLatencyToxicAsync(toxic =>
            {
                toxic.Latency = 1000;
                toxic.Jitter = 100;
            }, TestContext.Current.CancellationToken);

            await proxy.RemoveToxicAsync(latencyToxic, TestContext.Current.CancellationToken);

            var retrievedToxic = await proxy.GetToxicAsync<LatencyToxic>(latencyToxic.Name, TestContext.Current.CancellationToken);
            Assert.Null(retrievedToxic);
        }

        [Fact]
        public async Task Proxy_ShouldCreateToxic_WithDefaultValues()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            LatencyToxic latencyToxic = await proxy.AddLatencyToxicAsync(toxic =>
            {
                toxic.Latency = 1000;
                toxic.Jitter = 100;
            }, TestContext.Current.CancellationToken);

            Assert.Equal($"latency_downstream", latencyToxic.Name);
            Assert.Equal($"downstream", latencyToxic.Stream);
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(p => _client.DeleteProxyAsync(p, TestContext.Current.CancellationToken)));
        }
    }
}
