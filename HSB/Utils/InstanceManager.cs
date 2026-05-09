namespace HSB.Utils;

public static class InstanceManager<T> where T : class, new()
{
    
    private static T? _instance = null;
    

    public static T GetInstance()
    {
        _instance ??= new T();
        return _instance;
    }
}