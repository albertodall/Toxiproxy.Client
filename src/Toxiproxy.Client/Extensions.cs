namespace Toxiproxy.Client
{
    internal static class Extensions
    {
        /// <summary>
        /// Returns a serializer-friendly configuration of a <see cref="Proxy"/>.
        /// </summary>
        /// <param name="proxy">The <see cref="Proxy"/>.</param>
        public static ProxyConfiguration GetConfiguration(this Proxy proxy)
        {
            return new ProxyConfiguration
            {
                Name = proxy.Name,
                Listen = proxy.Listen,
                Upstream = proxy.Upstream,
                Enabled = proxy.Enabled,
                Toxics = proxy.Toxics.Select(t => t.GetConfiguration()).ToArray()
            };
        }

        /// <summary>
        /// Returns a serializer-friendly configuration of a <see cref="Toxic"/>.
        /// </summary>
        /// <param name="toxic">The <see cref="Toxic"/>.</param>
        public static ToxicConfiguration GetConfiguration(this Toxic toxic)
        {
            return new ToxicConfiguration
            {
                Name = toxic.Name,
                Type = toxic.Type,
                Stream = toxic.Stream,
                Toxicity = toxic.Toxicity,
                Attributes = toxic.Attributes
            };
        }
    }
}
