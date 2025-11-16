namespace Toxiproxy.Client.Tests
{
    public sealed class ResetPeerToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public ResetPeerToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task Should_AddResetPeerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            ResetPeerToxic sut = await proxy.AddResetPeerToxicAsync(cfg =>
            {
                cfg.Timeout = 42;
            }, TestContext.Current.CancellationToken);

            var createdToxic = await proxy.GetToxicAsync<ResetPeerToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(createdToxic);
            Assert.Equal(sut.Name, createdToxic.Name);
            Assert.Equal(sut.Type, createdToxic.Type);
            Assert.Equal(sut.Timeout, createdToxic.Timeout);
        }

        [Fact]
        public async Task Should_UpdateTimeout_ForResetPeerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            ResetPeerToxic sut = await proxy.AddResetPeerToxicAsync(cfg =>
            {
                cfg.Timeout = 100;
            }, TestContext.Current.CancellationToken);

            sut.Timeout = 200;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedResetPeerToxic = await proxy.GetToxicAsync<ResetPeerToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedResetPeerToxic);
            Assert.Equal(200, updatedResetPeerToxic.Timeout);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingInvalidTimeout_ForResetPeerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                ResetPeerToxic sut = await proxy.AddResetPeerToxicAsync(cfg =>
                {
                    cfg.Timeout = -100;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("Timeout", ex.PropertyName);
            Assert.Contains("Timeout must be a non-negative value", ex.Message, StringComparison.InvariantCulture);
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(p => _client.DeleteProxyAsync(p.Name, TestContext.Current.CancellationToken)));
        }
    }
}
