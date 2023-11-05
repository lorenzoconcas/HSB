namespace HSB.Constants.TLS;

/// <summary>
/// This enum is used to determine if the server should listen on a single port for both HTTP and HTTPS or if it should listen on two different ports
/// Note that when SSL is not set this setting is ignored (and is considered analog to a single port)
/// </summary>
public enum SSL_PORT_MODE{
    SINGLE_PORT, //One port for both HTTP and HTTPS, note that at the moment if SSL is enabled we cannot determine if the content of an HTTP request
    DUAL_PORT, //One for the SSL and one for the plain HTTP
}