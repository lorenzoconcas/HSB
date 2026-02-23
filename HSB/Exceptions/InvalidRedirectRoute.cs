namespace HSB.Exceptions;

public class InvalidRedirectRoute : Exception
{
    public InvalidRedirectRoute()
    {
        Terminal.Error("No redirect route specified");
    }
}