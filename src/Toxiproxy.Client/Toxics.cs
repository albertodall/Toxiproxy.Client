
namespace Toxiproxy.Client
{
    /// <summary>
    /// Implementation of the "latency" toxic.
    /// <see href="https://github.com/Shopify/toxiproxy?tab=readme-ov-file#latency"/>
    /// Adds a delay to all data going through the proxy. The delay is equal to latency +/- jitter.
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

    /// <summary>
    /// Implementation of the "bandwidth" toxic.
    /// <see href="https://github.com/Shopify/toxiproxy?tab=readme-ov-file#bandwidth"/>
    /// Limits a connection to a maximum number of kilobytes per second.
    /// </summary>
    public sealed class BandwidthToxic : Toxic
    {
        internal BandwidthToxic()
            : base(ToxicType.Bandwidth)
        { }

        internal BandwidthToxic(ToxicConfiguration configuration)
            : base(configuration) 
        { }

        /// <summary>
        /// Bandwidth rate in KB/s.
        /// </summary>
        public int Rate
        {
            get => GetAttribute<int>("rate");
            set
            {
                if (value < 0)
                {
                    throw new ToxicConfigurationException(nameof(Rate), "Rate must be a non-negative value.");
                }

                Attributes["rate"] = value;
            }
        }
    }

    /// <summary>
    /// Implementation of the "timeout" toxic.
    /// <see href="https://github.com/Shopify/toxiproxy?tab=readme-ov-file#timeout"/>
    /// If timeout is 0, the connection won't close, and data will be dropped until the toxic is removed.
    /// </summary>
    public sealed class TimeoutToxic : Toxic
    {
        internal TimeoutToxic()
            : base(ToxicType.Timeout)
        { }

        internal TimeoutToxic(ToxicConfiguration configuration)
            : base(configuration)
        { }

        /// <summary>
        /// Timeout time in milliseconds.
        /// </summary>
        public int Timeout
        {
            get => GetAttribute<int>("timeout");
            set
            {
                if (value < 0)
                {
                    throw new ToxicConfigurationException(nameof(Timeout), "Timeout must be a non-negative value.");
                }

                Attributes["timeout"] = value;
            }
        }
    }

    /// <summary>
    /// Implementation of the "slow_close" toxic.
    /// <see href="https://github.com/Shopify/toxiproxy?tab=readme-ov-file#slow_close"/>
    /// Delays the TCP socket from closing until delay has elapsed.
    /// </summary>
    public sealed class SlowCloseToxic : Toxic
    {
        internal SlowCloseToxic()
            : base(ToxicType.SlowClose)
        { }

        internal SlowCloseToxic(ToxicConfiguration configuration)
            : base(configuration)
        { }

        /// <summary>
        /// Delay time in milliseconds.
        /// </summary>
        public int Delay
        {
            get => GetAttribute<int>("delay");
            set
            {
                if (value < 0)
                {
                    throw new ToxicConfigurationException(nameof(Delay), "Delay must be a non-negative value.");
                }

                Attributes["delay"] = value;
            }
        }
    }

    /// <summary>
    /// Implementation of the "slicer" toxic.
    /// <see href="https://github.com/Shopify/toxiproxy?tab=readme-ov-file#slicer"/>
    /// Slices TCP data up into small bits, optionally adding a delay between each sliced "packet".
    /// </summary>
    public sealed class SlicerToxic : Toxic
    {
        internal SlicerToxic()
            : base(ToxicType.Slicer)
        { }

        internal SlicerToxic(ToxicConfiguration configuration)
            : base(configuration)
        { }

        /// <summary>
        /// Size in bytes of an average packet.
        /// </summary>
        public int AverageSize
        {
            get => GetAttribute<int>("average_size");
            set
            {
                if (value < 0)
                {
                    throw new ToxicConfigurationException(nameof(AverageSize), "Average size must be a non-negative value.");
                }

                Attributes["average_size"] = value;
            }
        }

        /// <summary>
        /// Variation in bytes of an average packet (should be smaller than <see cref="AverageSize"/>).
        /// </summary>
        public int SizeVariation 
        { 
            get => GetAttribute<int>("size_variation");
            set
            {
                if (value < 0)
                {
                    throw new ToxicConfigurationException(nameof(SizeVariation), "Size variation must be a non-negative value.");
                }

                if (value >= AverageSize)
                {
                    throw new ToxicConfigurationException(nameof(SizeVariation), $"Size variation must be smaller than average size ({AverageSize}).");
                }

                Attributes["size_variation"] = value;
            }
        }

        /// <summary>
        /// Time in microseconds to delay each packet by.
        /// </summary>
        public int Delay
        {
            get => GetAttribute<int>("delay");
            set
            {
                if (value < 0)
                {
                    throw new ToxicConfigurationException(nameof(Delay), "Delay must be a non-negative value.");
                }

                Attributes["delay"] = value;
            }
        }
    }
}
