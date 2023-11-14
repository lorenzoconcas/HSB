using System.Net.Sockets;
using System.Net;
using System.Text;
using HSB.TLS;
using HSB.TLS.Constants;

namespace HSB;

public class Custom
{
    Curl c = new();
    IPAddress address = IPAddress.Any;
    IPEndPoint endPoint;
    Socket listener;

    byte[] buffer = new byte[512];


    public Custom(string[] args)
    {
        Console.WriteLine("Hello from HSB");
        c.setTLSVersion(TLSVersion.TLS_1_2);
        c.RunAsync(false, 700);
        //c.setCipherSuites("TLS_AES_256_GCM_SHA384 TLS_CHACHA20_POLY1305_SHA256 TLS_AES_128_GCM_SHA256 TLS_AES_128_CCM_8_SHA256 TLS_AES_128_CCM_SHA256");

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
        while (true)
        {
            Socket newSocket = listener.Accept();
            int received = newSocket.Receive(buffer);
            if (received > 0 && buffer[0] == 0x16) //parse client hello
            {
                new CustomTLS(newSocket).Parse(buffer);
            }
            else
            {
                //Plain HTTP methods are NOT allowed
                string reply = "HTTP/1.1 405 Method Not Allowed\r\n" +
                    "Content-Type: text/html\r\n" +
                    "Content-Length: 0\r\n" +
                    "Connection: close\r\n" +
                    "\r\n";

                newSocket.Send(Encoding.UTF8.GetBytes(reply));
                newSocket.Close();
            }

            Thread.Sleep(125);
        }

    }

}