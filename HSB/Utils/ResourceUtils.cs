using System.Reflection;

namespace HSB.Utils;

public static class ResourceUtils
{
    internal static bool IsEmbeddedResource(string url, string prefix = "")
    {
        if (url.StartsWith('/')) url = url[1..];
        url = url.Replace('/', '.');
        if (prefix != "")
            url = $"{prefix}.{url}";
        AppDomain currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assemblies = [.. currentDomain.GetAssemblies()];

        assemblies.RemoveAll(a => a.ToString().StartsWith("System"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("Microsoft"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("Internal"));

        foreach (var assembly in assemblies)
        {
            foreach (var res in assembly.GetManifestResourceNames())
            {
                if (res.EndsWith(url)) return true;
            }
        }

        return false;
    }

    //Search all the loaded assemblies for the resource, then reads it and returns it as object
    public static T LoadResource<T>(string resName, string prefix = "")
    {
        if (resName.StartsWith('/')) resName = resName[1..];
        resName = resName.Replace('/', '.');
        if (prefix != "")
            resName = $"{prefix}.{resName}";
        AppDomain currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assemblies = [.. currentDomain.GetAssemblies()];

        assemblies.RemoveAll(a => a.ToString().StartsWith("System"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("Microsoft"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("Internal"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("ILLink"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("FxResources"));

        foreach (var assembly in assemblies)
        {
            var resources = assembly.GetManifestResourceNames();
            if (resources.Length == 0) continue;
            var resourceName = resources.First(str => str.EndsWith(resName));
            try
            {
                using var stream = assembly.GetManifestResourceStream(resourceName)!;
                if (typeof(T) == typeof(byte[]))
                {
                    var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    return (T) Convert.ChangeType(ms.ToArray(), typeof(T));
                }
                else
                {
                    using StreamReader r = new(stream);
                    return (T) Convert.ChangeType(r.ReadToEnd(), typeof(T));
                }
            }
            catch (Exception)
            {
                return default!;
            }
        }

        return default!;
    }

    internal static string LoadResourceAsString(string resName)
    {
        var assembly = Assembly.GetCallingAssembly();
        string resourceName;

        try
        {
            resourceName = assembly.GetManifestResourceNames().First(str => str.EndsWith(resName));
        }
        catch (Exception)
        {
            return "";
        }


        string result;

        Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return "";
        }

        using (StreamReader reader = new(stream))
        {
            result = reader.ReadToEnd();
        }

        return result;
    }

    public static List<String> GetResourcesList(string prefix = "")
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assemblies = [.. currentDomain.GetAssemblies()];

        assemblies.RemoveAll(a => a.ToString().StartsWith("System"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("Microsoft"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("Internal"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("ILLink"));
        assemblies.RemoveAll(a => a.ToString().StartsWith("FxResources"));


        List<string> resources = [];

        foreach (var assembly in assemblies)
        {
            var rsrcs = assembly.GetManifestResourceNames().ToList();
            //filter by prefix if not ""
            if (rsrcs.Count > 0 && prefix != "")
            {
                rsrcs = rsrcs.Where(rsrc => rsrc.StartsWith(prefix)).ToList();
            }

            resources.AddRange(rsrcs);
        }

        return resources;

        //  string resourceName = assembly.GetManifestResourceNames().First(str => str.EndsWith(resName));
    }
}