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

            var ex = await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy proxy2 = await _sut.ConfigureProxy(cfg =>
                {
                    cfg.Name = testProxyName;
                    cfg.Listen = "127.0.0.1:11112";
                    cfg.Upstream = "example.org:82";
                }, TestContext.Current.CancellationToken);
            });

            Assert.Contains($"Proxy with name '{testProxyName}' already exists", ex.Message, StringComparison.CurrentCulture);
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
        public async Task Client_ShouldNotThrowException_WhenDeletingNonExistingProxy()
        {
            await _sut.DeleteProxyAsync("non_existing_proxy", TestContext.Current.CancellationToken);
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

        [Fact]
        public async Task Client_ShouldNotRetrieveProxies_WhenServerHasNoProxies()
        {
            var proxies = await _sut.GetProxiesAsync(TestContext.Current.CancellationToken);
            Assert.Empty(proxies);
        }

        [Fact]
        public async Task Client_Should_ThrowException_WhenProxyNameIsNotValid()
        {
            var ex = await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy proxy = await _sut.ConfigureProxy(cfg =>
                {
                    cfg.Listen = "127.0.0.1:1234";
                    cfg.Upstream = "myserver.com:443";
                }, TestContext.Current.CancellationToken);
            });

            Assert.Contains("Configured proxy must have a name", ex.Message, StringComparison.CurrentCulture);
        }

        [Theory]
        [InlineData("invalid_address")]
        [InlineData("10.10:8080")]
        [InlineData("10.11.12:80")]
        [InlineData("127.0.0.180")]
        [InlineData("127.0.0.1")]
        [InlineData(":8080")]
        [InlineData("256.0.0.1:80")]
        [InlineData("256.256.256.256:80")]
        public async Task Client_Should_ThrowException_WhenListeningAddressIsInvalid(string listeningAddress)
        {
            var ex = await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy proxy = await _sut.ConfigureProxy(cfg =>
                {
                    cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                    cfg.Listen = listeningAddress;
                    cfg.Upstream = "example.org:80";
                }, TestContext.Current.CancellationToken);
            });

            Assert.Contains("You must set a proxy listening address in the form <ip address>:<port>", ex.Message, StringComparison.CurrentCulture);
        }

        [Theory]
        [InlineData("invalid_address")]
        [InlineData("10.10:8080")]
        [InlineData("10.11.12:80")]
        [InlineData("127.0.0.180")]
        [InlineData("127.0.0.1")]
        [InlineData(":8080")]
        [InlineData("256.0.0.1:80")]
        [InlineData("256.256.256.256:80")]
        public async Task Client_Should_ThrowException_WhenUpstreamAddressIsInvalid(string upstreamAddress)
        {
            var ex = await Assert.ThrowsAsync<ProxyConfigurationException>(async () =>
            {
                Proxy proxy = await _sut.ConfigureProxy(cfg =>
                {
                    cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                    cfg.Listen = "127.0.0.1:1234";
                    cfg.Upstream = upstreamAddress;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Contains("You must set an upstream address to proxy for, in the form <ip address/hostname>:<port>", ex.Message, StringComparison.CurrentCulture);
        }

        [Fact]
        public async Task Client_ShouldResetServer()
        {
            Proxy proxy1 = await _sut.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example1.org:80";
            }, TestContext.Current.CancellationToken);

            Proxy proxy2 = await _sut.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:22222";
                cfg.Upstream = "example2.com:80";
            }, TestContext.Current.CancellationToken);

            await proxy1.AddLatencyToxicAsync(toxic =>
            {
                toxic.Latency = 1000;
                toxic.Jitter = 100;
            }, TestContext.Current.CancellationToken);

            await proxy2.DisableAsync(TestContext.Current.CancellationToken);

            await _sut.ResetAsync(TestContext.Current.CancellationToken);

            var proxies = await _sut.GetProxiesAsync(TestContext.Current.CancellationToken);
            Assert.True(proxies.All(p => p.Enabled));
            Assert.True(proxies.All(p => p.Toxics.Count == 0));
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _sut.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(proxy => _sut.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken)));
        }
    }
}
