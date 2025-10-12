
namespace Toxiproxy.Client
{
    public sealed class LatencyToxic : Toxic
    {
        internal LatencyToxic(ToxicConfiguration data) : base(data)
        { }

        public int Latency
        {
            get => GetAttribute<int>("latency");
            set => Attributes["latency"] = value;
        }
        public int Jitter
        {
            get => GetAttribute<int>("jitter");
            set => Attributes["jitter"] = value;
        }
    }
}
