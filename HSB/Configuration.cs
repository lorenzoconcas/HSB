using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HSB
{
    /// <summary>
    /// This class contains all the settings of the server
    /// </summary>
    public partial class Configuration
    {
        /// <summary>
        /// The server listening address, ex : "127.0.0.1" or "192.168.1.2" or "" (for any address) //check this
        /// </summary>
        public string address;

        /// <summary>
        /// The server listening port
        /// </summary>
        public int port;

        /// <summary>
        /// Sets if the server must listen only to IPv4
        /// </summary>
        public bool UseIPv4Only { get; set; }

        /// <summary>
        /// Indicates the location where all static files will be searched and served from
        /// </summary>
        public string staticFolderPath;

        /// <summary>
        /// Holds all debug information and routines
        /// </summary>
        public Debugger debug;

        /// <summary>
        /// Specifies the size of the buffer that will contain the HTTP request
        /// </summary>
        public readonly int requestMaxSize;

        /// <summary>
        /// Useful to share objects between servlets without using the singleton technique
        /// </summary>
        protected Dictionary<string, object> sharedObjects = new();

        /// <summary>
        /// headers added to ANY response
        /// </summary>
        protected Dictionary<string, string> customGlobalHeaders = new();

        protected Dictionary<string, Cookie> customGlobalCookies = new();

        /// <summary>
        /// Sets the expiration time of the session
        /// </summary>
        public long defaultSessionExpirationTime;

        /// <summary>
        /// Expressjs-like routing (es in expressjs you map pages and path like : app.get(path, (req, res){})
        /// </summary>
        private readonly List<Tuple<string, Tuple<HTTP_METHOD, Delegate>>> expressMapping = new();

        /// <summary>
        /// Creates a default fail-safe configuration (still, the port could be in use)
        /// </summary>
        public Configuration()
        {
            address = "127.0.0.1";
            port = 8080;
            staticFolderPath = "./static";
            debug = new Debugger();
            requestMaxSize = 1024; //max 1MB Requests default
            UseIPv4Only = false;
            //default one day
            defaultSessionExpirationTime = TimeSpan.FromDays(1).Ticks;
        }

        /// <summary>
        /// Instantiate configuration from a json file (content passed as string)
        /// </summary>
        /// <param name="jsonContent">The content of the JSON file</param>
        public Configuration(string jsonContent)
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;


            address = root.GetProperty("address").GetString() ?? "127.0.0.1";
            port = root.GetProperty("port").GetInt16();
            staticFolderPath = root.GetProperty("staticFolderPath").GetString() ?? "";
            debug = Debugger.FromJson(root.GetProperty("debug"));
            requestMaxSize = root.GetProperty("port").GetInt32();
            UseIPv4Only = root.GetProperty(nameof(UseIPv4Only)).GetBoolean();
            defaultSessionExpirationTime = root.GetProperty("defaultSessionExpirationTime").GetUInt32();
        }

        /// <summary>
        /// Instantiate a configuration with the base settings
        /// </summary>
        /// <param name="address">Listening address (es: "127.0.0.1" or "192.168.1.2" or "" for any)</param>
        /// <param name="port">Listening port</param>
        /// <param name="staticPath">Path of the static folder</param>
        /// <param name="debugInfo">Class holding debugging information</param>
        /// <param name="IPv4Only">Sets whether or not listen only to ipv6 addresses</param>
        public Configuration(string address, int port, string staticPath, Debugger? debugInfo = null, bool IPv4Only = false)
        {
            this.address = address;
            this.port = port;
            staticFolderPath = staticPath;
            debug = debugInfo ?? new Debugger();
            UseIPv4Only = IPv4Only;
            //default 1MB max requests
            requestMaxSize = 1024;
            //default one day
            defaultSessionExpirationTime = TimeSpan.FromDays(1).Ticks;
        }

        private void AddExpressMapping(string path, HTTP_METHOD method, Delegate func)
        {
            Tuple<HTTP_METHOD, Delegate> x = new(method, func);
            Tuple<string, Tuple<HTTP_METHOD, Delegate>> tuple = new(path, x);
            expressMapping.Add(tuple);
        }

        protected internal void Process(Request req, Response res)
        {


            new Task(() =>
            {
                try
                {
                    //check if request is valid                    
                    if (!req.validRequest)
                    {
                        new Error(req, res, "Invalid Request", 400).Process();
                        return;
                    }
                    debug.INFO($"Requested '{req.URL}'", true);


                    if (RunIfExpressMapping(req, res))
                        return;

                    object? o = GetInstance(req, res);
                    if (o != null)
                    {
                        Servlet servlet = (Servlet)o;
                        servlet.Process();
                    }
                    else
                    {
                        //the client searched for a route that is not mapped by any servlet
                        //so we do some other checks like root page or static resource
                        //if no root page is set we search for and index.html file, else we show the default home page
                        if (req.URL == "/")
                            //if the client is requesting the root file, we check if there is an index.html file
                            //if not, we use the default servlet
                            if (File.Exists(staticFolderPath + "/index.html"))
                            {
                                debug.INFO("Serving home page from disk");
                                res.SendHTMLPage(staticFolderPath + "/index.html");
                            }

                            else
                            {
                                debug.INFO("Serving default home page");
                                new Index(req, res).Process();
                            }
                        else
                        {
                            //we check if the client is requesting a resource, else 404 not found
                            //to check if the path is safe we use the same regex used in send.js
                            //see: https://github.com/pillarjs/send/blob/master/index.js#L63

                            Regex rgx = SafePathRegex();
                            if (rgx.Match(req.URL).Success)
                            {
                                debug.WARNING("Requested unsafe path");
                                new Error(req, res, "", 404).Process();
                            }

                            if (File.Exists(staticFolderPath + "/" + req.URL))
                            {
                                debug.INFO($"Static file found, serving '{req.URL}'");
                                res.SendFile(staticFolderPath + "/" + req.URL);
                            }
                            else
                            {
                                //if no servlet or static file found, send 404
                                debug.WARNING($"No servlet or static found for URL : {req.URL}");
                                new Error(req, res, "Page not found", 404).Process();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    debug.ERROR("Error handling request ->\n " + e);
                    //we show an error page with the message and code 500
                    new Error(req, res, e.ToString(), 500).Process();
                }
            }).Start();
        }

        private bool RunIfExpressMapping(Request req, Response res)
        {
            var e = expressMapping.Find(e => e.Item1 == req.URL && e.Item2.Item1 == req.METHOD);

            if (e == null) return false;
            e.Item2.Item2.DynamicInvoke(req, res);
            return true;
        }


        private static Dictionary<Tuple<string, bool>, Type> CollectStaticRoutes()
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


        private object? GetInstance(Request req, Response res)
        {
            Dictionary<Tuple<string, bool>, Type> routes = CollectStaticRoutes();


            if (routes.ContainsKey(new(req.URL, false)))
            {
                Type c = routes[new(req.URL, false)];
                var x = c.GetConstructors()[0];
                return x.GetParameters().Length switch
                {
                    3 => Activator.CreateInstance(c, req, res, this),
                    2 => Activator.CreateInstance(c, req, res),
                    _ => throw new Exception($"Invalid servlet constructor found {x.Name}"),
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
                        3 => Activator.CreateInstance(c, req, res, this),
                        2 => Activator.CreateInstance(c, req, res),
                        _ => throw new Exception($"Invalid servlet constructor found {x.Name}"),
                    }).FirstOrDefault();
        }

        /// <summary>
        /// String representing the configuration
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str = $"Current configuration:\nListening address and port: {address}:{port}";
            if (staticFolderPath == "")
                str += "\nStatic folder is not set";
            else
                str += $"\nStatic folder path: {staticFolderPath}";

            if (expressMapping.Any())
            {
                str += "\nExpressJS-Like routing map:";
                expressMapping.ForEach(m => str += $"\nPath : {m.Item1} -> {m.Item2.Item2.Method.Name}");
            }

            var staticRoutes = CollectStaticRoutes();

            if (staticRoutes.Any())
            {
                str += "\nStatic routes:";
                staticRoutes.ToList().ForEach(m => str += $"\nPath : {m.Key.Item1} -> {m.Value.Name}");
            }


            return str;
        }

        /// <summary>
        /// Map a function to a path that will reply with a GET response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void GET(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.GET, func);

        /// <summary>
        /// Map a function to a path that will reply with a POST response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void POST(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.POST, func);

        /// <summary>
        /// Map a function to a path that will reply with a HEAD response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void HEAD(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.HEAD, func);

        /// <summary>
        /// Map a function to a path that will reply with a HEAD response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void PUT(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.PUT, func);

        /// <summary>
        /// Map a function to a path that will reply with a DELETE response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void DELETE(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.DELETE, func);

        /// <summary>
        /// Map a function to a path that will reply with a PATCH response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void PATCH(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.PATCH, func);

        /// <summary>
        /// Map a function to a path that will reply with a TRACE response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void TRACE(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.TRACE, func);

        /// <summary>
        /// Map a function to a path that will reply with a OPTIONS response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void OPTIONS(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.OPTIONS, func);

        /// <summary>
        /// Map a function to a path that will reply with a CONNECT response 
        /// </summary>
        /// <param name="path">Mapping</param>
        /// <param name="func">Function that will handle the request</param>
        public void CONNECT(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.CONNECT, func);

        /// <summary>
        /// Add an object shared between all servlet
        /// </summary>
        /// <param name="name">Name of the object</param>
        /// <param name="o">Object to share</param>
        public void AddSharedObject(string name, object o) => sharedObjects.Add(name, o);

        /// <summary>
        /// Get an object shared between all servlet
        /// </summary>
        /// <param name="name">Name of the shared object</param>
        public object GetSharedObject(string name) => sharedObjects[name];

        /// <summary>
        /// Remove an object shared between all servlet
        /// </summary>
        /// <param name="name">Name of the shared object</param>
        public void RemoveSharedObject(string name) => sharedObjects.Remove(name);

        /// <summary>
        /// Add an HTTP Response header that will be added to ALL the responses
        /// </summary>
        /// <param name="name">Name of the header</param>
        /// <param name="value">Value of the header</param>
        public void AddCustomGlobalHeader(string name, string value) => customGlobalHeaders.Add(name, value);

        /// <summary>
        /// Remove a global HTTP Response header previously added
        /// </summary>
        /// <param name="name">Name of the header</param>
        public void RemoveCustomGlobalHeader(string name) => customGlobalHeaders.Remove(name);

        /// <summary>
        /// Gets the value of a global HTTP Response header previously added
        /// </summary>
        /// <param name="name">Name of the header</param>
        public string GetCustomGlobalHeader(string name) => customGlobalHeaders[name];

        /// <summary>
        /// Gets all global HTTP Response headers 
        /// </summary>
        public Dictionary<string, string> GetCustomGlobalHeaders => customGlobalHeaders;


        /// <summary>
        /// Add (Or replaces) a cookie that will be added to ALL the responses
        /// </summary>
        /// <param name="name">Name of the cookie</param>
        /// <param name="value">Value of the cookie</param>
        public void AddCustomGlobalCookie(Cookie cookie)
        {
            customGlobalCookies.Remove(cookie.name);
            customGlobalCookies.Add(cookie.name, cookie);
        }

        /// <summary>
        /// Remove a global cookie previously added
        /// </summary>
        /// <param name="name">Name of the cookie</param>
        public void RemoveCustomGlobalCookie(string name) => customGlobalCookies.Remove(name);

        /// <summary>
        /// Gets the value of a global cookie previously added
        /// </summary>
        /// <param name="name">Name of the cookie</param>
        /// 
        public Cookie GetCustomGlobalCookie(string name) => customGlobalCookies[name];

        /// <summary>
        /// Gets all global cookies
        /// </summary>
        public Dictionary<string, Cookie> GetCustomGlobalCookies => customGlobalCookies;



        [GeneratedRegex("/(?:^|[\\\\/])\\.\\.(?:[\\\\/]|$)/")]
        private static partial Regex SafePathRegex();
    }
}