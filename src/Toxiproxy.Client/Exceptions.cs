using System;

namespace Toxiproxy.Client
{
    public class ToxiproxyException : Exception
    {
        public ToxiproxyException(string message) : base(message) { }
        public ToxiproxyException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ToxiproxyConnectionException : ToxiproxyException
    {
        public ToxiproxyConnectionException(string message)
            : base($"Failed to connect to Toxiproxy server: {message}") 
        { }

        public ToxiproxyConnectionException(string message, Exception innerException)
            : base($"Failed to connect to Toxiproxy server: {message}", innerException) 
        { }
    }

    public class ProxyNotFoundException : ToxiproxyException
    {
        public ProxyNotFoundException(string proxyName)
            : base($"Proxy '{proxyName}' not found") { }
    }
}
