/*
* This class contains all parameters needed to configure a SSL connection.
* */

using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using HSB.Constants.TLS;
namespace HSB;

public class SslConfiguration
{

    //TODO -> add support for loading certificates from store
    public bool enabled = false;
    public string? CertificatePath;
    public string? CertificatePassword;
    public List<TLSVersion> TLSVersions;
    public bool CheckCertificateRevocation = true;
    public bool ValidateClientCertificate = true;
    public bool ClientCertificateRequired = false;


    public SslConfiguration()
    {
        enabled = false;
        TLSVersions = new List<TLSVersion>();
     
    }

    public SslConfiguration(string certificatePath, string certificatePassword)
    {
        enabled = true;
        TLSVersions = new List<TLSVersion>();
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
        enabled = true;
        TLSVersions = tlsVersions;
        CertificatePassword = certificatePassword;
        CertificatePath = certificatePath;
        CheckCertificateRevocation = checkCertificateRevocation;
        ValidateClientCertificate = validateClientCertificate;
        ClientCertificateRequired = clientCertificateRequired;
    }

    public bool ConfigIsValid() => enabled && CertificatePath != null && CertificatePassword != null && File.Exists(CertificatePath);

    public X509Certificate2 GetCertificate()
    {
        return new(CertificatePath!, CertificatePassword!);
    }

    public SslProtocols GetProtocols(){
        SslProtocols protocols = SslProtocols.None;
        if(TLSVersions.Count == 0){
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
        return new()
        {
            enabled = true,
            CertificatePath = json.GetProperty("certificatePath").GetString() ?? "",
            CertificatePassword = json.GetProperty("certificatePassword").GetString() ?? "",
            CheckCertificateRevocation = json.GetProperty("checkCertificateRevocation").GetBoolean(),
            ValidateClientCertificate = json.GetProperty("validateClientCertificate").GetBoolean(),
            ClientCertificateRequired = json.GetProperty("ClientCertificateRequired").GetBoolean()
        };

    }

}