using System.Reflection;
using HSB.Constants;
namespace HSB;

public class Servlet
{
    protected Request req;
    protected Response res;
    protected Configuration configuration;
    //in case of an unsupported http method, we can specify a generic handler
    protected Delegate? handlerFallback;

    private Dictionary<string, Delegate> CustomMethodsMap;
    public Servlet(Request req, Response res)
    {
        if (req == null || res == null)
            throw new Exception("Request or Response cannot be null!");
        this.req = req;
        this.res = res;
        configuration = new();
        CustomMethodsMap = new();
    }

    public Servlet(Request req, Response res, Configuration conf)
    {
        if (req == null || res == null)
            throw new Exception("Request or Response cannot be null!");
        this.req = req;
        this.res = res;
        configuration = conf;
        CustomMethodsMap = new();
    }

    public void AddCustomMethodHandler(string name, Delegate handler)
    {
        CustomMethodsMap.Add(name.ToUpper(), handler);
    }

    public void RemoveCustomMethodHandler(string name)
    {
        CustomMethodsMap.Remove(name);
    }

    public string GetRoute()
    {
        //if class has Binding (Attribute), we return is value, else ""
        var binding = this.GetType().GetCustomAttribute<Binding>();
        return binding == null ? "" : binding.Path;
    }
    /// <summary>
    /// Extract a string from an embedded resource
    /// </summary>
    /// <param name="resourceName"></param>
    /// <returns>The resource as string or an empty string if not found</returns>
    protected static string ReadFromResources(string resourceName)
    {
        try
        {

            Assembly assembly = Assembly.GetExecutingAssembly();
            string _resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
            string result;
            using (Stream stream = assembly.GetManifestResourceStream(_resourceName)!)
            using (StreamReader reader = new(stream))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }
        catch (Exception)
        {
            return "";
        }
    }

    public void Process()
    {
        //if the servlet has a method with a file associated and it exists it will be send
        var associatedFiles = GetType().GetCustomAttributes<AssociatedFile>();
        if (associatedFiles.Any())
        {
            var file = associatedFiles.Where(a => a.MethodMatches(req.METHOD) || a.CustomMethodMatches(req.RawMethod.ToUpper()));
            if (file.Any())
            {
                var path =  file.First().FilePath;
                if(!Path.IsPathRooted(path)){
                    //if the path is not rooted, we assume it is relative to the current directory
                    path = Path.Combine(Directory.GetCurrentDirectory(), path);
                }
                if(File.Exists(path)){
                    res.SendFile(file.First().FilePath);
                   // configuration.debug.INFO($"Serving associated file {}", true);
                }
                return;
            }
        }

        switch (req.METHOD)
        {
            case HTTP_METHOD.GET:
                ProcessGet();
                break;
            case HTTP_METHOD.POST:
                ProcessPost();
                break;
            case HTTP_METHOD.PUT:
                ProcessPut();
                break;
            case HTTP_METHOD.DELETE:
                ProcessDelete();
                break;
            case HTTP_METHOD.HEAD:
                ProcessHead();
                break;
            case HTTP_METHOD.PATCH:
                ProcessPatch();
                break;
            case HTTP_METHOD.OPTIONS:
                ProcessOptions();
                break;
            case HTTP_METHOD.TRACE:
                ProcessTrace();
                break;
            case HTTP_METHOD.CONNECT:
                ProcessConnect();
                break;
            default:
                if (CustomMethodsMap.ContainsKey(req.RawMethod.ToUpper()))
                {
                    Terminal.INFO($"Custom method requested for route '{req.URL}'", true);
                    CustomMethodsMap[req.RawMethod].DynamicInvoke(req, res);
                    return;
                }
                if (handlerFallback != null)
                {
                    handlerFallback.DynamicInvoke();
                    return;
                }
                Terminal.ERROR($"Can't process request, unknown HTTP method or malformed request : {req.GetRawRequest}", true);
                res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
                break;

        }
    }

    public virtual void ProcessPost()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

    public virtual void ProcessGet()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

    public virtual void ProcessDelete()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

    public virtual void ProcessPut()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

    public virtual void ProcessHead()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

    public virtual void ProcessPatch()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

    public virtual void ProcessOptions()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

    public virtual void ProcessTrace()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }

    public virtual void ProcessConnect()
    {
        res.SendCode(HTTP_CODES.METHOD_NOT_ALLOWED);
    }
}
