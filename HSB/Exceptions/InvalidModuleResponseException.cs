using HSB.Components;

namespace HSB.Exceptions;

public class InvalidModuleResponseException: Exception
{
    public InvalidModuleResponseException(InternalModule m) : base("Module return an invalid response")
    {
        var message = $"Module return an invalid response blame {m.name} author {m.author}";
        Terminal.Error(message);
    }
    
}