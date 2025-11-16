namespace Toxiproxy.Client
{
    /// <summary>
    /// List of supported toxic types.
    /// This list maps against the values expected by Toxiproxy server.
    /// <see href="https://github.com/Shopify/toxiproxy?tab=readme-ov-file#toxics"/>
    /// </summary>
    internal static class ToxicType
    {
        public const string Latency = "latency";
        public const string Bandwidth = "bandwidth";
        public const string Timeout = "timeout";
        public const string SlowClose = "slow_close";
        public const string Slicer = "slicer";
        public const string LimitData = "limit_data";
        public const string ResetPeer = "reset_peer";
    }

    internal static class ToxicFactory
    {
        public static Toxic CreateToxic(ToxicConfiguration configuration)
        {
            string toxicType = configuration.Type.ToLowerInvariant();
            return toxicType switch
            {
                ToxicType.Latency => new LatencyToxic(configuration),
                ToxicType.Bandwidth => new BandwidthToxic(configuration),
                ToxicType.Timeout => new TimeoutToxic(configuration),
                ToxicType.SlowClose => new SlowCloseToxic(configuration),
                ToxicType.Slicer => new SlicerToxic(configuration),
                ToxicType.LimitData => new LimitDataToxic(configuration),
                ToxicType.ResetPeer => new ResetPeerToxic(configuration),
                _ => throw new ToxicConfigurationException(nameof(configuration.Type), $"Unknown toxic type: {toxicType}")
            };
        }

        /// <summary>
        /// Creates a toxic based on the provided configuration.
        /// </summary>
        /// <typeparam name="T">Type of the created <see cref="Toxic"/>.</typeparam>
        /// <param name="configuration">The toxic configuration.</param>
        /// <returns>The instance of the <see cref="Toxic"/>.</returns>
        /// <exception cref="InvalidCastException"></exception>
        public static T CreateToxic<T>(ToxicConfiguration configuration) where T : Toxic
        {
            var toxic = CreateToxic(configuration);
            if (toxic is T createdToxic)
            {
                return createdToxic;
            }

            throw new InvalidCastException($"Cannot cast toxic '{configuration.Name}' of type '{configuration.Type}' to {typeof(T).Name}");
        }
    }
}
