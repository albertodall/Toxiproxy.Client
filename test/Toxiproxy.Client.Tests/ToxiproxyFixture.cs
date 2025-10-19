
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Toxiproxy.Client.Tests
{
    public sealed class ToxiproxyFixture : IAsyncLifetime
    {
        private const string Toxiproxy_Host = "localhost";
        private const int Toxiproxy_Port = 8474;

        public ToxiproxyClient Client { get; private set; } = default!;

        public async ValueTask InitializeAsync()
        {
            Client = await ToxiproxyClient.ConnectAsync(Toxiproxy_Host, Toxiproxy_Port);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;       
    }
}
