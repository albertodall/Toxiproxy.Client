
namespace Toxiproxy.Client.Tests
{
    public sealed class SlowCloseToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public SlowCloseToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task Should_AddSlowCloseToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            SlowCloseToxic sut = await proxy.AddSlowCloseToxicAsync(cfg =>
            {
                cfg.Delay = 42;
            }, TestContext.Current.CancellationToken);

            var createdToxic = await proxy.GetToxicAsync<SlowCloseToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(createdToxic);
            Assert.Equal(sut.Name, createdToxic.Name);
            Assert.Equal(sut.Type, createdToxic.Type);
            Assert.Equal(sut.Delay, createdToxic.Delay);
        }

        [Fact]
        public async Task Should_UpdateDelay_ForSlowCloseToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            SlowCloseToxic sut = await proxy.AddSlowCloseToxicAsync(cfg =>
            {
                cfg.Delay = 100;
            }, TestContext.Current.CancellationToken);

            sut.Delay = 42;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedSlowCloseToxic = await proxy.GetToxicAsync<SlowCloseToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedSlowCloseToxic);
            Assert.Equal(42, updatedSlowCloseToxic.Delay);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingInvalidJitter_ForLatencyToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                SlowCloseToxic sut = await proxy.AddSlowCloseToxicAsync(cfg =>
                {
                    cfg.Delay = -42;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("Delay", ex.PropertyName);
            Assert.Contains("Delay must be a non-negative value", ex.Message, StringComparison.InvariantCulture);
        }

        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(p => _client.DeleteProxyAsync(p.Name, TestContext.Current.CancellationToken)));
        }
    }
}
