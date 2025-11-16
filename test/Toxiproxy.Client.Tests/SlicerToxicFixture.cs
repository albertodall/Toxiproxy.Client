namespace Toxiproxy.Client.Tests
{
    public sealed class SlicerToxicFixture : IClassFixture<ToxiproxyFixture>, IAsyncLifetime
    {
        private readonly ToxiproxyClient _client;

        public SlicerToxicFixture(ToxiproxyFixture fixture)
        {
            _client = fixture.Client;
        }

        [Fact]
        public async Task Should_AddSlicerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            SlicerToxic sut = await proxy.AddSlicerToxicAsync(cfg =>
            {
                cfg.AverageSize = 1042;
                cfg.SizeVariation = 42;
                cfg.Delay = 142;
            }, TestContext.Current.CancellationToken);

            var createdToxic = await proxy.GetToxicAsync<SlicerToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(createdToxic);
            Assert.Equal(sut.Name, createdToxic.Name);
            Assert.Equal(sut.Type, createdToxic.Type);
            Assert.Equal(sut.AverageSize, createdToxic.AverageSize);
            Assert.Equal(sut.SizeVariation, createdToxic.SizeVariation);
            Assert.Equal(sut.Delay, createdToxic.Delay);
        }

        [Fact]
        public async Task Should_UpdateAverageSize_ForSlicerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            SlicerToxic sut = await proxy.AddSlicerToxicAsync(cfg =>
            {
                cfg.AverageSize = 1042;
                cfg.SizeVariation = 42;
                cfg.Delay = 142;
            }, TestContext.Current.CancellationToken);

            sut.AverageSize = 2042;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedSlicerToxic = await proxy.GetToxicAsync<SlicerToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedSlicerToxic);
            Assert.Equal(2042, updatedSlicerToxic.AverageSize);
        }

        [Fact]
        public async Task Should_UpdateSizeVariation_ForSlicerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            SlicerToxic sut = await proxy.AddSlicerToxicAsync(cfg =>
            {
                cfg.AverageSize = 1042;
                cfg.SizeVariation = 42;
                cfg.Delay = 142;
            }, TestContext.Current.CancellationToken);

            sut.SizeVariation = 500;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedSlicerToxic = await proxy.GetToxicAsync<SlicerToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedSlicerToxic);
            Assert.Equal(500, updatedSlicerToxic.SizeVariation);
        }

        [Fact]
        public async Task Should_UpdateDelay_ForSlicerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            SlicerToxic sut = await proxy.AddSlicerToxicAsync(cfg =>
            {
                cfg.AverageSize = 1042;
                cfg.SizeVariation = 42;
                cfg.Delay = 142;
            }, TestContext.Current.CancellationToken);

            sut.Delay = 200;
            await proxy.UpdateToxicAsync(sut, TestContext.Current.CancellationToken);

            var updatedSlicerToxic = await proxy.GetToxicAsync<SlicerToxic>(sut.Name, TestContext.Current.CancellationToken);
            Assert.NotNull(updatedSlicerToxic);
            Assert.Equal(200, updatedSlicerToxic.Delay);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingInvalidAverageSize_ForSlicerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                SlicerToxic sut = await proxy.AddSlicerToxicAsync(cfg =>
                {
                    cfg.AverageSize = -1000;
                    cfg.SizeVariation = 42;
                    cfg.Delay = 142;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("AverageSize", ex.PropertyName);
            Assert.Contains("Average size must be a non-negative value", ex.Message, StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingInvalidSizeVariation_ForSlicerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                SlicerToxic sut = await proxy.AddSlicerToxicAsync(cfg =>
                {
                    cfg.AverageSize = 512;
                    cfg.SizeVariation = -128;
                    cfg.Delay = 32;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("SizeVariation", ex.PropertyName);
            Assert.Contains("Size variation must be a non-negative value", ex.Message, StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingSizeVariationGreatherThanAverageSize_ForSlicerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                SlicerToxic sut = await proxy.AddSlicerToxicAsync(cfg =>
                {
                    cfg.AverageSize = 512;
                    cfg.SizeVariation = 600;
                    cfg.Delay = 32;
                }, TestContext.Current.CancellationToken);
            });

            Assert.Equal("SizeVariation", ex.PropertyName);
            Assert.Contains("Size variation must be smaller than average size", ex.Message, StringComparison.InvariantCulture);
        }

        [Fact]
        public async Task Should_ThrowException_WhenSettingInvalidDelay_ForSlicerToxic()
        {
            Proxy proxy = await _client.ConfigureProxyAsync(cfg =>
            {
                cfg.Name = $"test_proxy_{Guid.NewGuid()}";
                cfg.Listen = "127.0.0.1:11111";
                cfg.Upstream = "example.org:80";
            }, TestContext.Current.CancellationToken);

            var ex = await Assert.ThrowsAsync<ToxicConfigurationException>(async () =>
            {
                SlicerToxic sut = await proxy.AddSlicerToxicAsync(cfg =>
                {
                    cfg.AverageSize = 512;
                    cfg.SizeVariation = 128;
                    cfg.Delay = -32;
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
