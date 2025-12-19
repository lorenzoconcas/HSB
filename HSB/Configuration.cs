using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HSB.Constants;

namespace HSB;

/// <summary>
/// This class contains all the settings of the server
/// </summary>

public class Configuration
{

    //private props
    private static readonly JsonSerializerOptions JserializerOptions = new() { WriteIndented = true, IncludeFields = true };
    
    /// <summary>
    /// The server listening address, ex : "127.0.0.1" or "192.168.1.2" or "" (for any address)
    /// </summary>
    public string Address; //Todo -> support for multiple addresses?
    /// <summary>
    /// The server listening port
    /// </summary>
    public ushort Port;

    /// <summary>
    /// The max number of concurrent connections, by default is 100
    /// </summary>
    public ushort MaxConnections = 100;
    /// <summary>
    /// When this field is set, it will be used for Unsecure SSL requests upgrade 
    /// </summary>
    public readonly string PublicUrl = "";
    /// <summary>
    /// Set server listening mode to any, only ipv4 or only ipv6. This is valid only if the address is set to ""
    /// </summary>
    public IPMode ListeningMode { get; set; }
    /// <summary>
    /// Indicates the location where all static files will be searched and served from
    /// </summary>
    public string StaticFolderPath;
    /// <summary>
    /// Holds all debug information and routines
    /// </summary>
    public Debugger Debug;
    /// <summary>
    /// Specifies the size in bytes of the buffer that will contain the HTTP request
    /// </summary>
    public int RequestMaxSize;
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
    protected Dictionary<string, object> sharedObjects = [];
    /// <summary>
    /// headers added to ANY response
    /// </summary>
    protected Dictionary<string, string> customGlobalHeaders = [];
    /// <summary>
    /// Cookies added to ANY response
    /// </summary>
    protected Dictionary<string, Cookie> customGlobalCookies = [];
    /// <summary>
    /// Sets the expiration time of the session
    /// </summary>
    public ulong DefaultSessionExpirationTime;
    /// <summary>
    /// Expressjs-like routing (es in expressjs you map pages and path like : app.get(path, (req, res){})
    /// </summary>
    private readonly List<Tuple<string, Tuple<HTTP_METHOD, Delegate>>> expressMapping = [];
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
    public BLOCK_MODE BlockMode = BLOCK_MODE.NONE;
    /// <summary>
    /// This list contains all the IP addresses that will be allowed/denied to access the server, it's behavior depends on the blockMode
    /// If BlockMode is set to BlockMode.OKLIST, only the IP addresses in this list will be allowed to access the server
    /// If BlockMode is set to BlockMode.BANLIST, the IP addresses in this list will be banned from accessing the server
    /// </summary>
    /// <remarks>Note that IPv6 and IPv4 are considered different ips!</remarks>
    public List<string> PermanentIPList = [];
    /// <summary>
    /// If set to true, the server will try to search for the requested resource in the assembly resources
    /// if fails to find it, the usual chain of execution will be followed
    /// </summary>
    public bool ServeEmbeddedResource = false;
    /// <summary>
    /// If ServeEmbeddedResource is set to true, this will be prepended to the requested resource
    /// ex: if the requested resource is /index.html and the prefix is set to "www"
    /// the server will search for the resource in the assembly resources at www/index.html
    /// </summary>
    public string EmbeddedResourcePrefix = "";

    /// <summary>
    /// If this is not empty the server will map any embedded resource with the prefix in this list as paths
    /// for example the folder ./dist/ is included as embedded resources and this folder contains the following files:
    /// ./dist/index.html
    /// ./dist/file.js
    /// ./dist/style.css
    ///
    ///  those files will have this resource identifier:
    /// YourApplicationNameSpace.dist.FILE_NAME
    /// setting this prefix will make convert automatically those files in paths, ex
    /// ./dist/file.js -> /file.js and so on
    ///
    /// WIP!
    /// </summary>
    public string[] EmbeddedPaths = [];
    /// <summary>
    /// Contains the arguments passed from the command line
    /// </summary>
    private readonly List<string> rawArguments = [.. Environment.GetCommandLineArgs()];

    /// <summary>
    /// Contains the SSL configuration properties
    /// </summary>
    public SslConfiguration SslSettings;

    public CORS? GlobalCORS = null;

    /// <summary>
    /// If set to a non-empty string, a page mapped to this path will show routes with the @Documentation attribute,
    /// it's similiar to Swagger or alternative documentation systems
    /// </summary>
    public string DocumentationPath = "";
    
    /// <summary>
    /// Creates a default fail-safe configuration (still, the port could be in use)
    /// </summary>
    public Configuration()
    {
        Address = "";
        Port = 8080;
        
        StaticFolderPath = ""; //no static file support if not set
        Debug = new Debugger();
        RequestMaxSize = KILOBYTE; //max 1KB Requests default
        ListeningMode = IPMode.ANY; //listen to both ipv6 and ipv4
        //default one day
        DefaultSessionExpirationTime = (ulong)TimeSpan.FromDays(1).Ticks;
        SslSettings = new SslConfiguration();
        GlobalCORS = null;
        DocumentationPath = "";
    }

    /// <summary>
    /// Instantiate configuration from a json file (content passed as string)
    /// </summary>
    /// <param name="jsonContent">The content of the JSON file</param>
    public Configuration(string content)
    {
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        string lastProp = "";
        try
        {

            lastProp = "Address"; Address = root.GetProperty("Address").GetString() ?? "";
            lastProp = "Port"; Port = root.GetProperty("Port").GetUInt16();
            lastProp = "MaxConnections"; MaxConnections = root.GetProperty("MaxConnections").GetUInt16();
            lastProp = "PublicURL"; PublicUrl = root.GetProperty("PublicURL").GetString() ?? "";
            lastProp = "StaticFolderPath"; StaticFolderPath = root.GetProperty("StaticFolderPath").GetString() ?? "";
            lastProp = "Debug"; Debug = Debugger.FromJson(root.GetProperty("Debug"));
            lastProp = "SslSettings"; SslSettings = SslConfiguration.FromJSON(root.GetProperty("SslSettings"));
            lastProp = "RequestMaxSize"; RequestMaxSize = root.GetProperty("Port").GetInt32();
            lastProp = "BlockMode"; BlockMode = (BLOCK_MODE)root.GetProperty("BlockMode").GetInt32();
            lastProp = "HideBranding"; HideBranding = root.GetProperty("HideBranding").GetBoolean();
            lastProp = "IPAutoblock"; IPAutoblock = root.GetProperty("IPAutoblock").GetBoolean();
            lastProp = "ListeningMode"; ListeningMode = (IPMode)root.GetProperty(nameof(ListeningMode)).GetInt32();
            lastProp = "CustomServerName"; CustomServerName = root.GetProperty("CustomServerName").GetString() ?? "";
            lastProp = "ServeEmbeddedResource"; ServeEmbeddedResource = root.GetProperty("ServeEmbeddedResource").GetBoolean();
            lastProp = "EmbeddedResourcePrefix"; EmbeddedResourcePrefix = root.GetProperty("EmbeddedResourcePrefix").GetString() ?? "";
            lastProp = "DefaultSessionExpirationTime"; DefaultSessionExpirationTime = root.GetProperty("DefaultSessionExpirationTime").GetUInt64();
            lastProp = "GlobalCORS"; GlobalCORS = CORS.FromJSON(root);
            lastProp = "DocumentationPath"; DocumentationPath = root.GetProperty("DocumentationPath").GetString() ?? "";
            lastProp = "PermanentIPList";

            foreach (var v in root.GetProperty("PermanentIPList").EnumerateArray().Select(item => item.GetString()).OfType<string>())
            {
                PermanentIPList.Add(v);
            }



        }
        catch (Exception e)
        {
            Terminal.ERROR("Cannot parse configuration file");
            Terminal.ERROR(e);
            Terminal.ERROR($"Last property that was being parsed: {lastProp}");
            Environment.Exit(1);
        }
    }


    /// <summary>
    /// Instantiate a configuration with the minimal settings
    /// </summary>
    /// <param name="address">Listening address (es: "127.0.0.1" or "192.168.1.2" or "" for any)</param>
    /// <param name="port">Listening port</param>
    /// <param name="staticPath">Path of the static folder</param>
    /// <param name="debugInfo">Class holding debugging information</param>
    /// <param name="IPv4Only">Sets whether listen only to ipv6 addresses</param>
    public Configuration(string address, ushort port, string staticPath, Debugger? debugInfo = null, IPMode ipMode = IPMode.ANY, int requestMaxSize = KILOBYTE, ulong? defaultSessionExpirationTime = null, SslConfiguration? sslConfiguration = null)
    {
        Address = address;
        Port = port;
        StaticFolderPath = staticPath;
        Debug = debugInfo ?? new Debugger();
        ListeningMode = ipMode;
        //default 1KB max requests
        RequestMaxSize = requestMaxSize;
        //default one day
        DefaultSessionExpirationTime = defaultSessionExpirationTime ?? (ulong)TimeSpan.FromDays(1).Ticks;
        SslSettings = sslConfiguration ?? new SslConfiguration();
        DocumentationPath = "";
    }

    /// <summary>
    /// Instantiate configuration from a json file passed as parameter
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Configuration LoadFromJson(string path)
    {
        string content = File.ReadAllText(path);
        return new Configuration(content);
    }
    /// <summary>
    /// Save the current configuration to a json file
    /// </summary>
    /// <param name="path"></param>
    public void SaveToJson(string path)
    {
       
        string json = JsonSerializer.Serialize(this, JserializerOptions);
        File.WriteAllText(path, json);
    }
    public void AddExpressMapping(string path, HTTP_METHOD method, Delegate func)
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

    public List<string> GetRawArguments() => rawArguments;

    public void HideBrandingOnStartup() => HideBranding = true;
    /// <summary>
    /// String representing the configuration
    /// </summary>
    /// <returns></returns>

    public override string ToString()
    {
        string str = $"Current configuration:\nListening address and port: {Address}:{Port}";
        if (StaticFolderPath == "")
            str += "\nStatic folder is not set";
        else
            str += $"\nStatic folder path: {StaticFolderPath}";

        if (expressMapping.Count != 0)
        {
            str += "\nExpressJS-Like routing map:";
            expressMapping.ForEach(m => str += $"\nPath : {m.Item1} -> {m.Item2.Item2.Method.Name}");
        }

        var staticRoutes = Server.CollectStaticRoutes();

        if (staticRoutes.Count != 0)
        {
            str += "\nStatic routes:";
            staticRoutes.ToList().ForEach(m => str += $"\nPath : {m.Key.Item1} -> {m.Value.Name}");
        }
        return str;
    }

    public List<Tuple<string, string>> GetAllRoutes()
    {
        List<Tuple<string, string>> routes = [];
        expressMapping.ForEach(m => routes.Add(new(m.Item1, m.Item2.Item1.ToString())));
        var staticRoutes = Server.CollectStaticRoutes();
        staticRoutes.ToList().ForEach(m => routes.Add(new(m.Key.Item1, "")));
        return routes;
    }

}
