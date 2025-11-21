namespace Toxiproxy.Client.Tests
{
    public sealed class BandwidthToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public BandwidthToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task Should_AddBandwidthToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            BandwidthToxic sut = await proxy.AddBandwidthToxicAsync(cfg =>
            {
                cfg.Rate = 10;
            }, TestContext.Current.CancellationToken);

            var createdToxic = await proxy.GetToxicAsync<BandwidthToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(createdToxic);
            Assert.Equal(sut.Name, createdToxic.Name);
            Assert.Equal(sut.Type, createdToxic.Type);
            Assert.Equal(sut.Rate, createdToxic.Rate);
        }

        [Fact]
        public async Task Should_UpdateRate_ForBandwidthToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            BandwidthToxic sut = await proxy.AddBandwidthToxicAsync(cfg =>
            {
                cfg.Rate = 10;
            }, TestContext.Current.CancellationToken);

            sut.Rate = 42;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedBandwidthToxic = await proxy.GetToxicAsync<BandwidthToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedBandwidthToxic);
            Assert.Equal(42, updatedBandwidthToxic.Rate);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingInvalidRate_ForBandwidthToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                BandwidthToxic sut = await proxy.AddBandwidthToxicAsync(cfg =>
                {
                    cfg.Rate = -42;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("Rate", ex.PropertyName);
            Assert.Contains("Rate must be a non-negative value", ex.Message, StringComparison.InvariantCulture);
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(p => _client.DeleteProxyAsync(p, TestContext.Current.CancellationToken)));
        }
    }
}
