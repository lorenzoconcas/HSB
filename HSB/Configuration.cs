using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HSB
{
    /// <summary>
    /// This class contains all the settings of the server
    /// </summary>
    public class Configuration
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
        /// Useful to share objects between servlets without using the singleton tecnique
        /// </summary>
        protected Dictionary<string, object> sharedObjects = new();

        /// <summary>
        /// headers added to ANY response
        /// </summary>
        protected Dictionary<string, string> customGlobalHeaders = new();

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
            debug = Debugger.FromJson(root.GetProperty("debugInfo"));
            requestMaxSize = root.GetProperty("port").GetInt32();
        }

        /// <summary>
        /// Instantiate a configuration with the base settings
        /// </summary>
        /// <param name="address">Listening address (es: "127.0.0.1" or "192.168.1.2")</param>
        /// <param name="port">Listening port</param>
        /// <param name="staticPath">Path of the static folder</param>
        /// <param name="debugInfo">Class holding debuggin information</param>
        public Configuration(string address, int port, string staticPath, Debugger? debugInfo = null)
        {
            this.address = address;
            this.port = port;
            staticFolderPath = staticPath;
            debug = debugInfo ?? new Debugger();
            requestMaxSize = 1024;
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
                        //controlliamo se esistono routing che usano regex


                        //non è stata trovata una mappatura valida
                        //se siamo qui non c'è una pagina di root preimpostata, restituiamo quella di default
                        if (req.URL == "/")
                            //controlliamo che ci sia un file "index.html" else base servlet
                            if (File.Exists(staticFolderPath + "/index.html"))
                                res.SendFile(staticFolderPath + "/index.html", "text/html");
                            else
                                new Index(req, res).Process();
                        else
                        {
                            //controlliamo se si cerca una risorsa, altrimenti 404 non trovato

                            //usiamo la regex usata nella libreria send.js ()
                            //see: https://github.com/pillarjs/send/blob/master/index.js#L63
                            var UNSAFE_PATH_REGEX = "/(?:^|[\\\\/])\\.\\.(?:[\\\\/]|$)/";
                            Regex rgx = new(UNSAFE_PATH_REGEX);
                            if (rgx.Match(req.URL).Success)
                            {
                                debug.WARNING("Requested unsafe path");
                                new Error(req, res, "", 404).Process();
                            }

                            if (File.Exists(staticFolderPath + "/" + req.URL))
                            {
                                debug.INFO($"Static file found, serving '{req.URL}'", true);
                                res.SendFile(staticFolderPath + "/" + req.URL);
                            }
                            else
                            {
                                //potrebbe non venire trovata la giusta servlet, rimandiamo un errore 404
                                debug.WARNING($"No servlet or static found for URL : {req.URL}", true);
                                new Error(req, res, "Page not found", 404).Process();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    debug.ERROR("Error handling request ->\n " + e, true);
                    // debug.ERROR(e.Message);

                    new Error(req, res, e.ToString(), 500).Process();
                    return;
                }
            }).Start();
        }

        private bool RunIfExpressMapping(Request req, Response res)
        {
            var e = expressMapping.Find(e => (e.Item1 == req.URL && e.Item2.Item1 == req.METHOD));

            if (e != null)
            {
                e.Item2.Item2.DynamicInvoke(new object[] { req, res });
                return true;
            }

            return false;
        }


        private static Dictionary<Tuple<string, bool>, Type> CollectStaticRoutes()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            List<Assembly> assems = currentDomain.GetAssemblies().ToList();

            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));

            Dictionary<Tuple<string, bool>, Type> routes = new();


            // List<Tuple<string, Type, bool>> routes = new();


            foreach (Assembly assem in assems)
            {
                List<Type> classes = assem.GetTypes().ToList();

                foreach (var c in classes)
                {
                    try
                    {
                        IEnumerable<Binding> multiBindings = c.GetCustomAttributes<Binding>(false);

                        //se non ci sono classi che hanno più binding, cerchiamo chi ne ha una sola
                        if (multiBindings.Any())
                        {
                            foreach (Binding b in multiBindings)
                            {
                                if (b.Path != "")
                                    routes.Add(new(b.Path, b.StartsWith), c);
                                // routes.Add(new(b.Path, c, b.StartsWith));
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
            else
            {
                //controllare se esiste un url che inizia come quello della richiesta
                foreach (var r in routes)
                {
                    string path = r.Key.Item1;
                    if (req.URL.StartsWith(path))
                    {
                        Type c = r.Value;
                        var x = c.GetConstructors()[0];
                        return x.GetParameters().Length switch
                        {
                            3 => Activator.CreateInstance(c, req, res, this),
                            2 => Activator.CreateInstance(c, req, res),
                            _ => throw new Exception($"Invalid servlet constructor found {x.Name}"),
                        };
                    }
                }


                //WIP
                /* //controllare se usa una regex
                 Dictionary<string, Type> regexRoutes = new();
                 foreach (var r in routes)
                     regexRoutes.Add(r.Key.Replace("/", @"\/"), r.Value);


                 IEnumerable<Match> p = regexRoutes.Keys.Select(s => new Regex(s).Match(req.URL));

                 if (p.First().Success)
                 {
                     //trovato il routing regex
                     debug.INFO("lol");
                 }*/
            }

            return null;
        }

        /// <summary>
        /// String rappresentation of the configuration
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
        /// Gets all globabl HTTP Response headers 
        /// </summary>
        public Dictionary<string, string> GetCustomGlobalHeaders => customGlobalHeaders;
    }
}