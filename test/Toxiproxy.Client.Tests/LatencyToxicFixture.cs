namespace Toxiproxy.Client.Tests
{
    public sealed class LatencyToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public LatencyToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }
        
        [Fact]
        public async Task Should_AddLatencyToxic()
        {
            Proxy proxy = await _client.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            LatencyToxic sut = await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, cfg => 
            { 
                cfg.Toxicity = 1.0f;
                cfg.Latency = 1000;
                cfg.Jitter = 10;
            }, TestContext.Current.CancellationToken);

            var createdToxic = await proxy.GetToxicAsync<LatencyToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(createdToxic);
            Assert.Equal(sut.Name, createdToxic.Name);
            Assert.Equal(sut.Type, createdToxic.Type);
            Assert.Equal(sut.Latency, createdToxic.Latency);
            Assert.Equal(sut.Jitter, createdToxic.Jitter);
        }
        
        [Fact]
        public async Task Should_UpdateLatency_ForLatencyToxic()
        {
            Proxy proxy = await _client.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            LatencyToxic sut = await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, cfg =>
            {
                cfg.Toxicity = 1.0f;
                cfg.Latency = 1000;
                cfg.Jitter = 10;
            }, TestContext.Current.CancellationToken);

            sut.Latency = 5000;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedLatencyToxic = await proxy.GetToxicAsync<LatencyToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedLatencyToxic);
            Assert.Equal(5000, updatedLatencyToxic.Latency);
        }

        [Fact]
        public async Task Should_UpdateJitter_ForLatencyToxic()
        {
            Proxy proxy = await _client.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            LatencyToxic sut = await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, cfg =>
            {
                cfg.Toxicity = 1.0f;
                cfg.Latency = 1000;
                cfg.Jitter = 10;
            }, TestContext.Current.CancellationToken);

            sut.Jitter = 20;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedLatencyToxic = await proxy.GetToxicAsync<LatencyToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedLatencyToxic);
            Assert.Equal(20, updatedLatencyToxic.Jitter);
        }

        [Fact]
        public async Task Should_UpdateToxicity_ForLatencyToxic()
        {
            Proxy proxy = await _client.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            LatencyToxic sut = await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, cfg =>
            {
                cfg.Toxicity = 1.0f;
                cfg.Latency = 1000;
                cfg.Jitter = 10;
            }, TestContext.Current.CancellationToken);

            sut.Toxicity = 0.1f;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedLatencyToxic = await proxy.GetToxicAsync<LatencyToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedLatencyToxic);
            Assert.Equal(0.1f, updatedLatencyToxic.Toxicity);
        }

        [Fact]
        public async Task Should_Throw_WhenSettingInvalidLatency_ForLatencyToxic()
        {
            Proxy proxy = await _client.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                LatencyToxic sut = await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, cfg =>
                {
                    cfg.Toxicity = 1.0f;
                    cfg.Latency = -42;
                    cfg.Jitter = 10;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("Latency", ex.PropertyName);
            Assert.Contains("Latency must be a non-negative value", ex.Message, StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task Should_Throw_WhenSettingInvalidJitter_ForLatencyToxic()
        {
            Proxy proxy = await _client.ConfigureProxy(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                LatencyToxic sut = await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, cfg =>
                {
                    cfg.Toxicity = 1.0f;
                    cfg.Latency = 100;
                    cfg.Jitter = -10;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("Jitter", ex.PropertyName);
            Assert.Contains("Jitter must be a non-negative value", ex.Message, StringComparison.InvariantCulture);
        }


        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(p => _client.DeleteProxyAsync(p.Name, TestContext.Current.CancellationToken)));
        }
    }
}
