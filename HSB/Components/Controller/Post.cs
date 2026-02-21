using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.Components.Controller;

public class Post(string path) : Route(path, HttpMethod.Post)
{
}