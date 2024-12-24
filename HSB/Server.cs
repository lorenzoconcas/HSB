using HSB.Components.WebSockets;
using HSB.Constants;
using HSB.Constants.TLS;
using HSB.DefaultPages;
using HSB.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace HSB;

public class Server
{
    private readonly IPAddress ipAddress;
    private readonly IPEndPoint localEndPoint;
    private readonly IPEndPoint? sslLocalEndPoint;
    private readonly Configuration config;
    private readonly Socket listener;
    private readonly Socket? sslListener;
    private readonly X509Certificate2? sslCertificate = null;

    public static void Main()
    {
        Terminal.INFO("HSB-# has wrongfully been compiled has executable and will not run!");
        Terminal.INFO("To run as standalone you must compile/execute the \"Standalone\" or the \"Launcher\" project");
        Terminal.INFO("Check the documentation for more info (\"https://github.com/lorenzoconcas/HSB-Sharp\")");
    }

    public Server(Configuration config)
    {
        var errorCode = 0;
        if (!config.HideBranding)
            Utils.PrintLogo();

        if (config.Port == 0)
        {
            //if port is 0, we use a random port in the range 1024-65535
            config.Port = (ushort) new Random().Next(1024, 65535);
        }

        this.config = config;
        config.Debug.INFO("Starting logging...");

        try
        {
            if (config.Address == "")
            {
                //address must be ANY
                ipAddress = config.ListeningMode switch
                {
                    IPMode.IPV4_ONLY => IPAddress.Any,
                    _ => IPAddress.IPv6Any,
                };
            }
            else //note that in this case ListeningMode is NOT a valid parameter as it depends on the address
            {
                List<IPAddress> addresses = [.. Dns.GetHostAddresses(config.Address, AddressFamily.InterNetwork)];

                //this fixes an error where user specifies an ipv4 address but want the server to listenes to BOTH or ipv6 only

                if (addresses.Count != 0)
                {
                    ipAddress = addresses.First();
                    config.ListeningMode = IPMode.IPV4_ONLY;
                }
                else
                {
                    addresses = [.. Dns.GetHostAddresses(config.Address, AddressFamily.InterNetworkV6)];
                    if (addresses.Count != 0)
                    {
                        ipAddress = addresses.First();
                        config.ListeningMode = IPMode.IPV6_ONLY;
                    }
                    else
                    {
                        errorCode = (int) SERVER_ERRORS.ADDRESS_NOT_FOUND;
                        throw new Exception("Cannot found address to listen to");
                    }
                }
            }

            //initialize the endpoints
            localEndPoint = new(ipAddress, config.Port);
            listener = new(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            SslConfiguration sslConf = config.SslSettings;


            //if ssl is set and configuration is set to use two ports we start the sslListener
            if ((sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) && sslConf.PortMode == SSL_PORT_MODE.DUAL_PORT)
            {
                sslLocalEndPoint = new(ipAddress, config.SslSettings.SslPort);
                if (sslLocalEndPoint == null)
                {
                    errorCode = (int) SERVER_ERRORS.CANNOT_CREATE_SSL_ENDPOINT;
                    throw new Exception("Cannot create SSL endpoint");
                }

                sslListener = new(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);
                if (sslListener == null)
                {
                    errorCode = (int) SERVER_ERRORS.CANNOT_CREATE_SSL_LISTENER;
                    throw new Exception("Cannot create SSL listener");
                }

                if (sslConf.UseDebugCertificate)
                {
                    config.Debug.INFO("Server is set to use a debug certificate");
                    sslCertificate = SslConfiguration.TryLoadDebugCertificate(c: config);
                    if (sslCertificate == null)
                    {
                        errorCode = (int) SERVER_ERRORS.CANNOT_LOAD_DEBUG_CERTIFICATE;
                        throw new Exception(
                            "Cannot load debug certificate, server cannot start with this configuration! Make sure openssl is installed");
                    }
                }
                else if (sslConf.IsEnabled())
                    sslCertificate = sslConf.GetCertificate();
            }

            if (config.ListeningMode == IPMode.ANY)
            {
                listener.DualMode = true;
                if (config.SslSettings.IsEnabled())
                    sslListener!.DualMode = true;
            }


            if (sslConf.IsEnabled() || sslConf.IsDebugModeEnabled())
            {
                config.Debug.INFO("Server is running in SSL mode");
            }

            var prefix = "http";
            if ((sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) && sslConf.PortMode == SSL_PORT_MODE.DUAL_PORT)
            {
                if (config.PublicUrl == "")
                    config.Debug.INFO($"Listening at https://{sslLocalEndPoint}/");
                else config.Debug.INFO($"Listening at https://{config.PublicUrl}:{sslConf.SslPort}/");
            }

            else if ((sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) &&
                     sslConf.PortMode == SSL_PORT_MODE.SINGLE_PORT)
                prefix += "s";

            if (config.PublicUrl == "")
                config.Debug.INFO($"Listening at {prefix}://{localEndPoint}/");
            else config.Debug.INFO($"Listening at {prefix}://{config.PublicUrl}:{config.Port}/");

            config.Debug.INFO("Server started");
        }
        catch (Exception e)
        {
            config.Debug.ERROR("An exception occurred while initializing the server ->\n" + e);
            config.Debug.INFO("Server will now exit...");
            Environment.Exit(-errorCode);
        }
        //end of the server initialization
    }

    public void Start(bool openInBrowser = false)
    {
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(100);

            var sslConf = config.SslSettings;

            if (sslConf.IsEnabled() || sslConf.IsDebugModeEnabled())
            {
                //sslListener and sslLocalEndPoint are not null because we checked in the constructor
                sslListener!.Bind(sslLocalEndPoint!);
                sslListener.Listen(100);
            }

            OpenInBrowserIfSet(openInBrowser, sslConf.IsEnabled(),
                sslConf.PortMode == SSL_PORT_MODE.DUAL_PORT ? sslLocalEndPoint! : localEndPoint);

            //this makes the second port listen to SSL requests
            if ((sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) && sslConf.PortMode == SSL_PORT_MODE.DUAL_PORT)
            {
                new Task(() =>
                {
                    while (true)
                    {
                        Step(sslListener!, true);
                    }
                }).Start();
            }

            //since the base port is always listening this is always executed
            while (true)
            {
                //if ssl is enabled and single port is used
                var sslMode = (sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) &&
                              sslConf.PortMode == SSL_PORT_MODE.SINGLE_PORT;

                Step(listener, sslMode);
            }
        }
        catch (Exception e)
        {
            Terminal.ERROR(e);
        }
    }

    private static void OpenInBrowserIfSet(bool openInBrowser, bool ssl, IPEndPoint endpoint)
    {
        if (openInBrowser)
        {
            var psi = new ProcessStartInfo
            {
                FileName = $"http{(ssl ? "s" : "")}:{endpoint}",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }


    private void Step(Socket listener, bool sslMode)
    {
        Socket socket = listener.Accept();
        byte[] bytes = new byte[config.RequestMaxSize];
        int bytesRec = 0;
        SslStream? sslStream = null;
        bool sslOK = false;
        if (sslMode)
        {
            var netstream = new NetworkStream(socket);
            sslStream = new(netstream, true);
            try
            {
                sslStream.AuthenticateAsServer(
                    sslCertificate!,
                    config.SslSettings.ClientCertificateRequired,
                    config.SslSettings.GetProtocols(),
                    config.SslSettings.CheckCertificateRevocation
                );
                bytesRec = sslStream.Read(bytes);
                sslOK = true;
            }
            catch (Exception)
            {
                //if auth fails, the behaviour depends on the configuration
                sslStream.Dispose();
                if (config.SslSettings.UpgradeUnsecureRequests)
                {
                    config.Debug.WARNING("SSL authentication failed, redirecting to SSL");
                    //attempt redirect
                    Request _req = new(bytes, socket, config, false);
                    Response res = new(socket, _req, config, null);

                    //the endpoint varies if by the port mode
                    //if the port mode is dual port, we redirect to the ssl port
                    IPEndPoint redirectEndpoint = config.SslSettings.PortMode == SSL_PORT_MODE.DUAL_PORT
                        ? sslLocalEndPoint!
                        : localEndPoint;
                    res.Redirect("https://" + redirectEndpoint, HTTP_CODES.MOVED_PERMANENTLY);
                    return;
                }

                else
                {
                    config.Debug.WARNING("SSL authentication failed, closing connection");
                    socket.Close();
                    return;
                }
            }
        }
        else
        {
            if (config.SslSettings.IsEnabled() && config.SslSettings.UpgradeUnsecureRequests)
            {
                config.Debug.WARNING("Unsecure request received, redirecting to SSL");
                //attempt redirect
                Request _req = new(bytes, socket, config, sslOK);
                Response res = new(socket, _req, config, null);

                //the endpoint varies if by the port mode
                //if the port mode is dual port, we redirect to the ssl port
                IPEndPoint redirectEndpoint = config.SslSettings.PortMode == SSL_PORT_MODE.DUAL_PORT
                    ? sslLocalEndPoint!
                    : localEndPoint;
                res.Redirect("https://" + redirectEndpoint, HTTP_CODES.MOVED_PERMANENTLY);
                return;
            }

            bytesRec = socket.Receive(bytes);
        }

        bytes = bytes[..bytesRec]; //trim the array to the actual size of the request

        //parse the request
        Request req = new(bytes, socket, config, sslOK);
        //if is valid we process it
        if (req.IsValidRequest)
        {
            Response res = new(socket, req, config, sslStream);
            new Task(() => ProcessRequest(req, res)).Start();
        }
        else
        {
            //abort if the request is not valid
            socket.Close();
        }
    }

    private bool RunIfExpressMapping(Request req, Response res)
    {
        var route = config.ExpressRoutes.Find(e => e.Item1 == req.URL && e.Item2.Item1 == req.METHOD);
        //regex routes are the ones that starts with /` (slash and backtick)
        var regexRoutes = config.ExpressRoutes.FindAll(e => e.Item1.StartsWith("/`") && e.Item2.Item1 == req.METHOD);

        //regex paths have priority
        if (regexRoutes.Count > 0)
        {
            var regexRoute = regexRoutes.Find(r => 
                new Regex(r.Item1[2..])
                    .Match(req.URL)
                    .Success
            );

            if (regexRoute != null)
            {
                regexRoute.Item2.Item2.DynamicInvoke(req, res);
                return true;
            }
        }


        if (route == null) return false;

        route.Item2.Item2.DynamicInvoke(req, res);
        return true;
    }

    /// <summary>
    /// Collect all the static routes from the assemblies
    /// </summary>
    /// <returns></returns>
    protected internal static Dictionary<Tuple<string, bool>, Type> CollectStaticRoutes()
    {
        Dictionary<Tuple<string, bool>, Type> routes = [];

        string[] excludeList = ["System", "Microsoft", "Internal"];

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !excludeList.Any(e => a.FullName!.StartsWith(e)));

        var classes = assemblies.SelectMany(assembly => assembly.GetTypes(), (assembly, type) => new {assembly, type})
            .Where(@t => @t.type.IsClass)
            .Select(@t => @t.type);


        foreach (var c in classes)
        {
            var multipleBinded = c.GetCustomAttributes<Binding>(false);

            var enumerable = multipleBinded as Binding[] ?? multipleBinded.ToArray();
            if (enumerable.Any())
            {
                foreach (var b in enumerable)
                {
                    if (b.Path != "")
                    {
                        routes.Add(new Tuple<string, bool>(b.Path, b.StartsWith), c);
                    }
                    else if (b.Auto)
                    {
                        routes.Add(new Tuple<string, bool>("/" + c.Name.ToLower(), false), c);
                    }
                }
            }
            else
            {
                var attr = c.GetCustomAttribute<Binding>(false);
                if (attr == null) continue;
                if (attr.Path != "")
                {
                    routes.Add(new Tuple<string, bool>(attr.Path, attr.StartsWith), c);
                }
                else if (attr.Auto)
                {
                    routes.Add(new Tuple<string, bool>(c.Name.ToLower(), false), c);
                }
            }
        }


        return routes;
    }

    /*protected internal static string GetStaticRoutesInfo()
    {
        string str = "";
        var staticRoutes = CollectStaticRoutes();

        if (staticRoutes.Count != 0)
        {
            str += "\nStatic routes:";
            staticRoutes.ToList().ForEach(m => str += $"\nPath : {m.Key.Item1} -> {m.Value.Name}");
        }

        return str;
    }*/

    private object? GetInstance(Request req, Response res)
    {
        Dictionary<Tuple<string, bool>, Type> routes = CollectStaticRoutes();

        //fetch routes with regexes
        var regexRoutes = routes.Keys.Where(e => e.Item1.StartsWith("/`")).ToList();
        if (regexRoutes.Count > 0)
        {
            var regexRoute = regexRoutes.Find(r => 
                new Regex(r.Item1[2..])
                    .Match(req.URL)
                    .Success
                );


            /*var regexRoute = regexRoutes.Find(r =>
            {
                var regexString = r.Item1[2..];
                //check if regex matches and return if true
                var regex = new Regex(regexString);
                var success = regex.Match(req.URL).Success;
                return success;
            });*/

            if (regexRoute != null)
            {
                var c = routes[regexRoute];
                var x = c.GetConstructors()[0];
                return x.GetParameters().Length switch
                {
                    3 => Activator.CreateInstance(c, req, res, config),
                    2 => Activator.CreateInstance(c, req, res),
                    _ => throw new Exception($"Invalid constructor found {x.Name}"),
                };
            }
        }


        if (routes.ContainsKey(new Tuple<string, bool>(req.URL, false)))
        {
            var c = routes[new(req.URL, false)];
            var x = c.GetConstructors()[0];
            return x.GetParameters().Length switch
            {
                3 => Activator.CreateInstance(c, req, res, config),
                2 => Activator.CreateInstance(c, req, res),
                _ => throw new Exception($"Invalid constructor found {x.Name}"),
            };
        }

        //we omit the else branch 
        //we check if there is a path that starts like the request url
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
        var blockMode = config.BlockMode;
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
                config.Debug.WARNING($"{req.METHOD} '{req.URL}' {HTTP_CODES.NOT_FOUND} (Invalid Request)", true);
                new Error(req, res, config, "Invalid Request", HTTP_CODES.NOT_FOUND).Process();
                return;
            }


            //check if there is a filter that blocks the request
            if (Filter(req)) return;

            //check if server is launched with --listFiles
            if (config.GetRawArguments().Contains("--listFiles"))
            {
                if (CheckSafePath(req.URL, req, res)) return;

                new FileList(req, res, config).Process();
                return;
            }


            //if global CORS are set in configuration, check if the request is allowed
            if (config.GlobalCORS != null)
            {
                if (!config.GlobalCORS.IsRequestAllowed(req))
                {
                    config.Debug.WARNING($"{req.METHOD} '{req.URL}' {HTTP_CODES.FORBIDDEN} (CORS not allowed)", true);
                    new Error(req, res, config, "CORS not allowed", HTTP_CODES.FORBIDDEN).Process();
                    return;
                }
            }

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
                        config.Debug.WARNING(
                            $"{req.METHOD} '{req.URL}' {HTTP_CODES.METHOD_NOT_ALLOWED} (Invalid Request)", true);
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
                    if (File.Exists(config.StaticFolderPath + "/index.html"))
                    {
                        config.Debug.INFO($"{req.METHOD} '{req.URL}' 200");
                        res.SendHTMLFile(config.StaticFolderPath + "/index.html");
                    }
                    else
                    {
                        config.Debug.INFO($"{req.METHOD} '{req.URL}' 200 (Default Index Page)");
                        new Index(req, res, config).Process();
                    }
                }
                else if (config.DocumentationPath != "" && config.DocumentationPath == req.URL)
                {
                    config.Debug.INFO($"{req.METHOD} '{req.URL}' 200 (Documentation Page)");
                    new Documentation(req, res, config).Process();
                }
                else
                {
                    //we check if the client is requesting a resource, else 404 not found
                    //to check if the path is safe we use the same regex used in send.js
                    //see: https://github.com/pillarjs/send/blob/master/index.js#L63
                    if (CheckSafePath(req.URL, req, res))
                    {
                        return;
                    }

                    //if the path is safe, the static folder is set and the file exists, we send it
                    if (config.StaticFolderPath != "" && File.Exists(config.StaticFolderPath + "/" + req.URL))
                    {
                        //config.debug.INFO($"Static file found, serving '{req.URL}'");
                        config.Debug.INFO($"{req.METHOD} '{req.URL}' 200 (Static file)");
                        res.SendFile(config.StaticFolderPath + "/" + req.URL);
                    }
                    else if (config.ServeEmbeddedResource &&
                             Utils.IsEmbeddedResource(req.URL, config.EmbeddedResourcePrefix))
                    {
                        config.Debug.INFO($"{req.METHOD} '{req.URL}' 200 (Embedded resource)");
                        object resource = Utils.LoadResource<object>(req.URL, config.EmbeddedResourcePrefix);
                        res.SendObject(resource, req.URL);
                    }
                    else
                    {
                        //if no servlet or static file found, send 404
                        config.Debug.INFO($"{req.METHOD} '{req.URL}' 404 (Resource not found)");
                        new Error(req, res, config, "Page not found", HTTP_CODES.NOT_FOUND).Process();
                    }
                }
            }
        }
        catch (Exception e)
        {
            //config.debug.ERROR("Error handling request ->\n " + e);
            config.Debug.ERROR($"{req.METHOD} '{req.URL}' 500 (Internal Server Error)\n{e}");
            //we show an error page with the message and code 500
            new Error(req, res, config, e.ToString(), HTTP_CODES.INTERNAL_SERVER_ERROR).Process();
        }
    }

    private bool CheckSafePath(string path, Request req, Response res)
    {
        if (Utils.IsUnsafePath(path))
        {
            config.Debug.WARNING($"{req.METHOD} '{req.URL}' 200 (Requested unsafe path, ignoring request)");
            new Error(req, res, config, "", HTTP_CODES.NOT_FOUND).Process();
            if (config.IPAutoblock)
            {
                config.Debug.WARNING($"Autoblocking IP {req.ClientIP}");
                if (File.Exists("./banned_ips.txt"))
                    File.AppendAllText("./banned_ips.txt", req.ClientIP + "\n");
                else
                    File.WriteAllText("./banned_ips.txt", req.ClientIP + "\n");
            }

            return true;
        }

        return false;
    }
}