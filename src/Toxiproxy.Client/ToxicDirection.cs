namespace Toxiproxy.Client
{
    /// <summary>
    /// In which direction a toxic is working: upstream or downstream.
    /// </summary>
    public sealed record ToxicDirection
    {
        private readonly string _direction;

        private ToxicDirection(string direction)
        {
            _direction = direction;
        }

        /// <summary>
        /// Toxic is working upstream.
        /// </summary>
        public static ToxicDirection Upstream => new("upstream");

        /// <summary>
        /// Toxic is working downstream.
        /// </summary>
        public static ToxicDirection Downstream => new("downstream");

        /// <inheritdoc />
        public static implicit operator ToxicDirection(string value)
        {
            string direction = value.ToLowerInvariant();
            if (direction != "upstream" && direction != "downstream")
            {
                throw new ToxicConfigurationException(nameof(direction), "Direction must be either 'upstream' or 'downstream'.");
            }

            return new ToxicDirection(direction);
        }

        /// <inheritdoc />
        public static implicit operator string(ToxicDirection direction) => direction.ToString();

        /// <inheritdoc />
        public override string ToString() => _direction;
    }
}

