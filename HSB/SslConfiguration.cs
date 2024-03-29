/*
* This class contains all parameters needed to configure a SSL connection.
* */

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using HSB.Constants.TLS;
using HSB.Exceptions;
namespace HSB;

public class SslConfiguration
{

    private static readonly string DEBUG_CERT_FOLDER_PATH =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HSB");
    private static readonly string DEBUG_CERT_PASSWORD = "HSB_DEV_CERT_PASSWORD";
    private static readonly string DEBUG_CERT_P12_PATH = Path.Combine(DEBUG_CERT_FOLDER_PATH, "HSB_DEV_CERT.p12");
    private static readonly string DEBUG_CERT_CRT_PATH = Path.Combine(DEBUG_CERT_FOLDER_PATH, "HSB_DEV_CERT.crt");
    private static readonly string DEBUG_CERT_KEY_PATH = Path.Combine(DEBUG_CERT_FOLDER_PATH, "HSB_DEV_CERT.key");



    //TODO -> add support for loading certificates from store

    public ushort SslPort = 8443;
    public SSL_PORT_MODE PortMode = SSL_PORT_MODE.DUAL_PORT; //by default we use two ports, one for HTTP and one for HTTPS
    public bool UpgradeUnsecureRequests = true;
    public string? CertificatePath;

    /// <summary>
    /// When set, a developer certificate valid only for 1 month and for localhost will be created (if doesn't exist or is expired) and used.
    /// OpenSSL is required to use this feature.
    /// </summary>
    public bool UseDebugCertificate = false;
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
    public bool IsDebugModeEnabled() => UseDebugCertificate;


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

    private static bool CheckOpenSslInstalled()
    {
        //check if openssl is installed
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        //maybe this case can be removed, but needs testing
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C openssl version";

        }
        else
        {
            startInfo.FileName = "openssl";
            startInfo.Arguments = $"version";
        }
        var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Terminal.ERROR($"❌ Openssl is not installed, cannot continue ({process.ExitCode})", true);
            return false;
        }

        Terminal.DEBUG("Openssl is installed", true);

        return true;
    }

    private static bool PrepareFolders()
    {
        //if old certificate exists, delete it
        if (Directory.Exists(DEBUG_CERT_FOLDER_PATH))
        {
            if (File.Exists(DEBUG_CERT_P12_PATH))
            {
                File.Delete(DEBUG_CERT_P12_PATH);
            }
            if (File.Exists(DEBUG_CERT_CRT_PATH))
            {
                File.Delete(DEBUG_CERT_CRT_PATH);
            }
            if (File.Exists(DEBUG_CERT_KEY_PATH))
            {
                File.Delete(DEBUG_CERT_KEY_PATH);
            }
        }
        else //create folder if not exists
        {
            Terminal.DEBUG("Creating debug certificate folder", true);
            try
            {
                DirectoryInfo dirInfo = Directory.CreateDirectory(DEBUG_CERT_FOLDER_PATH);

                Terminal.DEBUG($"Debug certificate folder created at {dirInfo.FullName}", true);
            }
            catch (Exception e)
            {
                Terminal.ERROR($"Cannot create debug certificate folder: {e.Message}", true);
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Creates a developer certificate valid only for 1 month and for localhost.
    /// OpenSSL is required to use this feature.
    /// </summary>
    public static bool CreateDebugCertificate()
    {
        //check prerequisites
        if (!CheckOpenSslInstalled())
        {
            return false;
        }
        //remove old certificates, create folders if needed
        if (!PrepareFolders())
        {
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "openssl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        List<string> commands = [
            //command 1
            $"req -x509 -newkey rsa:4096 -sha256 -days 30 -nodes -subj \"/CN=localhost/C=US\" " +
            $"-keyout \"{DEBUG_CERT_KEY_PATH}\" " +
            $"-out \"{DEBUG_CERT_CRT_PATH}\"",
            //command 2
            $"pkcs12 -export " +
            $"-out \"{DEBUG_CERT_P12_PATH}\" " +
            $"-inkey \"{DEBUG_CERT_KEY_PATH}\" " +
            $"-in \"{DEBUG_CERT_CRT_PATH}\" " +
            $"-passout pass:\"{DEBUG_CERT_PASSWORD}\""
        ];

        Process process;

        foreach (var arg in commands)
        {
            //set the command argument
            startInfo.Arguments = arg;

            process = new()
            {
                StartInfo = startInfo
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Terminal.WARNING($"Openssl error, certificate has not been created", true);
                return false;
            }

        }
        Terminal.DEBUG($"Debug certificate (valid only for localhost) created successfully", true);
        return true;
    }

    public static X509Certificate2? TryLoadDebugCertificate(bool create = true, Configuration? c = null)
    {
        c?.Debug.DEBUG("Loading debug certificate");

        X509Certificate2 cert;

        if (!File.Exists(DEBUG_CERT_P12_PATH))
        {
            if (create)
            {
                if (!CreateDebugCertificate())
                {
                    c?.Debug.WARNING("Cannot load debug certificate, file not found");
                    return null;
                }

            }
            else
            {
                c?.Debug.WARNING("Cannot load debug certificate, file not found");
                //Terminal.DEBUG("Cannot load debug certificate, file not found");
                return null;
            }
        }
        //workaround  to avoid error if path contains spaces
        var bytes = File.ReadAllBytes(DEBUG_CERT_P12_PATH);
        cert = new X509Certificate2(bytes, DEBUG_CERT_PASSWORD);

        //if expired, delete and create again
        if (cert.NotAfter < DateTime.Now)
        {
            File.Delete(DEBUG_CERT_P12_PATH);
            if (!CreateDebugCertificate())
            {
                c?.Debug.DEBUG("Cannot load debug certificate, file not found");
                return null;
            }

            cert = new X509Certificate2(DEBUG_CERT_P12_PATH, DEBUG_CERT_PASSWORD);
        }

        c?.Debug.DEBUG("Debug certificate loaded. Remember to trust it in your system!");
        return cert;
    }
}