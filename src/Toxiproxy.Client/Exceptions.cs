namespace Toxiproxy.Client
{
    /// <summary>
    /// Base exception for errors that occur during Toxiproxy operations.
    /// </summary>
    public class ToxiproxyException : Exception
    {
        /// <inheritdoc />
        public ToxiproxyException(string message) : base(message) { }

        /// <inheritdoc />
        public ToxiproxyException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// You're trying to access a proxy that does not exist.
    /// </summary>
    public sealed class ProxyNotFoundException : ToxiproxyException
    {
        /// <inheritdoc />
        public ProxyNotFoundException(string message)
            : base(message) 
        { }
    }

    /// <summary>
    /// Error while configuring a proxy due to invalid parameters.
    /// </summary>
    public sealed class ProxyConfigurationException : ToxiproxyException
    {
        /// <inheritdoc />
        public ProxyConfigurationException(string propertyName, string message)
            : base($"Invalid proxy configuration: Parameter '{propertyName}' has an invalid value. {message}")
        { 
            PropertyName = propertyName;
        }

        /// <summary>
        /// Name of the parameter containing the error.
        /// </summary>
        public string PropertyName { get; }
    }

    /// <summary>
    /// Error while configuring a toxic on a proxy due to invalid parameters.
    /// </summary>
    public sealed class ToxicConfigurationException : ToxiproxyException
    {
        /// <inheritdoc />
        public ToxicConfigurationException(string propertyName, string message)
            : base($"Invalid toxic configuration: Parameter '{propertyName}' has an invalid value. {message}")
        { 
            PropertyName = propertyName;
        }

        /// <summary>
        /// Name of the parameter containing the error.
        /// </summary>
        public string PropertyName { get; }
     }
}
