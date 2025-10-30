
namespace Toxiproxy.Client
{
    public sealed class LatencyToxic : Toxic
    {
        internal LatencyToxic(ToxicConfiguration data) 
            : base(ToxicType.Latency, data)
        { }

        public int Latency
        {
            get => GetAttribute<int>("latency");
            set 
            {
                if (value < 0)
                {
                    throw new ToxicConfigurationException(nameof(Latency), "Latency must be a non-negative value.");
                }

                Attributes["latency"] = value;
            } 
        }

        public int Jitter
        {
            get => GetAttribute<int>("jitter");
            set
            {
                if (value < 0)
                {
                    throw new ToxicConfigurationException(nameof(Jitter), "Jitter must be a non-negative value.");
                }

                Attributes["jitter"] = value;
            }
        }
    }
}
