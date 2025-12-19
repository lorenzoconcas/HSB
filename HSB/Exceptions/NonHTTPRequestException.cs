namespace HSB.Exceptions;

public class NonHttpRequestException: Exception
{
    public NonHttpRequestException(string message = "") : base(message)
    {
        Terminal.ERROR("Client attempted non-HTTP request: " + message);
    }
}