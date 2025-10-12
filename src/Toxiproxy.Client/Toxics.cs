
namespace Toxiproxy.Client
{
    public sealed class LatencyToxic : Toxic
    {
        internal LatencyToxic(Proxy proxy, ToxicConfiguration data) : base(proxy, data) 
        { }

        public int Latency => GetAttribute<int>("latency");
        public int Jitter => GetAttribute<int>("jitter");
    }
}
