using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Reflection;

namespace HSB
{
    public class ServerEngine
    {

        public ServerEngine(Configuration config)
        {
            Utils.PrintLogo();

            if (config.port > 65535)
                throw new ArgumentOutOfRangeException("port");

            Terminal.INFO($"Listening at address http://{config.address}:{config.port}/");

            // Data buffer for incoming data.  
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.  
            // Dns.GetHostName returns the name of the
            // host running the application.  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(config.address);
            IPAddress ipAddress = ipHostInfo.AddressList[1];

            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, config.port);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and
            // listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                // Start listening for connections.  
                while (true)
                {
                    // Program is suspended while waiting for an incoming connection.  
                    Socket handler = listener.Accept();

                    int bytesRec = handler.Receive(bytes);
                    Request r = new(bytes);
                    Response res = new(handler, r);
                    config.Process(r, res);
                    //sarebbe opportuno salvare le connessioni aperte... forse

                }

            }
            catch (Exception e)
            {
                Terminal.ERROR(e);
            }
        }

    }
}
