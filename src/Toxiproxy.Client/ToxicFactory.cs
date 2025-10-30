namespace Toxiproxy.Client
{
    public static class ToxicType
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
            return configuration.Type.ToLowerInvariant() switch
            {
                "latency" => new LatencyToxic(configuration),
                //"bandwidth" => new BandwidthToxic(data),
                //"timeout" => new TimeoutToxic(data),
                //"slow_close" => new SlowCloseToxic(data),
                //"slicer" => new SlicerToxic(data),
                //"limit_data" => new LimitDataToxic(data),
                //"reset_peer" => new ResetPeerToxic(data),
                _ => throw new InvalidOperationException($"Unknown toxic type: {configuration.Type}")
            };
        }

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
