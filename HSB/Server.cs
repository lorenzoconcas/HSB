using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace HSB
{
    public class Server
    {
        public static void Main(string[] args)
        {
            Terminal.INFO("HSB-# has wrongfully been compiled has executable and will not run!");
            Terminal.INFO("To run as standalone you must compile/execute the \"Standalone\" project");
        }
        public Server(Configuration config)
        {
            /* if (!CheckIfRequiredDLLAreLoaded())
             {
                 Terminal.ERROR("Error some library are missing, install them via nuget:\nMimeTypeMapOfficial");
                 return;
             }*/
            Utils.PrintLogo();

            if (config.port > 65535)
                throw new ArgumentOutOfRangeException("port");

            Terminal.INFO($"Listening at address http://{config.address}:{config.port}/");

            //buffer per dati in ingresso


            //ricerca indirizzo ip 
            var addresses = Dns.GetHostAddresses(config.address, AddressFamily.InterNetwork);
            IPAddress ipAddress = addresses[0];
            IPEndPoint localEndPoint = new(ipAddress, config.port);

            Socket listener = new(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);


            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                while (true)
                {
                    //attendiamo una connessione
                    Socket socket = listener.Accept();
                    new Task(() =>
                    {
                        byte[] bytes = new byte[1024];
                        int bytesRec = socket.Receive(bytes);
                        Request req = new(bytes, socket);
                        Response res = new(socket, req);
                        config.Process(req, res);

                    }).Start();

                }

            }
            catch (Exception e)
            {
                Terminal.ERROR(e);
            }
        }

        private bool CheckIfRequiredDLLAreLoaded()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            List<Assembly> assems = currentDomain.GetAssemblies().ToList();
            //required dlls: MimeTypeMapOfficial
            bool ok = false;
            foreach (var a in assems)
            {
                if (a.FullName!.Contains("MimeTypes"))
                    ok = true;
            }
            return ok;
        }
    }


}
