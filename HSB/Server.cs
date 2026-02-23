using HSB.Components.WebSockets;
using HSB.Constants;
using HSB.Constants.TLS;
using HSB.DefaultPages;
using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using HSB.Components.Attributes;
using HSB.Components.Controller;
using HSB.Constants.TLS.Manual;
using HSB.OpenApi;
using HSB.Utils;
using Index = HSB.DefaultPages.Index;

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

    //routing-related variables
    private List<Map> _routes = [];

    public static void Main()
    {
        Terminal.Info("HSB-# has wrongfully been compiled has executable and will not run!");
        Terminal.Info("To run as standalone you must compile/execute the \"Standalone\" or the \"Launcher\" project");
        Terminal.Info("Check the documentation for more info (\"https://github.com/lorenzoconcas/HSB\")");
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
                IpMode.Ipv4 => IPAddress.Any,
                _ => IPAddress.IPv6Any,
            };
            return;
        }


        List<IPAddress> addresses = [.. Dns.GetHostAddresses(_config.Address, AddressFamily.InterNetwork)];

        //this fixes an error where user specifies an ipv4 address but want the server to listen BOTH or ipv6 only

        if (addresses.Count != 0)
        {
            _ipAddress = addresses.First();
            _config.ListeningMode = IpMode.Ipv4;
        }
        else
        {
            addresses = [.. Dns.GetHostAddresses(_config.Address, AddressFamily.InterNetworkV6)];
            if (addresses.Count != 0)
            {
                _ipAddress = addresses.First();
                _config.ListeningMode = IpMode.Ipv6;
            }
            else
            {
                _config.Debug.ERROR("Cannot determine address to listen to");
                Environment.Exit((int) ServerErrors.AddressNotFound);
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


        if (_config.ListeningMode != IpMode.Any) return;
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
                Environment.Exit((int) ServerErrors.CannotLoadDebugCertificate);
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
            Environment.Exit((int) ServerErrors.CannotCreateSslEndpoint);
        }

        _sslListener = new(_ipAddress!.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        if (_sslListener != null) return;
        _config.Debug.ERROR("Cannot create SSL listener");
        Environment.Exit((int) ServerErrors.CannotCreateSslListener);
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
            CliUtils.PrintLogo();

        if (config.Port == 0)
        {
            //if port is 0, we use a random port in the range 1024-65535
            config.Port = (ushort) new Random().Next(1024, 65535);
        }

        _config = config;

        _config.ExpressRouteAdded += (r) =>
        {
            _routes.Add(new Map()
            {
                Path = r.Path,
                SubRoutes =
                [
                    new RoutableMethod()
                    {
                        Delegate = r.Delegate,
                        HttpMethod = r.HttpMethod,
                        Path = "/"
                    }
                ]
            });


            _config.Debug.INFO(
                $"Route |{Terminal.FG_TO_STRING(FgColor.Green)}{r.HttpMethod}{Terminal.RESET} -> {r.Path} (Delegate)");
        };


        config.Debug.INFO("Starting logging...");


        SetIpAddress();
        SetEndpoint();
        SetSsl();
        MapRoutes();
        PrintFinalInfo();

        //the class will automatically set according to configuration
        new OpenApiBuilder(config, _routes).Init();

        //end of the server initialization
    }

    public void Start(bool openInBrowser = false)
    {
        if (_localEndPoint == null)
        {
            _config.Debug.ERROR("An error occurred while initializing the server (local endpoint is null)");
            Environment.Exit((int) ServerErrors.CannotCreateLocalEndpoint);
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

                    Request rq = new(bytes, socket, _config);
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

                    res.Redirect("https://" + redirectEndpoint, HttpCodes.MOVED_PERMANENTLY);
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


                res.Redirect("https://" + redirectEndpoint, HttpCodes.MOVED_PERMANENTLY);
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

    /*private bool RunIfExpressMapping(Request req, Response res)
    {
        var route = _config.ExpressRoutes.Find(e => e.Item1 == req.Url && e.Item2.Item1 == req.Method);
        //regex routes are the ones that starts with /` (slash and backtick)
        var regexRoutes = _config.ExpressRoutes.FindAll(e => e.Item1.StartsWith("/`") && e.Item2.Item1 == req.Method);

        //regex paths have priority
        if (regexRoutes.Count > 0)
        {
            var regexRoute = regexRoutes.Find(r =>
                new Regex(r.Item1[2..])
                    .IsMatch(req.Url)
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
    }*/


    private object? GetInstance(Request req)
    {
        var candidateControllers = _routes.Where(map => req.Url.StartsWith(map.Path)).ToArray();

        foreach (var map in candidateControllers)
        {
            if (map.SubRoutes.Count == 0)
            {
                continue;
            }

            //slice relative path, for example if the map path is "/api" and the request url is "/api/status", the relative path will be "/status"
            var relativePath = req.Url[map.Path.Length..];
            var candidateMethods = map.SubRoutes.Where(r => r.HttpMethod == req.Method).ToList();

            //if the root is called, activate the first "/" subRoute if exists, else return 404
            if (relativePath == "")
            {
                RoutableMethod? rootRoute = candidateMethods.Find(sr => sr.Path == "/");
                if (!rootRoute.HasValue) return null;
                //inject Request and Response in the class if there are any parameter with those types, this allows to avoid having to declare them in the route method

                return (map.Class, rootRoute.Value); //activation is done in replacement of the Process() function call
            }

            foreach (var route in candidateMethods)
            {
                //get public instance fields

                if (route.Path == relativePath)
                    return (map.Class, route);

                var pattern = "^" + Regex.Replace(route.Path, @":[^/]+", @"[^/]+") + "$";

                if (!Regex.IsMatch(relativePath, pattern)) continue;
                //we extract the parameters from the url and add them to the request parameters
                var routeParts = route.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var relativeParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < routeParts.Length; i++)
                {
                    if (!routeParts[i].StartsWith(':')) continue;
                    var paramName = routeParts[i][1..];
                    var paramValue = relativeParts[i];
                    req.Parameters[paramName] = paramValue;
                }

                return (map.Class, route);
            }
        }

        return null;
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
                if (_config.PermanentIpList.Any(ip => ip == req.ClientIp))
                {
                    return true; //blocked request
                }

                if (!File.Exists("./banned_ips.txt")) return true;
                var bannedIps = File.ReadAllLines("./banned_ips.txt");
                return bannedIps.Contains(req.ClientIp) || true; //blocked request
            case BLOCK_MODE.OKLIST:
                if (_config.PermanentIpList.Any(ip => ip == req.ClientIp))
                {
                    return false; //allowed request
                }

                if (!File.Exists("./allowed_ips.txt"))
                    return false; //no allowed_ips.txt file found, so we allow all requests
                var allowedIps = File.ReadAllLines("./allowed_ips.txt");
                return !allowedIps.Contains(req.ClientIp);
            default:
                return false;
        }
    }


    private void ProcessRequest(Request req, Response res)
    {
        try
        {
            //check if request is valid                    
            if (!req.ValidRequest)
            {
                _config.Debug.WARNING($"{req.Method} '{req.Url}' {HttpCodes.NOT_FOUND} (Invalid Request)");
                new Error(res, _config, "Invalid Request", HttpCodes.NOT_FOUND).Throw();
                return;
            }

            //check if there is a filter that blocks the request
            if (Filter(req)) return;

            //check if server is launched with --listFiles
            if (_config.GetRawArguments().Contains("--listFiles"))
            {
                if (CheckSafePath(req.Url, req, res)) return;

                new FileList(req, res, _config).Get();
                return;
            }


            //if global CORS are set in configuration, check if the request is allowed
            if (_config.GlobalCors != null)
            {
                if (!_config.GlobalCors.IsRequestAllowed(req))
                {
                    _config.Debug.WARNING($"{req.Method} '{req.Url}' {HttpCodes.FORBIDDEN} (CORS not allowed)");
                    new Error(res, _config, "CORS not allowed", HttpCodes.FORBIDDEN).Throw();
                    return;
                }
            }

            //if dev has used the express mapping, we run the mapped function
            //if (RunIfExpressMapping(req, res)) return;

            //We check if the route requested is handled by any servlet
            var o = GetInstance(req);

            if (o != null)
            {
                ParameterInfo[] parameters;
                switch (o)
                {
                    //we check if the object is a servlet or a websocket
                    case WebSocket when !req.IsWebSocket():
                        _config.Debug.WARNING(
                            $"{req.Method} '{req.Url}' {HttpCodes.METHOD_NOT_ALLOWED} (Invalid Request) ");
                        new Error(res, _config, "Invalid Request", HttpCodes.METHOD_NOT_ALLOWED).Throw();
                        return;
                    case WebSocket ws:
                        ws.Process();
                        return;
                    case (null, RoutableMethod route):
                        if (route.Type != RoutableMethodType.Delegate)
                        {
                            throw new Exception("Invalid route type, expected delegate");
                        }

                        parameters = route.Delegate!.GetMethodInfo().GetParameters();
                        List<object> callingParams = [];

                        foreach (var field in parameters)
                        {
                            if (field.ParameterType == typeof(Request))
                            {
                                callingParams.Add(req);
                            }
                            else if (field.ParameterType == typeof(Response))
                            {
                                callingParams.Add(res);
                            }
                        }

                        route.Delegate!.DynamicInvoke(callingParams.ToArray());
                        return;

                    case (Type tipo, RoutableMethod route):
                        if (route.Type != RoutableMethodType.Method)
                        {
                            throw new Exception("Invalid route type, expected Class and Method");
                        }

                        parameters = route.MethodInfo!.GetParameters();
                        var instance = Activator.CreateInstance(tipo);

                        //get public instance fields
                        var fields = tipo
                            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(fi => fi.FieldType == typeof(Request) || fi.FieldType == typeof(Response));
                        foreach (var field in fields)
                        {
                            if (field.FieldType == typeof(Request))
                            {
                                field.SetValue(instance, req);
                            }
                            else if (field.FieldType == typeof(Response))
                            {
                                field.SetValue(instance,
                                    res); //response is not available at this point, we will set it to null and then inject the real response in the Process() function
                            }
                        }

                        _config.Debug.INFO($"HTTP Request | {req.Method} {req.Url}");
                        //try automatically inject parameters from request to the method
                        //method must use decorator to specify the parameters to inject, for example [FromQuery] or [FromBody]
                        //try collect parameters
                        var methodParameters = route.MethodInfo.GetParameters()
                            .Where(p => p.GetCustomAttribute<NamedParameter>() != null);

                        List<object> injectionParameters = [];

                        foreach (var methodParameter in methodParameters)
                        {
                            var paramAttributes = methodParameter.GetCustomAttribute<NamedParameter>();

                            if ((!req.Parameters.ContainsKey(paramAttributes!.Name) ||
                                 req.Parameters[paramAttributes!.Name] == "") && paramAttributes.Required)
                            {
                                new Error(res,
                                        _config,
                                        $"Missing value for parameter {paramAttributes!.Name}",
                                        HttpCodes.BAD_REQUEST)
                                    .Throw();
                                return;
                            }

                            //parameters must be collected in order to be applied
                            var paramValue = req.Parameters[paramAttributes!.Name];
                            var parsedType = TypeUtils.ConvertToType(paramValue, methodParameter.ParameterType);
                            injectionParameters.Add(parsedType);
                        }

                        if (parameters.Length != methodParameters.Count())
                        {
                            //method has parameters that are not noted by decorators, we will try to inject req and res if those are needed
                            foreach (var parameter in parameters)
                            {
                             
                                if (parameter.ParameterType == typeof(Request))
                                {
                                    injectionParameters.Add(req);
                                }
                                else if (parameter.ParameterType == typeof(Response))
                                {
                                    injectionParameters.Add(res);
                                }
                                else
                                {
                                    injectionParameters.Add(new object()); //fall back
                                }
                            }
                        }

                        switch (parameters.Length)
                        {
                            case 0:
                                route.MethodInfo.Invoke(instance, null);
                                break;
                            default:
                                route.MethodInfo.Invoke(instance, injectionParameters.ToArray());
                                break;
                        }

                        return;

                    default:
                        Console.WriteLine(o.GetType());
                        throw new Exception($"Developer tried to map an invalid object to a route -> {o.GetType()}");
                }
            }
            else
            {
                //the client searched for a route that is not mapped by any servlet
                //so we do some other checks like root page or static resource
                //if no root page is set we search for and index.html file, else we show the default home page
                if (req.Url == "/")
                {
                    //if the client is requesting the root file, we check if there is an index.html file
                    //if not, we use the default servlet
                    if (File.Exists(_config.StaticFolderPath + "/index.html"))
                    {
                        _config.Debug.INFO($"{req.Method} '{req.Url}' 200");
                        res.SendHTMLFile(_config.StaticFolderPath + "/index.html");
                    }
                    else
                    {
                        _config.Debug.INFO($"{req.Method} '{req.Url}' 200 (Default Index Page)");
                        new Index(res, _config).Get();
                    }
                }
                else
                {
                    //we check if the client is requesting a resource, else 404 not found
                    //to check if the path is safe we use the same regex used in send.js
                    //see: https://github.com/pillarjs/send/blob/master/index.js#L63
                    if (CheckSafePath(req.Url, req, res))
                    {
                        return;
                    }

                    //if the path is safe, the static folder is set and the file exists, we send it
                    if (_config.StaticFolderPath != "" && File.Exists(_config.StaticFolderPath + "/" + req.Url))
                    {
                        //config.debug.INFO($"Static file found, serving '{req.URL}'");
                        _config.Debug.INFO($"{req.Method} '{req.Url}' 200 (Static file)");
                        res.SendFile(_config.StaticFolderPath + "/" + req.Url);
                    }
                    else if (_config.ServeEmbeddedResource &&
                             ResourceUtils.IsEmbeddedResource(req.Url, _config.EmbeddedResourcePrefix))
                    {
                        _config.Debug.INFO($"{req.Method} '{req.Url}' 200 (Embedded resource)");
                        object resource = ResourceUtils.LoadResource<object>(req.Url, _config.EmbeddedResourcePrefix) ??
                                          throw new Exception("Resource not found");
                        res.SendObject(resource, req.Url);
                    }
                    else
                    {
                        //if no servlet or static file found, send 404
                        _config.Debug.INFO($"{req.Method} '{req.Url}' 404 (Resource not found)");
                        new Error(res, _config, "Page not found", HttpCodes.NOT_FOUND).Throw();
                    }
                }
            }
        }
        catch (Exception e)
        {
            //config.debug.ERROR("Error handling request ->\n " + e);
            _config.Debug.ERROR($"{req.Method} '{req.Url}' 500 (Internal Server Error)\n{e}");
            //we show an error page with the message and code 500
            new Error(res, _config, e.ToString(), HttpCodes.INTERNAL_SERVER_ERROR).Throw();
        }
    }

    private bool CheckSafePath(string path, Request req, Response res)
    {
        if (!PathUtils.IsUnsafePath(path)) return false;

        _config.Debug.WARNING($"{req.Method} '{req.Url}' 200 (Requested unsafe path, ignoring request)");
        new Error(res, _config, "", HttpCodes.NOT_FOUND).Throw();

        if (!_config.IpAutoblock) return true;

        _config.Debug.WARNING($"Autoblocking IP {req.ClientIp}");

        if (File.Exists("./banned_ips.txt"))
            File.AppendAllText("./banned_ips.txt", req.ClientIp + "\n");
        else
            File.WriteAllText("./banned_ips.txt", req.ClientIp + "\n");

        return true;
    }

    private void MapRoutes()
    {
        //order : ExpressMapping -> Controllers -> Servlets -> Static files
        //Servlets must be deprecated

        _config.Debug.INFO("Collecting routes...");


        //express routes are now treated equally to controller routes
        _routes.AddRange(_config.ExpressRoutes.Select(r => new Map()
        {
            Path = r.Path,
            SubRoutes =
            [
                new RoutableMethod()
                {
                    Delegate = r.Delegate,
                    HttpMethod = r.HttpMethod,
                    Path = "/"
                }
            ]
        }));


        string[] excludeList = ["System", "Microsoft", "Internal", "HSB"];

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !excludeList.Any(e => a.FullName!.StartsWith(e)));

        var classes = assemblies.SelectMany(assembly => assembly.GetTypes(), (assembly, type) => new {assembly, type})
            .Where(@t => @t.type.IsClass)
            .Select(@t => @t.type)
            .Where(@t => @t.GetCustomAttribute<Controller>() != null)
            .ToList();


        foreach (var c in classes)
        {
            //get only class with the attribute [Controller]

            var attr = c.GetCustomAttribute<Controller>(false);
            if (attr == null) continue;
            _config.Debug.INFO($"Controller | {c.Name} -> {attr.Path}");

            var map = new Map()
            {
                Path = attr.Path,
                Class = c,
                SubRoutes = []
            };

            var methods = c.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<Route>(false) != null);
            foreach (var m in methods)
            {
                var routeAttr = m.GetCustomAttribute<Route>(true);
                if (routeAttr == null) continue;
                var logStr =
                    $"ROUTE | {Terminal.FG_TO_STRING(FgColor.Green)}{routeAttr.Method}{Terminal.RESET} -> {Terminal.FG_TO_STRING(FgColor.Yellow)}{attr.Path}{routeAttr.Path}{Terminal.RESET} {m.Name}";
                _config.Debug.INFO(logStr);

                map.SubRoutes.Add(new RoutableMethod()
                {
                    Path = routeAttr.Path,
                    HttpMethod = routeAttr.Method,
                    MethodInfo = m
                });
            }

            _routes.Add(map);
        }
    }


    public List<Map> GetRoutes()
    {
        return _routes;
    }

    public Configuration GetConfiguration()
    {
        return _config;
    }
}