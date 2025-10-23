namespace Toxiproxy.Client
{
    /// <summary>
    /// Base exception for errors that occur during Toxiproxy operations.
    /// </summary>
    public class ToxiproxyException : Exception
    {
        public ToxiproxyException(string message) : base(message) { }
        public ToxiproxyException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Error while connecting to the Toxiproxy server.
    /// </summary>
    public sealed class ToxiproxyConnectionException : ToxiproxyException
    {
        public ToxiproxyConnectionException(string message)
            : base($"Failed to connect to Toxiproxy server: {message}") 
        { }

        public ToxiproxyConnectionException(string message, Exception innerException)
            : base($"Failed to connect to Toxiproxy server: {message}", innerException) 
        { }
    }

    public sealed class ProxyNotFoundException : ToxiproxyException
    {
        public ProxyNotFoundException(string proxyName)
            : base($"Proxy '{proxyName}' not found") 
        { }
    }

    /// <summary>
    /// Error while configuring a proxy due to invalid parameters.
    /// </summary>
    public sealed class ProxyConfigurationException : ToxiproxyException
    {
        public ProxyConfigurationException(string propertyName, string message)
            : base($"Invalid proxy configuration: Parameter '{propertyName}' has an invalid value. {message}")
        { }
    }

    /// <summary>
    /// Error while configuring a toxic on a proxy due to invalid parameters.
    /// </summary>
    public sealed class ToxicConfigurationException : ToxiproxyException
    {
        public ToxicConfigurationException(string propertyName, string message)
            : base($"Invalid toxic configuration: Parameter '{propertyName}' has an invalid value. {message}")
        { }
    }
}
