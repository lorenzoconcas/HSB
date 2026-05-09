using System.Reflection;

namespace HSB.Utils;

public static class ClassUtils
{
    public static List<Type> GetClassesWithAttribute<T>() where T : Attribute
    {
        string[] excludeList = ["System", "Microsoft", "Internal"];

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null)
            .Where(a => !excludeList.Any(e => a.FullName!.StartsWith(e)));

        var types = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                types.AddRange(ex.Types.Where(t => t != null)!);
            }
        }

        return types
            .Where(t => t.IsClass)
            .Where(t => t.GetCustomAttribute<T>() != null)
            .ToList();
    }

    public static List<string> ListClassWithPrefix(string prefix)
    {
        string[] excludeList = ["System", "Microsoft", "Internal"];

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null)
            .Where(a => !excludeList.Any(e => a.FullName!.StartsWith(e)));

        var types = new List<Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                types.AddRange(ex.Types.Where(t => t != null)!);
            }
        }

        return types
            .Where(t => t.IsClass)
            .Where(t => t.FullName != null)
            .Where(t => t.FullName!.StartsWith(prefix))
            .Where(t => !Attribute.IsDefined(
                t,
                typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)
            ))
            .Select(t => t.FullName!)
            .ToList();
    }


    public static List<MethodInfo> GetClassMethods<T>(Type type) where T : Attribute
    {
        return type
            .GetMethods()
            .Where(m => m.GetCustomAttribute<T>() != null)
            .ToList();
    }

    public static List<MethodInfo> GetClassMethods(Type type)
    {
        return type
            .GetMethods()
            .ToList();
    }
}