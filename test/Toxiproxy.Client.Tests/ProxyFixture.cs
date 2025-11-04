namespace Toxiproxy.Client.Tests
{
    public sealed class ProxyFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public ProxyFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task EnableDisableProxy_ShouldWork_WhenProxyExists()
        {
            Proxy sut = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            await sut.DisableAsync(TestContext.Current.CancellationToken);
            var disabledProxy = await _client.GetProxyAsync(sut.Name, TestContext.Current.CancellationToken);
            Assert.False(disabledProxy?.Enabled);
            
            await sut.EnableAsync(TestContext.Current.CancellationToken);
            var enabledProxy = await _client.GetProxyAsync(sut.Name, TestContext.Current.CancellationToken);
            Assert.True(enabledProxy?.Enabled);
        }

        [Fact]
        public async Task ConfigureProxy_ShouldThrowException_WhenNameIsEmpty()
        {
            await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy sut = await _client.ConfigureProxyAsync(cfg =>
                {
                    cfg.Listen = "127.0.0.1:11111";
                    cfg.Upstream = "example.org:80";
                }, TestContext.Current.CancellationToken);
            });
        }

        [Fact]
        public async Task ConfigureProxy_ShouldThrowException_WhenListeningAddressIsNotValid()
        {
            await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy sut = await _client.ConfigureProxyAsync(cfg =>
                {
                    cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                    cfg.Upstream = "example.org:80";
                }, TestContext.Current.CancellationToken);
            });
        }

        [Fact]
        public async Task ConfigureProxy_ShouldThrowException_WhenUpstreamIsNotValid()
        {
            await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy sut = await _client.ConfigureProxyAsync(cfg =>
                {
                    cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                    cfg.Listen = "127.0.0.1:12345";
                }, TestContext.Current.CancellationToken);
            });
        }

        [Fact]
        public async Task DeleteProxy_ShouldDeleteProxy_WhenProxyExists()
        {
            Proxy sut = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:12345";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            await _client.DeleteProxyAsync(sut.Name, TestContext.Current.CancellationToken);

            var deletedProxy = await _client.GetProxyAsync(sut.Name, TestContext.Current.CancellationToken);
            Assert.Null(deletedProxy);
        }

        [Fact]
        public async Task DeleteProxy_ShouldNotThrowException_WhenProxyDoesNotExists()
        {
            await _client.DeleteProxyAsync("non_existing_proxy", TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task AddToxic_ShouldUpdateProxyToxicList()
        {
            Proxy sut = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:12345";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            await sut.AddLatencyToxicAsync(toxic => toxic.Latency = 1000, TestContext.Current.CancellationToken);

            Assert.Single(sut.Toxics);
        }

        [Fact]
        public async Task RemoveToxic_ShouldUpdateProxyToxicList()
        {
            Proxy sut = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:12345";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var toxic = await sut.AddLatencyToxicAsync(toxic => toxic.Jitter = 20, TestContext.Current.CancellationToken);

            await sut.RemoveToxicAsync(toxic.Name, TestContext.Current.CancellationToken);

            Assert.Empty(sut.Toxics);
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(proxy => _client.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken)));
        }
    }
}
