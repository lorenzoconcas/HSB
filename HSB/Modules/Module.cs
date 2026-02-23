namespace HSB.Modules;

/// <summary>
/// Defines the foundation for a Module
/// Modules permit to extends or improve HSB functionality
/// Note that modules are currently not supported and are still in development, so they may not work as expected
/// </summary> 
public class Module
{
    /// <summary>
    /// defines module priority over other modules
    /// by default is set to 0 and is considered empty
    /// the higher the value, the higher the priority
    /// </summary>
    private readonly uint priority;
    

    public Module()
    {
        this.priority = 0;
    }

    public uint Priority
    {
        get { return priority; }
    }
    
    
    
}