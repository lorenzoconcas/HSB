using System.Reflection;
using HSB.Utils;

namespace HSB.Components;

public class ModuleInfo(string name, string author, string description)
{
    public readonly string name = name;
    public readonly string author = author;
    public readonly string description = description;
}

public enum ModuleExitCode
{
    Continue,
    Success,
    Error,
    Reject,
    //  Close
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class Module(ModuleType type, uint priority = 0, string name = "", string author = "", string description = "")
    : Attribute
{
    /// <summary>
    /// defines module priority over other modules
    /// by default is set to 0 and is considered empty
    /// the higher the value, the higher the priority
    /// </summary>
    public uint priority = priority;

    public readonly ModuleType type = type;
    public readonly ModuleInfo info = new ModuleInfo(name, author, description);
}

[AttributeUsage(AttributeTargets.Method)]
public class ModuleInvokeMethod() : Attribute
{
}

public enum ModuleType
{
    Global, //modules like Debugger
    RequestInterceptor, //CORS, Filter. Attached in Server.ProcessRequest(Request, Response)
    RequestHandlerInterceptor, //Authentication checker
   // ResponseInterceptor, //Response Shaper, attached to Send(byte[], bool) method
    Service, //Swagger/OpenApi, File List Page, Index Page
}

public class InternalModule(Type type, ModuleType moduleType, MethodInfo method, ModuleInfo info)
    : ModuleInfo(info.name, info.author, info.description)
{
    public readonly Type type = type;
    public readonly ModuleType moduleType = moduleType;
    public readonly MethodInfo method = method;
}

public class ModuleManager
{
    private readonly List<InternalModule> _modules = [];

    public ModuleManager()
    {
        //collect al classes with module attribute
        var classes = ClassUtils.GetClassesWithAttribute<Module>();
        //map to InternalModule[]

        foreach (var cls in classes)
        {
            var method = ClassUtils.GetClassMethods<ModuleInvokeMethod>(cls);
            if (method.Count == 0)
            {
                break;
            }

            var attr = cls.GetCustomAttribute<Module>()!;
            _modules.Add(new InternalModule(cls, attr.type, method[0], attr.info));
        }
    }


    public List<InternalModule> GetModules(ModuleType type, List<string> include)
    {
        return _modules
            .Where(cls => cls.moduleType == type)
            .Where(cls => include.Contains(cls.type.FullName ?? cls.name))
            .ToList();
    }
}