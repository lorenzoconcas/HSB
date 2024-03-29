//this class is used to spawn curl requests to the server

using System.Diagnostics;
using HSB.TLS.Constants;
using System.Runtime.InteropServices;

namespace HSB.TLS;
public class Curl(string url = "localhost", string port = "443")
{
    
    readonly string fullUrl = $"https://{url}:{port}/";

    ProtocolVersion protocolVersion = new(TLSVersion.TLS_1_3);

    string cipherSuitesString = "";

    public void SetTLSVersion(TLSVersion tlsVersion)
    {
        protocolVersion = new(tlsVersion);
    }

    public void SetCipherSuites(string cipherSuites)
    {
        cipherSuitesString = cipherSuites;
    }

    private string GetCurlCommand()
    {

        string command = $"curl -k --tlsv{protocolVersion} --tls-max {protocolVersion} ";
        if (cipherSuitesString != "")
        {
            command += $"--ciphers {cipherSuitesString} ";
        }
        command = command[..^1];
        command += $" {fullUrl}";
        return command;
    }


    /// <summary>
    /// Runs the curl command
    /// </summary>
    /// <param name="loop">Wether or not repeat the command</param>
    /// <param name="delay">Delay between each request</param>
    /// <param name="times">If loop is true, specifies how many times the command must be repeated (-1 means forever)</param>
    public void RunAsync(bool loop = true, int delay = 250, int times = -1)
    {

        Task.Run(() =>
        {
            if (loop)
            {

                bool _loop = true;
                while (_loop)
                {
                    Thread.Sleep(delay);
                    if (times != -1)
                    {
                        times--;
                        if (times == 0)
                        {
                            _loop = false;
                        }
                    }
                    Exec();

                }
            }
            else
            {
                Thread.Sleep(delay);
                Exec();
            }
        });
    }

    private void Exec()
    {
        string command = GetCurlCommand();
        ProcessStartInfo startInfo = new();
        //if windows
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/C \"{command}\"";

        }
        else 
        {
            startInfo.FileName = "/bin/zsh";
            startInfo.Arguments = $"-c \"{command}\"";
        }
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;
        Process process = new()
        {
            StartInfo = startInfo
        };
        Console.WriteLine($"Starting curl... \n{command}");
        process.Start();
    }


}
