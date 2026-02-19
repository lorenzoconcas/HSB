using HSB.Constants;

namespace HSB.Components.Controller;

public class Post(string path) : Route(path, HTTP_METHOD.POST)
{
}