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
    public class Server
    {
        public static void Main(string[] args)
        {
            Terminal.INFO("This instance of HSB-# is designed to run as library, therefore cannot run standalone");
            Terminal.INFO("To run as standalone you must compile/execute the \"Standalone\" project");
        }
        public Server(Configuration config)
        {
            Utils.PrintLogo();

            if (config.port > 65535)
                throw new ArgumentOutOfRangeException("port");

            Terminal.INFO($"Listening at address http://{config.address}:{config.port}/");

            //buffer per dati in ingresso
            byte[] bytes = new Byte[1024];

            //ricerca indirizzo ip 
            var addresses = Dns.GetHostAddresses(config.address, AddressFamily.InterNetwork);
            IPAddress ipAddress = addresses[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, config.port);

            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);


            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    //attendiamo una connessione
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
