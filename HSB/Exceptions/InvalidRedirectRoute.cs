namespace HSB.Exceptions;

public class InvalidRedirectRoute : Exception
{
    public InvalidRedirectRoute()
    {
        Terminal.ERROR("No redirect route specified");
    }
}