using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.Components.Controller;

public class Get(string path) : Route(path, HttpMethod.Get)
{
}