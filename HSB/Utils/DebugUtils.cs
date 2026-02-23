using System.Reflection;

namespace HSB.Utils;

public static class DebugUtils
{
    
    public static void PrintLoadedAssemblies(bool filter = true)
    {
        var currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assemblies = [.. currentDomain.GetAssemblies()];
        if (filter)
        {
            assemblies.RemoveAll(a => a.ToString().StartsWith("System"));
            assemblies.RemoveAll(a => a.ToString().StartsWith("Microsoft"));
            assemblies.RemoveAll(a => a.ToString().StartsWith("Internal"));
        }

        foreach (var assem in assemblies)
        {
            List<Type> classes = [.. assem.GetTypes()];

            foreach (var c in classes)
                Terminal.WriteLine(c.FullName, BgColor.White, FgColor.Blue);
        }
    }
}