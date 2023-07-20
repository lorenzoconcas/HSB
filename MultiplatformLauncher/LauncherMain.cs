/*
 * The goal of this program is to provide a launcher that makes possible to
 * launch your HSB server implementation without the need to compile for all 
 * possible platforms, will require just the library
 * 
 * How it works? 
 * 
 * First the launcher will list all *.dll files in the same folder,
 * then it tries to load all of them and finally the function "HSBMain" will
 * be searched and launched. If HSBMain is not found the program quits
 * 
 * 
 * How to compile in a single file?
 * 1) Open a terminal and go the project folder
 * 2) Give this command:
 * 
 *  dotnet publish --self-contained -r %PLATFORM% -c release -o %OUTPUTFOLDER% -p:PublishSingleFile=true
 * 
 * where 
 * %PLATFORM% is something like :  osx-arm64,  osx-x64, win-x64 ecc..
 * %OUTPUTFOLDER% is the path where the executable will be written
 * 
 */

using System.Reflection;
using System.Runtime.Loader;

public static class LauncherMain
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Listing DLLs...");
        List<string> dlls = ListDLLs();

        if (!dlls.Any())
        {
            Console.WriteLine("No dlls to load! Exiting...");
            return;
        }

        Console.WriteLine("Loading DLLs...");

        LoadDLLs(dlls);


        Console.WriteLine("DLLs loaded!\nSearching for HSBMain entry point...");

        //search for the "HSBMain" function in one of the loaded assemblies
        var info = SearchHSBMain();

        if (info == null)
        {
            Console.WriteLine("HSBMain not found! Exiting...");
            return;
        }
        else
        {

            Type? t = info.Item2;
            MethodInfo? m = info.Item1;
            Console.WriteLine("HSBMain found, clearing console and passing execution control to it");
            try
            {
                Console.Clear();
                m!.Invoke(Activator.CreateInstance(t!), new object[] { args });
            }
            catch (Exception e)
            {
                Console.WriteLine("The program launched from the launcher threw an exception so it was terminated, here is the exception:");
                Console.WriteLine(e);

            }
        }

    }

    private static Tuple<MethodInfo?, Type?>? SearchHSBMain()
    {
        //get all loaded assemblies
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //search for the "HSBMain" function in one of the loaded assemblies

        AppDomain currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assems = currentDomain.GetAssemblies().ToList();

        assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
        assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
        assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));
        assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Launcher"));
        assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("HSB"));


        Dictionary<Tuple<string, bool>, Type> routes = new();

        foreach (Assembly assem in assems)
        {
            Console.WriteLine($"Checking : {assem.FullName}");

            try
            {
                List<Type> classes = assem.GetTypes().ToList();
                foreach (var c in classes)
                {
                    MethodInfo? m = c.GetMethod("HSBMain");
                    if (m != null)
                        return new(m, c);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                foreach (Type? t in ex.Types.Where(t => t != null))
                {
                    MethodInfo? m = t!.GetMethod("HSBMain");
                    if (m != null)
                        return new(m, t);

                }
            }


        }
        return null;

    }

    /// <summary>
    /// Loads all dlls in the list
    /// </summary>
    /// <param name="dlls">List of libraries to be loaded</param>    
    private static void LoadDLLs(List<string> dlls)
    {
        foreach (string dll in dlls)
        {
            Assembly loadedAssembly = LoadPlugin(dll);//Assembly.LoadFile(dll);
            Console.WriteLine($"Loaded : {loadedAssembly.FullName}");
        }


    }
    static Assembly LoadPlugin(string path)
    {
        PluginLoadContext loadContext = new(path);
        return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
    }



    /// <summary>
    /// Lists all *.dll files in the launcher directory
    /// </summary>
    /// <returns></returns>
    private static List<string> ListDLLs()
    {
        //get executable path
        string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
        //get directory
        string? dir = Path.GetDirectoryName(path);
        if (dir == null) return new List<string>();
        //get all dlls
        List<string> lst = new();
        foreach (var l in Directory.GetFiles(dir, "*.dll").ToList())
        {
            lst.Add($"{Path.GetFullPath(l)}");
        }


        return lst;
    }
}

/// <summary>
/// <see cref="https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support"/>
/// </summary>
class PluginLoadContext : AssemblyLoadContext
{
    private AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}