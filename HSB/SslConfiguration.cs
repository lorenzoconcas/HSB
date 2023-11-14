/*
* This class contains all parameters needed to configure a SSL connection.
* */

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using HSB.Constants.TLS;
namespace HSB;

public class SslConfiguration
{

    //TODO -> add support for loading certificates from store

    public ushort SslPort = 8443;
    public SSL_PORT_MODE PortMode = SSL_PORT_MODE.DUAL_PORT; //by default we use two ports, one for HTTP and one for HTTPS
    public bool UpgradeUnsecureRequests = true;
    public string? CertificatePath;
    
    [JsonIgnore]
    public byte[]? CertificateBytes;
    public string? CertificatePassword;
    public List<TLSVersion> TLSVersions;
    public bool CheckCertificateRevocation = true;
    public bool ValidateClientCertificate = true;
    public bool ClientCertificateRequired = false;


    public SslConfiguration()
    {

        TLSVersions = [];

    }

    public SslConfiguration(string certificatePath, string certificatePassword)
    {

        TLSVersions = [];
        CertificatePassword = certificatePassword;
        CertificatePath = certificatePath;
    }

    public SslConfiguration(
        string certificatePath,
        string certificatePassword,
        List<TLSVersion> tlsVersions,
        bool checkCertificateRevocation = true,
        bool validateClientCertificate = false,
        bool clientCertificateRequired = false)
    {

        TLSVersions = tlsVersions;
        CertificatePassword = certificatePassword;
        CertificatePath = certificatePath;
        CheckCertificateRevocation = checkCertificateRevocation;
        ValidateClientCertificate = validateClientCertificate;
        ClientCertificateRequired = clientCertificateRequired;
    }

    public bool IsEnabled() => (CertificatePath != null || CertificateBytes != null) && CertificatePassword != null && File.Exists(CertificatePath);


    /// <summary>
    /// Returns a X509Certificate2 object from the certificate path and password.
    /// If a certificate in form of bytes is provided, it will be used instead of the path.
    /// </summary>
    /// <returns></returns>
    public X509Certificate2 GetCertificate()
    {
        if (CertificateBytes != null)
        {
            return new(CertificateBytes, CertificatePassword!);
        }
        return new(CertificatePath!, CertificatePassword!);
    }
    /// <summary>
    /// Sets the certificate path
    /// </summary>
    /// <param name="path"></param>
    public void SetCertificate(string path)
    {
        CertificatePath = path;
    }
    /// <summary>
    /// Sets the certificate data
    /// </summary>
    /// <param name="bytes"></param>
    public void SetCertificate(byte[] bytes)
    {
        CertificateBytes = bytes;
    }
    /// <summary>
    /// Sets the certificate password
    /// </summary>
    /// <param name="password"></param>
    public void SetCertificatePassword(string password)
    {
        CertificatePassword = password;
    }
    /// <summary>
    /// Returns the TLS protocols to use in a SSL Stream
    /// </summary>
    /// <returns></returns>
    /// <exception cref="DeprecatedTLSVersionException"></exception>
    internal SslProtocols GetProtocols()
    {
        SslProtocols protocols = SslProtocols.None;
        if (TLSVersions.Count == 0)
        {
            return SslProtocols.None;
        }
        foreach (var version in TLSVersions)
        {
            protocols |= version switch
            {
                TLSVersion.TLS_1_2 => SslProtocols.Tls12,
                TLSVersion.TLS_1_3 => SslProtocols.Tls13,
                TLSVersion.NOT_SET => SslProtocols.None,
                _ => throw new DeprecatedTLSVersionException(version)
            };
        }

        return protocols;
    }

    public static SslConfiguration FromJSON(JsonElement json)
    {

        //IsEnabled 

        var lastProp = "SslPort";
        try
        {

            var SslPort = Utils.Safe(json.GetProperty("SslPort").GetUInt16(), (ushort)8443);
            lastProp = "PortMode"; SSL_PORT_MODE PortMode = (SSL_PORT_MODE)Utils.Safe(json.GetProperty("PortMode").GetInt16(), (int)SSL_PORT_MODE.DUAL_PORT);
            lastProp = "upgradeUnsecureRequests"; bool upgradeUnsecureRequests = Utils.Safe(json.GetProperty("UpgradeUnsecureRequests").GetBoolean(), true);
            lastProp = "CertificatePath"; var CertificatePath = json.GetProperty("CertificatePath").GetString();
            lastProp = "CertificatePassword"; var CertificatePassword = json.GetProperty("CertificatePassword").GetString();
            lastProp = "CheckCertificateRevocation"; var CheckCertificateRevocation = Utils.Safe(json.GetProperty("CheckCertificateRevocation").GetBoolean(), true);
            lastProp = "ValidateClientCertificate"; var ValidateClientCertificate = Utils.Safe(json.GetProperty("ValidateClientCertificate").GetBoolean(), false);
            lastProp = "ClientCertificateRequired"; var ClientCertificateRequired = Utils.Safe(json.GetProperty("ClientCertificateRequired").GetBoolean(), false);
            lastProp = "tlsVersions"; var tlsVersions = json.GetProperty("TLSVersions").EnumerateArray().Select(x => (TLSVersion)x.GetInt16()).ToList();

            return new()
            {

                PortMode = PortMode,
                SslPort = SslPort,
                UpgradeUnsecureRequests = upgradeUnsecureRequests,
                CertificatePath = CertificatePath,
                CertificatePassword = CertificatePassword,
                TLSVersions = tlsVersions,
                CheckCertificateRevocation = CheckCertificateRevocation,
                ValidateClientCertificate = ValidateClientCertificate,
                ClientCertificateRequired = ClientCertificateRequired,
            };
        }
        catch (Exception e)
        {
            Terminal.ERROR("Error while parsing SslSettings property: " + lastProp + " " + e.Message);
            return new();
        }


    }

}