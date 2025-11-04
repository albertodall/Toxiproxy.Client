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

        public static ToxicDirection Upstream => new("upstream");
        public static ToxicDirection Downstream => new("downstream");

        public static implicit operator ToxicDirection(string value)
        {
            string direction = value.ToLowerInvariant();
            if (direction != "upstream" && direction != "downstream")
            {
                throw new ToxicConfigurationException(nameof(direction), "Direction must be either 'upstream' or 'downstream'.");
            }

            return new ToxicDirection(direction);
        }

        public static implicit operator string(ToxicDirection direction) => direction.ToString();

        public override string ToString() => _direction;
    }
}

