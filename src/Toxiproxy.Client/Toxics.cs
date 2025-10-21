
namespace Toxiproxy.Client
{
    public sealed class LatencyToxic : Toxic
    {
        internal LatencyToxic(ToxicConfiguration data) : base(data)
        { 
            data.Type = "latency";
        }

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

        public override void EnsureConfigurationIsValid()
        {
            base.EnsureConfigurationIsValid();

            if (Latency < 0)
            {
                throw new ToxicConfigurationException(nameof(Latency), "Latency must be non-negative value.");
            }

            if (Jitter < 0)
            {
                throw new ToxicConfigurationException(nameof(Jitter), "Jitter must be a non-negative value.");
            }
        }
    }
}
