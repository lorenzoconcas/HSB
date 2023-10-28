using System.Text.Json;
using HSB.Constants;

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
        /// Set server listening mode to any, only ipv4 or only ipv6
        /// </summary>
        public IPMode ListeningMode { get; set; }
        /// <summary>
        /// Indicates the location where all static files will be searched and served from
        /// </summary>
        public string staticFolderPath;
        /// <summary>
        /// Holds all debug information and routines
        /// </summary>
        public Debugger debug;
        /// <summary>
        /// Specifies the size in bytes of the buffer that will contain the HTTP request
        /// </summary>
        public int requestMaxSize;
        /// <summary>
        /// Defines the size of a kilobyte in bytes, useful to set the requestMaxSize
        /// </summary>
        public const int KILOBYTE = 1024;
        /// <summary>
        /// Defines the size of a megabyte in bytes, useful to set the requestMaxSize
        /// </summary>
        public const int MEGABYTE = KILOBYTE * KILOBYTE;//1024 * KILOBYTE;
        /// <summary>
        /// Hide the HSB logo on startup
        /// </summary>
        public bool HideBranding = false;
        /// <summary>
        /// Useful to share objects between servlets without using the singleton technique
        /// </summary>
        protected Dictionary<string, object> sharedObjects = new();
        /// <summary>
        /// headers added to ANY response
        /// </summary>
        protected Dictionary<string, string> customGlobalHeaders = new();
        /// <summary>
        /// Cookies added to ANY response
        /// </summary>
        protected Dictionary<string, Cookie> customGlobalCookies = new();
        /// <summary>
        /// Sets the expiration time of the session
        /// </summary>
        public ulong defaultSessionExpirationTime;
        /// <summary>
        /// Expressjs-like routing (es in expressjs you map pages and path like : app.get(path, (req, res){})
        /// </summary>
        private readonly List<Tuple<string, Tuple<HTTP_METHOD, Delegate>>> expressMapping = new();
        /// <summary>
        /// When set, the server will use this name instead of the default one (default is: HSB-#/assembly_version (os_version))
        /// </summary>
        public string CustomServerName = "";
        /// <summary>
        /// If this is set, the server will block the IP of the client if they try to access unsafe paths
        /// </summary>
        public bool IPAutoblock = false;
        /// <summary>
        /// Setting this to BlockMode.WhiteList will make the server accept only requests from ip presents in ip_whitelist.txt
        /// if set to BlockMode.BlackList will ban requests from ip presents in ip_blacklist.txt
        /// </summary>
        public BLOCK_MODE blockMode = BLOCK_MODE.NONE;
        /// <summary>
        /// This list contains all the IP addresses that will be allowed/denied to access the server, it's behavior depends on the blockMode
        /// If blockmode is set to BlockMode.OKLIST, only the IP addresses in this list will be allowed to access the server
        /// If blockmode is set to BlockMode.BANLIST, the IP addresses in this list will be banned from accessing the server
        /// </summary>
        /// <remarks>Note that IPv6 and IPv4 are considered different ips!</remarks>
        public List<string> PermanentIPList = new();
        /// <summary>
        /// Creates a default fail-safe configuration (still, the port could be in use)
        /// </summary>
        public Configuration()
        {
            address = "";
            port = 8080;
            staticFolderPath = "./static";
            debug = new Debugger();
            requestMaxSize = KILOBYTE; //max 1KB Requests default
            ListeningMode = IPMode.ANY; //listen to both ipv6 and ipv4
            //default one day
            defaultSessionExpirationTime = (ulong)TimeSpan.FromDays(1).Ticks;
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
            // UseIPv4Only = root.GetProperty(nameof(UseIPv4Only)).GetBoolean();
            defaultSessionExpirationTime = root.GetProperty("defaultSessionExpirationTime").GetUInt64();
        }

        /// <summary>
        /// Instantiate a configuration with the base settings
        /// </summary>
        /// <param name="address">Listening address (es: "127.0.0.1" or "192.168.1.2" or "" for any)</param>
        /// <param name="port">Listening port</param>
        /// <param name="staticPath">Path of the static folder</param>
        /// <param name="debugInfo">Class holding debugging information</param>
        /// <param name="IPv4Only">Sets whether or not listen only to ipv6 addresses</param>
        public Configuration(string address, int port, string staticPath, Debugger? debugInfo = null, IPMode ipMode = IPMode.ANY)
        {
            this.address = address;
            this.port = port;
            staticFolderPath = staticPath;
            debug = debugInfo ?? new Debugger();
            ListeningMode = ipMode;
            //default 1MB max requests
            requestMaxSize = 1024;
            //default one day
            defaultSessionExpirationTime = (ulong)TimeSpan.FromDays(1).Ticks;
        }

        private void AddExpressMapping(string path, HTTP_METHOD method, Delegate func)
        {
            Tuple<HTTP_METHOD, Delegate> x = new(method, func);
            Tuple<string, Tuple<HTTP_METHOD, Delegate>> tuple = new(path, x);
            expressMapping.Add(tuple);
        }

        protected internal List<Tuple<string, Tuple<HTTP_METHOD, Delegate>>> ExpressRoutes => expressMapping;



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
        public Dictionary<string, string> CustomGlobalHeaders => customGlobalHeaders;


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
        public Dictionary<string, Cookie> CustomGlobalCookies => customGlobalCookies;


        public void HideBrandingOnStartup() => HideBranding = true;

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

            var staticRoutes = Server.CollectStaticRoutes();

            if (staticRoutes.Any())
            {
                str += "\nStatic routes:";
                staticRoutes.ToList().ForEach(m => str += $"\nPath : {m.Key.Item1} -> {m.Value.Name}");
            }
            return str;
        }


        public List<Tuple<string, string>> GetAllRoutes()
        {
            List<Tuple<string, string>> routes = new();
            expressMapping.ForEach(m => routes.Add(new(m.Item1, m.Item2.Item1.ToString())));
            var staticRoutes = Server.CollectStaticRoutes();
            staticRoutes.ToList().ForEach(m => routes.Add(new(m.Key.Item1, "")));
            return routes;
        }

    }
}
