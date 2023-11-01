using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using HSB.TLS;

namespace HSB;

public class SystemTLS
{
    readonly Curl c = new(port: "443");

    static X509Certificate2 Certificate => new("PATH", "PASSWORD");

    readonly IPAddress address = IPAddress.Any;
    readonly IPEndPoint endPoint;
    readonly Socket listener;
    byte[] buffer = new byte[512];
    public SystemTLS(string[] args)
    {
        Console.WriteLine("Hello from HSB (SystemTLS)");

        c.setTLSVersion(TLS.Constants.TLSVersion.TLS_1_2);
        c.RunAsync(false, 700);


        endPoint = new(address, 443);


        listener = new(
            address.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        listener.Bind(endPoint);
        listener.Listen(100);
    }

    public void Run()
    {
        Socket newSocket = listener.Accept();
        SslStream sslStream = new(new NetworkStream(newSocket), true);

        try
        {
            sslStream.AuthenticateAsServer(
                Certificate,
                false,
                System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12,
                false
            );

            //print ssl informations
            Console.WriteLine("SSL Protocol: {0}", sslStream.SslProtocol);
            Console.WriteLine("Cipher: {0} {1} bits", sslStream.CipherAlgorithm, sslStream.CipherStrength);
            Console.WriteLine("Hash: {0} {1} bits", sslStream.HashAlgorithm, sslStream.HashStrength);
            Console.WriteLine("Key exchange: {0} {1} bits", sslStream.KeyExchangeAlgorithm, sslStream.KeyExchangeStrength);
            Console.WriteLine("\n");

            int received = sslStream.Read(buffer);

            if (received > 0)
            {
                Console.WriteLine("Received {0} bytes", received);
                string req = Encoding.UTF8.GetString(buffer[..received]);
                Console.WriteLine("Req: {0}\nFine stampa", req);

                var lines = req.Split("\r\n");
                var method = lines[0].Split(" ")[0];
                var path = lines[0].Split(" ")[1];

                string reply;
                if (path == "/")
                    reply = "HTTP/1.1 200 OK\r\n" +
                        "Content-Type: text/html\r\n" +
                        "Content-Length: 20\r\n" +
                        "Connection: close\r\n" +
                        "\r\n" +
                        "Hello from HSB (TLS)";
                else //not found

                    reply = "HTTP/1.1 404 Not Found\r\n" +
                "Content-Type: text/html\r\n" +
                "Content-Length: 0\r\n" +
                "Connection: close\r\n" +
                "\r\n";

                Console.WriteLine("Reply: {0}", reply);
                var bytes = Encoding.UTF8.GetBytes(reply);
                sslStream.Write(bytes);

                sslStream.Close();

            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
            return;
        }

    }
}