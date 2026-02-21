using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.Components.Controller;

public class Head(string path) : Route(path, HttpMethod.Head)
{
}