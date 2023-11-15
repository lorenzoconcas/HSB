namespace HSB.Exceptions;

public class InvalidHttpCodeException : Exception
{
    public InvalidHttpCodeException(int code)
    {
        Terminal.ERROR($"Invalid HTTP redirection code -> {code}");
    }
}

