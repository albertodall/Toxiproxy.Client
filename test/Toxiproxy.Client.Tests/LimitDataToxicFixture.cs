namespace Toxiproxy.Client.Tests
{
    public sealed class LimitDataToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public LimitDataToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task Should_AddLimitDataToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            LimitDataToxic sut = await proxy.AddLimitDataToxicAsync(cfg =>
            {
                cfg.Bytes = 256;
            }, TestContext.Current.CancellationToken);

            var createdToxic = await proxy.GetToxicAsync<LimitDataToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(createdToxic);
            Assert.Equal(sut.Name, createdToxic.Name);
            Assert.Equal(sut.Type, createdToxic.Type);
            Assert.Equal(sut.Bytes, createdToxic.Bytes);
        }

        [Fact]
        public async Task Should_UpdateBytes_ForLimitDataToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            LimitDataToxic sut = await proxy.AddLimitDataToxicAsync(cfg =>
            {
                cfg.Bytes = 512;
            }, TestContext.Current.CancellationToken);

            sut.Bytes = 1024;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedLimitDataToxic = await proxy.GetToxicAsync<LimitDataToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedLimitDataToxic);
            Assert.Equal(1024, updatedLimitDataToxic.Bytes);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingInvalidBytes_ForLimitDataToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                LimitDataToxic sut = await proxy.AddLimitDataToxicAsync(cfg =>
                {
                    cfg.Bytes = -256;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("Bytes", ex.PropertyName);
            Assert.Contains("Bytes must be a non-negative value", ex.Message, StringComparison.InvariantCulture);
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(p => _client.DeleteProxyAsync(p.Name, TestContext.Current.CancellationToken)));
        }
    }
}
