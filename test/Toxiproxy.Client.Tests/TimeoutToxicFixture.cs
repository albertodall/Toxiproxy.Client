namespace Toxiproxy.Client.Tests
{
    public sealed class TimeoutToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public TimeoutToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task Should_AddTimeoutToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            TimeoutToxic sut = await proxy.AddTimeoutToxicAsync(cfg =>
            {
                cfg.Timeout = 42;
            }, TestContext.Current.CancellationToken);

            var createdToxic = await proxy.GetToxicAsync<TimeoutToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(createdToxic);
            Assert.Equal(sut.Name, createdToxic.Name);
            Assert.Equal(sut.Type, createdToxic.Type);
            Assert.Equal(sut.Timeout, createdToxic.Timeout);
        }

        [Fact]
        public async Task Should_UpdateTimeout_ForTimeoutToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            TimeoutToxic sut = await proxy.AddTimeoutToxicAsync(cfg =>
            {
                cfg.Timeout = 10;
            }, TestContext.Current.CancellationToken);

            sut.Timeout = 42;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedTimeoutToxic = await proxy.GetToxicAsync<TimeoutToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedTimeoutToxic);
            Assert.Equal(42, updatedTimeoutToxic.Timeout);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingInvalidTimeout_ForTimeoutToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                TimeoutToxic sut = await proxy.AddTimeoutToxicAsync(cfg =>
                {
                    cfg.Timeout = -42;
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
