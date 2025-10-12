namespace Toxiproxy.Client.Tests
{
    public sealed class ToxiproxyClientFixture : IClassFixture<ToxiproxyTestFixture>
    {
        // Version of the Toxiproxy server used in tests, See Docker compose file
        public const string Server_Version = "2.12.0";

        private readonly ToxiproxyClient _sut;

        public ToxiproxyClientFixture(ToxiproxyTestFixture fixture)
        {
            _sut = fixture.Client;
        }

        [Fact]
        public async Task Client_ShouldCreateNewProxy()
        {
            var proxy = await _sut.CreateProxyAsync($"test_proxy_{Guid.NewGuid()}", "localhost:11111", "example.com:80", TestContext.Current.CancellationToken);
            Assert.NotNull(proxy);

            // Cleanup
            await _sut.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Client_ShouldThrowException_WhenProxyAlreadyExists()
        {
            string testProxyName = $"test_proxy_{Guid.NewGuid()}";

            var proxy = await _sut.CreateProxyAsync(testProxyName, "localhost:11111", "example.com:80", TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<ToxiproxyException>(async () =>
            {
                await _sut.CreateProxyAsync(testProxyName, "localhost:22222", "example.com:80", TestContext.Current.CancellationToken);
            });

            // Cleanup
            await _sut.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Client_Should_RetrieveProxy_WhenProxyExists()
        {
            var proxy = await _sut.CreateProxyAsync($"test_proxy_{Guid.NewGuid()}", "localhost:11111", "example.com:80", TestContext.Current.CancellationToken);

            var retrievedProxy = await _sut.GetProxyAsync(proxy.Name, TestContext.Current.CancellationToken);
            Assert.Equivalent(proxy, retrievedProxy);

            // Cleanup
            await _sut.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Client_ShouldDeleteProxy_WhenProxyExists()
        {
            var proxy = await _sut.CreateProxyAsync($"test_proxy_{Guid.NewGuid()}", "localhost:11111", "example.com:80", TestContext.Current.CancellationToken);

            await _sut.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken);
            var existingProxy = await _sut.GetProxyAsync(proxy.Name, TestContext.Current.CancellationToken);
            Assert.Null(existingProxy);
        }

        [Fact]
        public async Task Client_ShouldRetrieveAllProxies_WhenServerHasProxies()
        {
            await _sut.CreateProxyAsync($"test_proxy_{Guid.NewGuid()}", "localhost:11111", "example1.com:80", TestContext.Current.CancellationToken);
            await _sut.CreateProxyAsync($"test_proxy_{Guid.NewGuid()}", "localhost:22222", "example2.com:80", TestContext.Current.CancellationToken);

            var proxies = await _sut.GetProxiesAsync(TestContext.Current.CancellationToken);
            Assert.Equal(2, proxies.Length);

            // Cleanup
            await Task.WhenAll(proxies.Select(proxy => _sut.DeleteProxyAsync(proxy.Name, TestContext.Current.CancellationToken)));
        }

        [Fact]
        public async Task Client_ShouldReadServerVersion()
        {
            var version = await _sut.GetVersionAsync();
            Assert.Equal(Server_Version, version);
        }
    }
}
