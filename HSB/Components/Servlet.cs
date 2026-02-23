using System.Reflection;
using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB;

[Obsolete("Use Controller instead of Servlet for better organization and more features. Servlet will still work but it's recommended to switch to Controller for new projects.", false)]
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
            var assembly = Assembly.GetExecutingAssembly();
            var _resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(resourceName));
            string result;
            using (var stream = assembly.GetManifestResourceStream(_resourceName)!)
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
        //if the servlet has a method with a file associated and if it exists it will be sent
        var associatedFiles = GetType().GetCustomAttributes<AssociateFile>().ToList();
        if (associatedFiles.Count > 0)
        {
            var file = associatedFiles
                .Where(a => a.MethodMatches(req.Method) || a.CustomMethodMatches(req.RawMethod.ToUpper()))
                .ToList();
            if (file.Count > 0)
            {
                var path = file.First().FilePath;
                if (!Path.IsPathRooted(path))
                {
                    //if the path is not rooted, we assume it is relative to the current directory
                    path = Path.Combine(Directory.GetCurrentDirectory(), path);
                }

                if (File.Exists(path))
                {
                    res.SendFile(file.First().FilePath);
                    // configuration.debug.INFO($"Serving associated file {}", true);
                }

                return;
            }
        }

        switch (req.Method)
        {
            case HttpMethod.Get:
                GET();
                break;
            case HttpMethod.Post:
                POST();
                break;
            case HttpMethod.Put:
                PUT();
                break;
            case HttpMethod.Delete:
                DELETE();
                break;
            case HttpMethod.Head:
                HEAD();
                break;
            case HttpMethod.Patch:
                PATCH();
                break;
            case HttpMethod.Options:
                OPTIONS();
                break;
            case HttpMethod.Trace:
                TRACE();
                break;
            case HttpMethod.Connect:
                CONNECT();
                break;
            case HttpMethod.Unknown:
            default:
                if (CustomMethodsMap.Count < 1)
                {
                    res.Send(HttpCodes.METHOD_NOT_ALLOWED);
                    return;
                }
                if (CustomMethodsMap.ContainsKey(req.RawMethod.ToUpper()))
                {
                    Terminal.Info($"Custom method requested for route '{req.Url}'", true);
                    CustomMethodsMap[req.RawMethod].DynamicInvoke(req, res);
                    return;
                }

                if (handlerFallback != null)
                {
                    handlerFallback.DynamicInvoke();
                    return;
                }

                Terminal.Error($"Can't process request, unknown HTTP method or malformed request : {req.GetRawRequest}",
                    true);
                res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
                break;
        }
    }

    public virtual void POST()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }

    public virtual void GET()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }

    public virtual void DELETE()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }

    public virtual void PUT()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }

    public virtual void HEAD()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }

    public virtual void PATCH()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }

    public virtual void OPTIONS()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }

    public virtual void TRACE()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }

    public virtual void CONNECT()
    {
        res.SendCode(HttpCodes.METHOD_NOT_ALLOWED);
    }
}