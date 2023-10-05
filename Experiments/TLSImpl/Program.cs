/*
* This code is used to implement the TLS protocol.
* This project will be deleted once is ready and moved to the main library project.
* Some parts of the code are taken from the library project
*/

using System.Net.Sockets;
using System.Net;
using System.Text;
using HSB.TLS;


Console.WriteLine("Hello from HSB");

//Curl class is a little wrapper around the curl library, that makes and HTTPS request to this code
//remove if curl is not installed
Curl c = new();
c.setTLSVersion(ProtocolVersion.TLSVersionEnum.TLS_1_2);
c.RunAsync(false, 700);
//c.setCipherSuites("TLS_AES_256_GCM_SHA384 TLS_CHACHA20_POLY1305_SHA256 TLS_AES_128_GCM_SHA256 TLS_AES_128_CCM_8_SHA256 TLS_AES_128_CCM_SHA256");


//the new server will listen to any IPv4 address on port 443
//we must verify that we're allowed to listen on that port

IPAddress address = IPAddress.Any;
IPEndPoint endPoint = new(address, 443);
Socket listener = new(
    address.AddressFamily,
    SocketType.Stream,
    ProtocolType.Tcp
);

listener.Bind(endPoint);
listener.Listen(100);

byte[] buffer = new byte[512];
while (true)
{
    Socket newSocket = listener.Accept();
    int received = newSocket.Receive(buffer);
    if (received > 0 && buffer[0] == 0x16) //parse client hello
    {
        new TLS(newSocket).Parse(buffer);
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

