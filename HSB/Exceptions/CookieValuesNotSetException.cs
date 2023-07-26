
namespace HSB.Exceptions
{
    public class CookieValuesNotSetException : Exception
    {
        public CookieValuesNotSetException()
        {
            Terminal.ERROR("Cookie values not set!");
        }
    }
}

