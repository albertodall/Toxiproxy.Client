namespace Toxiproxy.Client
{
    public class ToxiproxyException : Exception
    {
        public ToxiproxyException(string message) : base(message) { }
        public ToxiproxyException(string message, Exception innerException) : base(message, innerException) { }
    }

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
            : base($"Proxy '{proxyName}' not found") { }
    }

    public sealed class ProxyConfigurationException : ToxiproxyException
    {
        public ProxyConfigurationException(string propertyName)
            : base($"Invalid proxy configuration: Parameter '{propertyName}' has an invalid value")
        { }
    }
}
