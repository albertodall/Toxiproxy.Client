namespace Toxiproxy.Client.Tests
{
    public sealed class ToxiproxyClientFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        // Version of the Toxiproxy server used in tests, See Docker compose file
        public const string Server_Version = "2.12.0";

        private readonly ToxiproxyClient _sut;

        public ToxiproxyClientFixture(ToxiproxyFixture fixture)
        {
            _sut = fixture.Client;
        }
        
        [Fact]
        public async Task Client_ShouldCreateNewProxy()
        {
            Proxy proxy = await _sut.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            Assert.NotNull(await _sut.GetProxyAsync(proxy.Name, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task Client_ShouldThrowException_WhenProxyAlreadyExists()
        {
            string testProxyName = $"test_proxy_{Guid.NewGuid()}";

            Proxy proxy = await _sut.ConfigureProxy(cfg =>
            {
                cfg.Name = testProxyName;
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<ToxiproxyException>(async () =>
            {
                Proxy proxy2 = await _sut.ConfigureProxy(cfg =>
                {
                    cfg.Name = testProxyName;
                    cfg.Listen = "127.0.0.1:11112";
                    cfg.Upstream = "example.org:82";
                }, TestContext.Current.CancellationToken);
            });
        }

        [Fact]
        public async Task Client_Should_RetrieveProxy_WhenProxyExists()
        {
            Proxy proxy = await _sut.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var retrievedProxy = await _sut.GetProxyAsync(proxy.Name, TestContext.Current.CancellationToken);
            Assert.Equivalent(proxy, retrievedProxy);
        }

        [Fact]
        public async Task Client_ShouldDeleteProxy_WhenProxyExists()
        {
            Proxy proxy = await _sut.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            await _sut.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken);

            var existingProxy = await _sut.GetProxyAsync(proxy.Name, TestContext.Current.CancellationToken);
            Assert.Null(existingProxy);
        }

        [Fact]
        public async Task Client_ShouldRetrieveAllProxies_WhenServerHasProxies()
        {
            Proxy proxy1 = await _sut.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            Proxy proxy2 = await _sut.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:22222";
                cfg.Upstream = "example.com:80";
            }, TestContext.Current.CancellationToken);

            var proxies = await _sut.GetProxiesAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, proxies.Count);
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _sut.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(proxy => _sut.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken)));
        }
    }
}
