using HSB.Components.WebSockets;
using HSB.Constants;
using HSB.Constants.TLS;
using HSB.DefaultPages;
using HSB.Exceptions;
using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using HSB.Constants.TLS.Manual;

namespace HSB;

public class Server
{
    private IPAddress? _ipAddress;
    private IPEndPoint? _localEndPoint;
    private IPEndPoint? _sslLocalEndPoint;
    private readonly Configuration _config;
    private Socket _listener;
    private Socket? _sslListener;
    private TlsConnection? _tlsConnection;
    private X509Certificate2? _serverCertificate;

    public static void Main()
    {
        Terminal.INFO("HSB-# has wrongfully been compiled has executable and will not run!");
        Terminal.INFO("To run as standalone you must compile/execute the \"Standalone\" or the \"Launcher\" project");
        Terminal.INFO("Check the documentation for more info (\"https://github.com/lorenzoconcas/HSB\")");
    }

    /// <summary>
    /// Calculates the IP Address to listen to based on configuration
    /// </summary>
    private void SetIpAddress()
    {
        if (_config.Address == "")
        {
            _ipAddress = _config.ListeningMode switch
            {
                IPMode.IPV4_ONLY => IPAddress.Any,
                _ => IPAddress.IPv6Any,
            };
            return;
        }


        List<IPAddress> addresses = [.. Dns.GetHostAddresses(_config.Address, AddressFamily.InterNetwork)];

        //this fixes an error where user specifies an ipv4 address but want the server to listenes to BOTH or ipv6 only

        if (addresses.Count != 0)
        {
            _ipAddress = addresses.First();
            _config.ListeningMode = IPMode.IPV4_ONLY;
        }
        else
        {
            addresses = [.. Dns.GetHostAddresses(_config.Address, AddressFamily.InterNetworkV6)];
            if (addresses.Count != 0)
            {
                _ipAddress = addresses.First();
                _config.ListeningMode = IPMode.IPV6_ONLY;
            }
            else
            {
                _config.Debug.ERROR("Cannot determine address to listen to");
                Environment.Exit((int) ServerErrors.ADDRESS_NOT_FOUND);
            }
        }
    }


    /// <summary>
    /// Initializes the endpoints and listeners based on configuration
    /// </summary>
    private void SetEndpoint()
    {
        _localEndPoint = new IPEndPoint(_ipAddress!, _config.Port);
        _listener = new Socket(_ipAddress!.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);


        if (_config.ListeningMode != IPMode.ANY) return;
        _listener.DualMode = true;
        if (_config.SslSettings.IsEnabled() && _sslListener != null)
            _sslListener!.DualMode = true;
    }

    /// <summary>
    /// If configuration has SSL enabled, initializes the SSL listener and endpoint
    /// </summary>
    private void SetSsl()
    {
        var sslConf = _config.SslSettings;


        //if ssl is set and configuration is set to use two ports we start the sslListener
        if ((!sslConf.IsEnabled() && !sslConf.IsDebugModeEnabled())) return;
        X509Certificate2? cert = null;
        if (sslConf.UseDebugCertificate)
        {
            _config.Debug.INFO("Server is set to use a debug certificate");
            cert = SslConfiguration.TryLoadDebugCertificate(c: _config);
            if (cert == null)
            {
                _config.Debug.ERROR("Cannot load debug certificate, server cannot start with this configuration!");
                Environment.Exit((int) ServerErrors.CANNOT_LOAD_DEBUG_CERTIFICATE);
            }
        }
        else if (sslConf.IsEnabled())
            cert = sslConf.GetCertificate();

        _serverCertificate = cert;

        if (cert != null)
        {
            _tlsConnection = new TlsConnection(
                cert,
                sslConf.GetProtocols(),
                sslConf.CheckCertificateRevocation,
                sslConf.ClientCertificateRequired
            );
        }


        if (sslConf.PortMode != SSL_PORT_MODE.DUAL_PORT) return;
        _sslLocalEndPoint = new(_ipAddress!, _config.SslSettings.SslPort);
        if (_sslLocalEndPoint == null)
        {
            _config.Debug.ERROR("Cannot create SSL endpoint");
            Environment.Exit((int) ServerErrors.CANNOT_CREATE_SSL_ENDPOINT);
        }

        _sslListener = new(_ipAddress!.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        if (_sslListener != null) return;
        _config.Debug.ERROR("Cannot create SSL listener");
        Environment.Exit((int) ServerErrors.CANNOT_CREATE_SSL_LISTENER);
    }

    private void PrintFinalInfo()
    {
        if (_config.SslSettings.IsEnabled() || _config.SslSettings.IsDebugModeEnabled())
        {
            _config.Debug.INFO("Server is running in SSL mode");
        }


        var prefix = "http";
        if ((_config.SslSettings.IsEnabled() || _config.SslSettings.IsDebugModeEnabled()) &&
            _config.SslSettings.PortMode == SSL_PORT_MODE.DUAL_PORT)
        {
            _config.Debug.INFO(_config.PublicUrl == ""
                ? $"Listening at https://{_sslLocalEndPoint}/"
                : $"Listening at https://{_config.PublicUrl}:{_config.SslSettings.SslPort}/");
        }

        else if ((_config.SslSettings.IsEnabled() || _config.SslSettings.IsDebugModeEnabled()) &&
                 _config.SslSettings.PortMode == SSL_PORT_MODE.SINGLE_PORT)
            prefix += "s";

        _config.Debug.INFO(_config.PublicUrl == ""
            ? $"Listening at {prefix}://{_localEndPoint}/"
            : $"Listening at {prefix}://{_config.PublicUrl}:{_config.Port}/");

        _config.Debug.INFO("Server started");
    }

    public Server(Configuration? config = null)
    {
        _ipAddress = IPAddress.Any;
        _listener = null!;
        _sslListener = null;
        _tlsConnection = null;
        _serverCertificate = null;

        config ??= new Configuration();

        if (!config.HideBranding)
            Utils.PrintLogo();

        if (config.Port == 0)
        {
            //if port is 0, we use a random port in the range 1024-65535
            config.Port = (ushort) new Random().Next(1024, 65535);
        }

        this._config = config;
        config.Debug.INFO("Starting logging...");


        SetIpAddress();
        SetEndpoint();
        SetSsl();
        PrintFinalInfo();

        //end of the server initialization
    }

    public void Start(bool openInBrowser = false)
    {
        if (_localEndPoint == null)
        {
            _config.Debug.ERROR("An error occurred while initializing the server (local endpoint is null)");
            Environment.Exit((int) ServerErrors.CANNOT_CREATE_LOCAL_ENDPOINT);
            return;
        }

        try
        {
            _listener.Bind(_localEndPoint);
            _listener.Listen(_config.MaxConnections);

            var sslConf = _config.SslSettings;

            if (sslConf.IsEnabled() || sslConf.IsDebugModeEnabled())
            {
                //sslListener and sslLocalEndPoint are not null because we checked in the constructor
                if (_sslListener != null)
                {
                    _sslListener!.Bind(_sslLocalEndPoint!);
                    _sslListener.Listen(100);
                }
            }

            OpenInBrowserIfSet(openInBrowser, sslConf.IsEnabled(),
                sslConf.PortMode == SSL_PORT_MODE.DUAL_PORT ? _sslLocalEndPoint! : _localEndPoint);

            //this makes the second port listen to SSL requests
            if ((sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) && sslConf.PortMode == SSL_PORT_MODE.DUAL_PORT)
            {
                new Task(() =>
                {
                    while (true)
                        Process(_sslListener!, true);
                }).Start();
            }

            //since the base port is always listening this is always executed
            while (true)
            {
                //if ssl is enabled and single port is used
                var sslMode = (sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) &&
                              sslConf.PortMode == SSL_PORT_MODE.SINGLE_PORT;

                Process(_listener, sslMode);
            }
        }
        catch (Exception e)
        {
            _config.Debug.ERROR(e);
        }
    }

    private static void OpenInBrowserIfSet(bool openInBrowser, bool ssl, IPEndPoint endpoint)
    {
        if (!openInBrowser) return;
        var psi = new ProcessStartInfo
        {
            FileName = $"http{(ssl ? "s" : "")}:{endpoint}",
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }


    private void Process(Socket listener, bool sslMode)
    {
        var socket = listener.Accept();
        var bytes = new byte[_config.RequestMaxSize];
        var bytesRec = 0;
        var sslOk = false;

        SslStream? sslStream = null;
        Tls12Handler? hsbTls = null;

        if (sslMode)
        {
            if (_config.SslSettings.SslHandler == SslHandler.HSB)
            {
                // Manual TLS Implementation (POC)
                try
                {

                    if (_serverCertificate == null) throw new Exception("Server certificate is null");

                    hsbTls = new Tls12Handler(socket, _serverCertificate);
                    hsbTls.PerformHandshake();
                    
                    bytesRec = hsbTls.Read(bytes, 0, bytes.Length);
                    sslOk = true;
                }
                catch (Exception e)
                {
                    sslOk = false;
                    _config.Debug.ERROR($"Manual TLS Handshake/Read Failed: {e.Message}");
                    socket.Close();
                    return;
                }
            }
            else
            {
                if (_tlsConnection == null)
                {
                    _config.Debug.ERROR("SSL Mode requested but TlsConnection is not initialized");
                    socket.Close();
                    return;
                }

                sslStream = _tlsConnection.EstablishSsl(socket);

                if (sslStream != null)
                {
                    try
                    {
                        bytesRec = sslStream.Read(bytes);
                        sslOk = true;
                    }
                    catch (Exception e)
                    {
                        _config.Debug.DEBUG(e);
                        sslStream.Dispose();
                        // Error reading from stream after auth
                    }
                }
            }

            if (!sslOk)
            {
                //if auth fails or read fails, the behavior depends on the configuration
                sslStream?.Dispose();

                if (_config.SslSettings.UpgradeUnsecureRequests)
                {
                    _config.Debug.WARNING(
                        "SSL authentication failed or read error, redirecting (if possible) or closing");

                    Request rq = new(bytes, socket, _config, false);
                    Response res = new(socket, rq, _config, null);

                    var redirectEndpoint = _config.SslSettings.PortMode == SSL_PORT_MODE.DUAL_PORT
                        ? _sslLocalEndPoint
                        : _localEndPoint;

                    if (redirectEndpoint == null)
                    {
                        _config.Debug.WARNING(
                            "Cannot initialize redirect endpoint, closing connection");
                        socket.Close();
                        return;
                    }

                    res.Redirect("https://" + redirectEndpoint, HTTP_CODES.MOVED_PERMANENTLY);
                }
                else
                {
                    _config.Debug.WARNING("SSL authentication failed, closing connection");
                    socket.Close();
                }

                return;
            }
        }
        else
        {
            if (_config.SslSettings.IsEnabled() && _config.SslSettings.UpgradeUnsecureRequests)
            {
                _config.Debug.WARNING("Unsecure request received, redirecting to SSL");
                //attempt redirect
                Request rq = new(bytes, socket, _config, sslOk);
                Response res = new(socket, rq, _config, null);

                //the endpoint varies if by the port mode
                //if the port mode is dual port, we redirect to the ssl port
                var redirectEndpoint = _config.SslSettings.PortMode == SSL_PORT_MODE.DUAL_PORT
                    ? _sslLocalEndPoint!
                    : _localEndPoint;

                if (redirectEndpoint == null)
                {
                    _config.Debug.WARNING("Cannot set redirect endpoint");
                    return;
                }


                res.Redirect("https://" + redirectEndpoint, HTTP_CODES.MOVED_PERMANENTLY);
                return;
            }

            bytesRec = socket.Receive(bytes);
        }

        bytes = bytes[..bytesRec]; //trim the array to the actual size of the request

        //parse the request
        Request req = new(bytes, socket, _config, sslOk);
        //if is valid we process it
        if (req.IsValidRequest)
        {
            Response res = new(socket, req, _config, sslStream, hsbTls);
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
        var route = _config.ExpressRoutes.Find(e => e.Item1 == req.URL && e.Item2.Item1 == req.METHOD);
        //regex routes are the ones that starts with /` (slash and backtick)
        var regexRoutes = _config.ExpressRoutes.FindAll(e => e.Item1.StartsWith("/`") && e.Item2.Item1 == req.METHOD);

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
                    3 => Activator.CreateInstance(c, req, res, _config),
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
                3 => Activator.CreateInstance(c, req, res, _config),
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
                3 => Activator.CreateInstance(c, req, res, _config),
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
        var blockMode = _config.BlockMode;
        switch (blockMode)
        {
            case BLOCK_MODE.NONE: return false; //no blocking
            case BLOCK_MODE.BANLIST:
                if (_config.PermanentIPList.Any(ip => ip == req.ClientIP))
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
                if (_config.PermanentIPList.Any(ip => ip == req.ClientIP))
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
                _config.Debug.WARNING($"{req.METHOD} '{req.URL}' {HTTP_CODES.NOT_FOUND} (Invalid Request)", true);
                new Error(req, res, _config, "Invalid Request", HTTP_CODES.NOT_FOUND).Process();
                return;
            }


            //check if there is a filter that blocks the request
            if (Filter(req)) return;

            //check if server is launched with --listFiles
            if (_config.GetRawArguments().Contains("--listFiles"))
            {
                if (CheckSafePath(req.URL, req, res)) return;

                new FileList(req, res, _config).Process();
                return;
            }


            //if global CORS are set in configuration, check if the request is allowed
            if (_config.GlobalCORS != null)
            {
                if (!_config.GlobalCORS.IsRequestAllowed(req))
                {
                    _config.Debug.WARNING($"{req.METHOD} '{req.URL}' {HTTP_CODES.FORBIDDEN} (CORS not allowed)", true);
                    new Error(req, res, _config, "CORS not allowed", HTTP_CODES.FORBIDDEN).Process();
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
                        _config.Debug.WARNING(
                            $"{req.METHOD} '{req.URL}' {HTTP_CODES.METHOD_NOT_ALLOWED} (Invalid Request)", true);
                        new Error(req, res, _config, "Invalid Request", HTTP_CODES.METHOD_NOT_ALLOWED).Process();
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
                    if (File.Exists(_config.StaticFolderPath + "/index.html"))
                    {
                        _config.Debug.INFO($"{req.METHOD} '{req.URL}' 200");
                        res.SendHTMLFile(_config.StaticFolderPath + "/index.html");
                    }
                    else
                    {
                        _config.Debug.INFO($"{req.METHOD} '{req.URL}' 200 (Default Index Page)");
                        new Index(req, res, _config).Process();
                    }
                }
                else if (_config.DocumentationPath != "" && _config.DocumentationPath == req.URL)
                {
                    _config.Debug.INFO($"{req.METHOD} '{req.URL}' 200 (Documentation Page)");
                    new Documentation(req, res, _config).Process();
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
                    if (_config.StaticFolderPath != "" && File.Exists(_config.StaticFolderPath + "/" + req.URL))
                    {
                        //config.debug.INFO($"Static file found, serving '{req.URL}'");
                        _config.Debug.INFO($"{req.METHOD} '{req.URL}' 200 (Static file)");
                        res.SendFile(_config.StaticFolderPath + "/" + req.URL);
                    }
                    else if (_config.ServeEmbeddedResource &&
                             Utils.IsEmbeddedResource(req.URL, _config.EmbeddedResourcePrefix))
                    {
                        _config.Debug.INFO($"{req.METHOD} '{req.URL}' 200 (Embedded resource)");
                        object resource = Utils.LoadResource<object>(req.URL, _config.EmbeddedResourcePrefix);
                        res.SendObject(resource, req.URL);
                    }
                    else
                    {
                        //if no servlet or static file found, send 404
                        _config.Debug.INFO($"{req.METHOD} '{req.URL}' 404 (Resource not found)");
                        new Error(req, res, _config, "Page not found", HTTP_CODES.NOT_FOUND).Process();
                    }
                }
            }
        }
        catch (Exception e)
        {
            //config.debug.ERROR("Error handling request ->\n " + e);
            _config.Debug.ERROR($"{req.METHOD} '{req.URL}' 500 (Internal Server Error)\n{e}");
            //we show an error page with the message and code 500
            new Error(req, res, _config, e.ToString(), HTTP_CODES.INTERNAL_SERVER_ERROR).Process();
        }
    }

    private bool CheckSafePath(string path, Request req, Response res)
    {
        if (!Utils.IsUnsafePath(path)) return false;
        
        _config.Debug.WARNING($"{req.METHOD} '{req.URL}' 200 (Requested unsafe path, ignoring request)");
        new Error(req, res, _config, "", HTTP_CODES.NOT_FOUND).Process();
        
        if (!_config.IPAutoblock) return true;
        
        _config.Debug.WARNING($"Autoblocking IP {req.ClientIP}");
        
        if (File.Exists("./banned_ips.txt"))
            File.AppendAllText("./banned_ips.txt", req.ClientIP + "\n");
        else
            File.WriteAllText("./banned_ips.txt", req.ClientIP + "\n");

        return true;

    }
}