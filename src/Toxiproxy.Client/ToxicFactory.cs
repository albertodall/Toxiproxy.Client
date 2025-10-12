namespace Toxiproxy.Client
{
    internal static class ToxicFactory
    {
        public static Toxic CreateToxic(ToxicConfiguration config)
        {
            return config.Type.ToLowerInvariant() switch
            {
                "latency" => new LatencyToxic(config),
                //"bandwidth" => new BandwidthToxic(data),
                //"timeout" => new TimeoutToxic(data),
                //"slow_close" => new SlowCloseToxic(data),
                //"slicer" => new SlicerToxic(data),
                //"limit_data" => new LimitDataToxic(data),
                //"reset_peer" => new ResetPeerToxic(data),
                _ => throw new InvalidOperationException($"Unknown toxic type: {config.Type}")
            };
        }

        public static T CreateToxic<T>(ToxicConfiguration data) where T : Toxic
        {
            var toxic = CreateToxic(data);
            if (toxic is T createdToxic)
            {
                return createdToxic;
            }

            throw new InvalidCastException($"Cannot cast toxic '{data.Name}' of type '{data.Type}' to {typeof(T).Name}");
        }
    }

}
