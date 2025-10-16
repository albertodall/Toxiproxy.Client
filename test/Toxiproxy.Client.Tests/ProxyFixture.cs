namespace Toxiproxy.Client.Tests
{
    public sealed class ProxyFixture : IClassFixture<ToxiproxyTestFixture>
    {
        private ToxiproxyClient _client;

        public ProxyFixture(ToxiproxyTestFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task ShouldUpdateUpstream_WhenProxyExists()
        {
            var sut = await _client.CreateProxyAsync($"test_proxy_{Guid.NewGuid()}", "localhost:11111", "example.com:80", TestContext.Current.CancellationToken);
            await sut.UpdateUpstreamAsync("example.org:80", TestContext.Current.CancellationToken);
            var updatedProxy = await _client.GetProxyAsync(sut.Name, TestContext.Current.CancellationToken);
            Assert.Equal("example.org:80", updatedProxy?.Upstream);

            // Cleanup
            await _client.DeleteProxyAsync(sut.Name, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ShouldDisableAndEnableProxy_WhenProxyExists()
        {
            var sut = await _client.CreateProxyAsync($"test_proxy_{Guid.NewGuid()}", "localhost:11111", "example.com:80", TestContext.Current.CancellationToken);
            await sut.DisableAsync(TestContext.Current.CancellationToken);
            var disabledProxy = await _client.GetProxyAsync(sut.Name, TestContext.Current.CancellationToken);
            Assert.False(disabledProxy?.Enabled);
            
            await sut.EnableAsync(TestContext.Current.CancellationToken);
            var enabledProxy = await _client.GetProxyAsync(sut.Name, TestContext.Current.CancellationToken);
            Assert.True(enabledProxy?.Enabled);

            // Cleanup
            await _client.DeleteProxyAsync(sut.Name, TestContext.Current.CancellationToken);
        }
    }
}
