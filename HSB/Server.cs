using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace HSB
{
    public class Server
    {

        private readonly IPAddress[] addresses;
        private readonly IPAddress ipAddress;
        private readonly IPEndPoint localEndPoint;
        private readonly Configuration config;
        private readonly Socket listener;


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

            this.config = config;

            Terminal.INFO($"Listening at address http://{config.address}:{config.port}/");

            //buffer per dati in ingresso


            //ricerca indirizzo ip 
            addresses = Dns.GetHostAddresses(config.address, AddressFamily.InterNetwork);
            ipAddress = addresses[0];
            localEndPoint = new(ipAddress, config.port);

            listener = new(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start(bool openInBrowser = false)
        {

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                if (openInBrowser)
                {

                    var psi = new ProcessStartInfo
                    {
                        FileName = "http://localhost:65001",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                while (true)
                {
                    Socket socket = listener.Accept();
                    new Task(() =>
                    {
                        byte[] bytes = new byte[config.requestMaxSize];
                        int bytesRec = socket.Receive(bytes);
                        Request req = new(bytes, socket);
                        Response res = new(socket, req, config);
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
