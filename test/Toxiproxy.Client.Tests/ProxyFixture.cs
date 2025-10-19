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
        public async Task Proxy_ShouldUpdateUpstream_WhenProxyExists()
        {
            string newUpstream = "example.com:80";

            Proxy sut = await _client.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            await sut.SetUpstreamAsync(newUpstream, TestContext.Current.CancellationToken);

            var updatedProxy = await _client.GetProxyAsync(sut.Name, TestContext.Current.CancellationToken);
            Assert.Equal(newUpstream, updatedProxy?.Upstream);
        }

        [Fact]
        public async Task Proxy_ShouldUpdateListeningAddress_WhenProxyExists()
        {
            string newListeningAddress = "127.0.0.1:22222";

            Proxy sut = await _client.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            await sut.SetListeningAddressAsync(newListeningAddress, TestContext.Current.CancellationToken);

            var updatedProxy = await _client.GetProxyAsync(sut.Name, TestContext.Current.CancellationToken);
            Assert.Equal(newListeningAddress, updatedProxy?.Listen);
        }

        [Fact]
        public async Task Proxy_ShouldDisableAndEnableProxy_WhenProxyExists()
        {
            Proxy sut = await _client.ConfigureProxy(cfg =>
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
        public async Task Proxy_ShouldThrowException_WhenNameIsNotValid()
        {
            await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy sut = await _client.ConfigureProxy(cfg =>
                {
                    cfg.Listen = "127.0.0.1:11111";
                    cfg.Upstream = "example.org:80";
                }, TestContext.Current.CancellationToken);
            });
        }

        [Fact]
        public async Task Proxy_ShouldThrowException_WhenListeningAddressIsNotValid()
        {
            await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy sut = await _client.ConfigureProxy(cfg =>
                {
                    cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                    cfg.Upstream = "example.org:80";
                }, TestContext.Current.CancellationToken);
            });
        }

        [Fact]
        public async Task Proxy_ShouldThrowException_WhenUpstreamIsNotValid()
        {
            await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy sut = await _client.ConfigureProxy(cfg =>
                {
                    cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                    cfg.Listen = "127.0.0.1:12345";
                }, TestContext.Current.CancellationToken);
            });
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(proxy => _client.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken)));
        }
    }
}
