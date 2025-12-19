using HSB;

/*
* This example shows how to set HSB in SSL mode using Manual TLS Handshake (POC).
* It uses the OpenSSL-generated debug certificate.
* */

var sslConfig = new SslConfiguration
{
    UseDebugCertificate = true, // Uses OpenSSL to generate/load debug cert
    SslHandler = SslHandler.HSB,
    UpgradeUnsecureRequests = false,
    SslPort = 8443
};

Configuration c = new()
{
    Port = 8080,
    SslSettings = sslConfig,
};

Console.WriteLine("Starting server with Manual TLS (POC)... (OpenSSL required for cert generation)");
new Server(c).Start();