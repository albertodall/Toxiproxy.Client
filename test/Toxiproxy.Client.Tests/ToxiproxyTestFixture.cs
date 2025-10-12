[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Toxiproxy.Client.Tests
{
    public sealed class ToxiproxyTestFixture
    {
        private const string Toxiproxy_Host = "localhost";
        private const int Toxiproxy_Port = 8474;

        public ToxiproxyTestFixture()
        {
            Client = new ToxiproxyClient(Toxiproxy_Host, Toxiproxy_Port);
        }

        public ToxiproxyClient Client { get; private set; }
    }
}
