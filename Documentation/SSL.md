# SSL

The class holding the SSL configuration is `SslConfiguration`, and is used only by the `Configuration` class

The certificate must be in \*.p12 or \*.pkcs12 format

Here is an example of how to set the SSL mode

```cs
import HSB;

SslConfiguration sslConf = new(
    "path_to_certificate",
    "password_of_certificate"
);

Configuration c = new(){
    SslSettings = sslConf,
    Port = 443, //we're forcing the port to 443
};

new Server(c).Start();
```

This code is very similiar to the example shown [here](./Library.md), with the addition of the SslConfiguration parameter

### Methods and parameters

The SslConfiguration contains the following parameters

| Name                             | Return Type        | Type     | Description                                                                                             |
| -------------------------------- | ------------------ | -------- | ------------------------------------------------------------------------------------------------------- |
| `IsEnabled`                      | `bool`             | Property | Set if the ssl mode is enabled, this value is set automatically when a valid path or certificate is set |
| `PortMode`                       | `void`             | Property | Sets if the server must listen to a secondary port for TLS requests                                     |
| `upgradeUnsecureRequests`        | `bool`             | Property | If set, http requests are redirected to https://                                                        |
| `TLSVersions`                    | `List<TLSVersion>` | Property | If set the server will use these TLS version with the client \*                                         |
| `SetCertificatePassword(string)` | void               | Function |                                                                                                         |
| `SetCertificate(string)`         | void               | Function | Sets the path of the certificate                                                                        |
| `SetCertificate(byte[])`         | void               | Function | Loads the certificate from a byte array                                                                 |

- TLS 1.0 (SSL3.0) and TLS 1.1 (SSL 3.1) are deprecated and their usage will throw an Exception

Note that `upgradeUnsecureRequests` will also redirect normal plain request sent to the base port, not only the invalid plain request that the SslPort can receive

### Exceptions

| Name                          | Description                                                   |
| ----------------------------- | ------------------------------------------------------------- |
| DeprecatedTLSVersionException | This exception is thrown when a depreceted TLS version is set |
