namespace HSB.Exceptions;

public class InvalidHttpCodeException : Exception
{
    public InvalidHttpCodeException(int code)
    {
        Terminal.Error($"Invalid HTTP redirection code -> {code}");
    }
}

