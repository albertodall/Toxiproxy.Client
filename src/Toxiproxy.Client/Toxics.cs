
namespace Toxiproxy.Client
{
    /// <summary>
    /// Implementation of the "latency" toxic.
    /// <see href="https://github.com/Shopify/toxiproxy?tab=readme-ov-file#latency"/>
    /// </summary>
    public sealed class LatencyToxic : Toxic
    {
        internal LatencyToxic() 
            : base(ToxicType.Latency)
        { }

        internal LatencyToxic(ToxicConfiguration configuration)
            : base(configuration)
        { }

        /// <summary>
        /// Latency time in milliseconds
        /// </summary>
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

        /// <summary>
        /// Jitter time in milliseconds.
        /// </summary>
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
