using HSB.Constants;

namespace HSB.Components.Controller;

public class Get(string path) : Route(path, HTTP_METHOD.GET)
{
}