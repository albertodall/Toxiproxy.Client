namespace Toxiproxy.Client.Tests
{
    public sealed class LatencyToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private ToxiproxyClient _client;

        public LatencyToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }
        /*
        [Fact]
        public async Task Should_AddLatencyToxic()
        {
            Proxy proxy = _client
                .ConfigureProxy()
                    .WithName($"test_proxy_{Guid.NewGuid()}")
                    .WithListeningAddress("localhost:11111")
                    .WithUpstream("example.org:80")
                    .Build();
            await _client.AddProxyAsync(proxy, TestContext.Current.CancellationToken);

            await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, 1.0f, 1000, 10, TestContext.Current.CancellationToken);

            var sut = await proxy.GetToxicAsync<LatencyToxic>("latency_downstream", TestContext.Current.CancellationToken);
            Assert.NotNull(sut);
            Assert.Equal("latency_downstream", sut.Name);
            Assert.Equal("latency", sut.Type);
            Assert.Equal(1000, sut.Latency);

            // Cleanup
            await proxy.RemoveAllToxicsAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Should_UpdateLatency_ForLatencyToxic()
        {
            Proxy proxy = _client
                 .ConfigureProxy()
                     .WithName($"test_proxy_{Guid.NewGuid()}")
                     .WithListeningAddress("localhost:11111")
                     .WithUpstream("example.org:80")
                     .Build();
            await _client.AddProxyAsync(proxy, TestContext.Current.CancellationToken);

            var sut = await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, 1.0f, 1000, 10, TestContext.Current.CancellationToken);
            sut.Latency = 5000;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedLatencyToxic = await proxy.GetToxicAsync<LatencyToxic>("latency_downstream", TestContext.Current.CancellationToken);
            Assert.NotNull(updatedLatencyToxic);
            Assert.Equal(5000, updatedLatencyToxic.Latency);

            // Cleanup
            await proxy.RemoveAllToxicsAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task Should_UpdateJitter_ForLatencyToxic()
        {
            Proxy proxy = _client
                 .ConfigureProxy()
                     .WithName($"test_proxy_{Guid.NewGuid()}")
                     .WithListeningAddress("localhost:11111")
                     .WithUpstream("example.org:80")
                     .Build();
            await _client.AddProxyAsync(proxy, TestContext.Current.CancellationToken);

            var sut = await proxy.AddLatencyToxicAsync("latency_downstream", ToxicDirection.Downstream, 1.0f, 1000, 10, TestContext.Current.CancellationToken);
            sut.Jitter = 20;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedLatencyToxic = await proxy.GetToxicAsync<LatencyToxic>("latency_downstream", TestContext.Current.CancellationToken);
            Assert.NotNull(updatedLatencyToxic);
            Assert.Equal(20, updatedLatencyToxic.Jitter);

            // Cleanup
            await proxy.RemoveAllToxicsAsync(TestContext.Current.CancellationToken);
        }
        */
        public ValueTask InitializeAsync() => ValueTask.CompletedTask;

        public async ValueTask DisposeAsync()
        {
            var proxies = await _client.GetProxiesAsync(TestContext.Current.CancellationToken);
            await Task.WhenAll(proxies.Select(p => _client.DeleteProxyAsync(p.Name, TestContext.Current.CancellationToken)));
        }
    }
}
