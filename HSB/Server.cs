using HSB.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace HSB
{
    public class Server
    {


        private readonly IPAddress ipAddress;
        private readonly IPEndPoint localEndPoint;
        private readonly Configuration config;
        private readonly Socket listener;


        public static void Main()
        {
            Terminal.INFO("HSB-# has wrongfully been compiled has executable and will not run!");
            Terminal.INFO("To run as standalone you must compile/execute the \"Standalone\" or the \"Launcher\" project");
            Terminal.INFO("Check the documentation for more info (\"https://github.com/lorenzoconcas/HSB-Sharp\")");
        }
        public Server(Configuration config)
        {
            Utils.PrintLogo();

            if (config.port > 65535)
                throw new InvalidConfigurationParameterException("Port", "Port number is over the maximum allowed (65535)");

            this.config = config;
            //config.UseIPv4Only = true;
            if (config.address == "")
            {
                //address must be ANY
                ipAddress = config.UseIPv4Only ? IPAddress.Any : IPAddress.IPv6Any;
            }
            else
            {
                List<IPAddress> addresses = Dns.GetHostAddresses(config.address, AddressFamily.InterNetwork).ToList();


                addresses.Clear();
                addresses.AddRange(Dns.GetHostAddresses(config.address, AddressFamily.InterNetworkV6).ToList());

                ipAddress = addresses.First();
            }

            localEndPoint = new(ipAddress, config.port);

            listener = new(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            listener.DualMode = !config.UseIPv4Only;


            config.debug.INFO($"Listening at http://{localEndPoint}/");
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
                        FileName = $"http://{config.address}:{config.port}",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                while (true)
                {
                    Socket socket = listener.Accept();

                    new Task(() =>
                    {
                        //socket.DualMode = true;
                        byte[] bytes = new byte[config.requestMaxSize];
                        int bytesRec = socket.Receive(bytes);
                        Request req = new(bytes, socket, config);
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

        //for future use or to be removed
        /*
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
        */
    }


}
