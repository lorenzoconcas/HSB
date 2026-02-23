namespace HSB.Exceptions;

public class CookieValuesNotSetException : Exception
{
    public CookieValuesNotSetException()
    {
        Terminal.Error("Cookie values not set!");
    }
}