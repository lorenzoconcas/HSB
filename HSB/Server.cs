using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using HSB.Components.WebSockets;
using HSB.Constants;
using HSB.Exceptions;

namespace HSB;

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
        if (!config.HideBranding)
            Utils.PrintLogo();

        if (config.port > 65535)
            throw new InvalidConfigurationParameterException("Port", "Port number is over the maximum allowed (65535)");

        this.config = config;

        if (config.address == "")
        {
            //address must be ANY
            ipAddress = config.ListeningMode switch
            {
                IPMode.IPV4_ONLY => IPAddress.Any,
                _ => IPAddress.IPv6Any,
            };
        }
        else
        {
            List<IPAddress> addresses = Dns.GetHostAddresses(config.address, AddressFamily.InterNetwork).ToList();

            if (config.ListeningMode != IPMode.IPV4_ONLY)
            {
                addresses.AddRange(Dns.GetHostAddresses(config.address, AddressFamily.InterNetworkV6).ToList());
            }

            if (addresses.Any())
                ipAddress = addresses.First();
            else
                throw new Exception("Cannot found address to listen to");
        }

        localEndPoint = new(ipAddress, config.port);

        listener = new(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        if (config.ListeningMode == IPMode.ANY)
        {
            listener.DualMode = true;
        }

        config.debug.INFO("Starting logging...");
        if (config.ListeningMode == IPMode.IPV4_ONLY && ipAddress == IPAddress.Any)
        {
            config.debug.INFO($"Listening at http://127.0.0.1:{config.port}/");
        }
        else
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
                    FileName = $"http:{localEndPoint}",
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
                    bytes = bytes[..bytesRec]; //trim the array to the actual size of the request

                    Request req = new(bytes, socket, config);
                    if (req.proceedWithElaboration)
                    {
                        Response res = new(socket, req, config);
                        new Task(() => ProcessRequest(req, res)).Start();
                    }

                }).Start();
            }
        }
        catch (Exception e)
        {
            Terminal.ERROR(e);
        }
    }
    private bool RunIfExpressMapping(Request req, Response res)
    {
        var e = config.ExpressRoutes.Find(e => e.Item1 == req.URL && e.Item2.Item1 == req.METHOD);

        if (e == null) return false;

        e.Item2.Item2.DynamicInvoke(req, res);
        return true;
    }

    /// <summary>
    /// Collect all the static routes from the assemblies
    /// </summary>
    /// <returns></returns>
    protected internal static Dictionary<Tuple<string, bool>, Type> CollectStaticRoutes()
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assemblies = currentDomain.GetAssemblies().ToList();

        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));

        Dictionary<Tuple<string, bool>, Type> routes = new();


        foreach (var assem in assemblies)
        {
            List<Type> classes = assem.GetTypes().ToList();

            foreach (var c in classes)
            {
                try
                {
                    IEnumerable<Binding> multiBindings = c.GetCustomAttributes<Binding>(false);

                    //if no class have more than one binding, we search the ones with only one
                    if (multiBindings.Any())
                    {
                        foreach (Binding b in multiBindings)
                        {
                            if (b.Path != "")
                                routes.Add(new(b.Path, b.StartsWith), c);
                        }
                    }
                    else
                    {
                        Binding? attr = c.GetCustomAttribute<Binding>(false);
                        if (attr != null && attr.Path != "")
                            routes.Add(new(attr.Path, attr.StartsWith), c);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        return routes;
    }

    protected internal string GetStaticRoutesInfo()
    {
        string str = "";
        var staticRoutes = CollectStaticRoutes();

        if (staticRoutes.Any())
        {
            str += "\nStatic routes:";
            staticRoutes.ToList().ForEach(m => str += $"\nPath : {m.Key.Item1} -> {m.Value.Name}");
        }
        return str;
    }

    private object? GetInstance(Request req, Response res)
    {
        Dictionary<Tuple<string, bool>, Type> routes = CollectStaticRoutes();


        if (routes.ContainsKey(new(req.URL, false)))
        {
            Type c = routes[new(req.URL, false)];
            var x = c.GetConstructors()[0];
            return x.GetParameters().Length switch
            {
                3 => Activator.CreateInstance(c, req, res, config),
                2 => Activator.CreateInstance(c, req, res),
                _ => throw new Exception($"Invalid constructor found {x.Name}"),
            };
        }

        //we omit the else branch 
        //we check if there is a a path that starts like the request url
        return (from r in routes
                let path = r.Key.Item1
                where req.URL.StartsWith(path)
                select r.Value
            into c
                let x = c.GetConstructors()[0]
                select x.GetParameters().Length switch
                {
                    3 => Activator.CreateInstance(c, req, res, config),
                    2 => Activator.CreateInstance(c, req, res),
                    _ => throw new Exception($"Invalid constructor found {x.Name}"),
                }).FirstOrDefault();
    }
    /// <summary>
    /// Check if the request is blocked by a blocking rule
    /// </summary>
    /// <param name="req"></param>
    /// <returns>True if request is blocked</returns>
    private bool Filter(Request req)
    {
        var blockMode = config.blockMode;
        switch (blockMode)
        {
            case BLOCK_MODE.NONE: return false; //no blocking
            case BLOCK_MODE.BANLIST:
                if (config.PermanentIPList.Any(ip => ip == req.ClientIP))
                {
                    return true; //blocked request
                }
                if (File.Exists("./banned_ips.txt"))
                {
                    var bannedIps = File.ReadAllLines("./banned_ips.txt");
                    if (bannedIps.Contains(req.ClientIP))
                    {
                        return true; //blocked request
                    }
                }
                return true;
            case BLOCK_MODE.OKLIST:
                if (config.PermanentIPList.Any(ip => ip == req.ClientIP))
                {
                    return false; //allowed request
                }

                if (File.Exists("./allowed_ips.txt"))
                {
                    var allowedIps = File.ReadAllLines("./allowed_ips.txt");
                    return !allowedIps.Contains(req.ClientIP);
                }

                return false; //no allowed_ips.txt file found, so we allow all requests
        }
        return false;
    }
    private void ProcessRequest(Request req, Response res)
    {
        try
        {
            //check if request is valid                    
            if (!req.validRequest)
            {
                config.debug.WARNING($"{req.METHOD} '{req.URL}' {HTTP_CODES.NOT_FOUND} (Invalid Request)", true);
                new Error(req, res, config, "Invalid Request", HTTP_CODES.NOT_FOUND).Process();
                return;
            }
            //check if there is a filter that blocks the request
            if (Filter(req)) return;

            //if dev has used the express mapping, we run the mapped function
            if (RunIfExpressMapping(req, res)) return;

            //We check if the route requested is handled by any servlet
            object? o = GetInstance(req, res);

            if (o != null)
            {
                //we check if the object is a servlet or a websocket
                if (o is Servlet s)
                {
                    s.Process();
                }
                else if (o is WebSocket ws)
                {
                    if (!req.IsWebSocket())
                    {
                        config.debug.WARNING($"{req.METHOD} '{req.URL}' {HTTP_CODES.METHOD_NOT_ALLOWED} (Invalid Request)", true);
                        new Error(req, res, config, "Invalid Request", HTTP_CODES.METHOD_NOT_ALLOWED).Process();
                        return;
                    }
                    else
                    {

                        ws.Process();
                        return;
                    }
                }
                else throw new Exception($"Developer tried to map an invalid object to a route -> {o.GetType()}");
            }
            else
            {
                //the client searched for a route that is not mapped by any servlet
                //so we do some other checks like root page or static resource
                //if no root page is set we search for and index.html file, else we show the default home page
                if (req.URL == "/")
                {
                    //if the client is requesting the root file, we check if there is an index.html file
                    //if not, we use the default servlet
                    if (File.Exists(config.staticFolderPath + "/index.html"))
                    {
                        res.SendHTMLFile(config.staticFolderPath + "/index.html");
                        config.debug.INFO($"{req.METHOD} '{req.URL}' 200");
                    }
                    else
                    {
                        new Index(req, res, config).Process();
                        config.debug.INFO($"{req.METHOD} '{req.URL}' 200 (Default Index Page)");
                    }
                }
                else
                {
                    //we check if the client is requesting a resource, else 404 not found
                    //to check if the path is safe we use the same regex used in send.js
                    //see: https://github.com/pillarjs/send/blob/master/index.js#L63
                    if (Utils.IsUnsafePath(req.URL))
                    {
                        config.debug.WARNING($"{req.METHOD} '{req.URL}' 200 (Requested unsafe path, ignoring request)");
                        new Error(req, res, config, "", HTTP_CODES.NOT_FOUND).Process();
                        if (config.IPAutoblock)
                        {
                            config.debug.WARNING($"Autoblocking IP {req.ClientIP}");
                            if (File.Exists("./banned_ips.txt"))
                                File.AppendAllText("./banned_ips.txt", req.ClientIP + "\n");
                            else
                                File.WriteAllText("./banned_ips.txt", req.ClientIP + "\n");
                        }
                    }
                    //if the path is safe and the file exists, we send it
                    if (File.Exists(config.staticFolderPath + "/" + req.URL))
                    {
                        //config.debug.INFO($"Static file found, serving '{req.URL}'");
                        config.debug.INFO($"{req.METHOD} '{req.URL}' 200 (Static file)");
                        res.SendFile(config.staticFolderPath + "/" + req.URL);
                    }
                    else
                    {
                        //if no servlet or static file found, send 404
                        config.debug.INFO($"{req.METHOD} '{req.URL}' 404 (Resource not found)");
                        new Error(req, res, config, "Page not found", HTTP_CODES.NOT_FOUND).Process();
                    }
                }
            }
        }
        catch (Exception e)
        {
            //config.debug.ERROR("Error handling request ->\n " + e);
            config.debug.ERROR($"{req.METHOD} '{req.URL}' 500 (Internal Server Error)\n{e}");
            //we show an error page with the message and code 500
            new Error(req, res, config, e.ToString(), HTTP_CODES.INTERNAL_SERVER_ERROR).Process();
        }
    }
}
