using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.Components.Controller;

public class Patch(string path) : Route(path, HttpMethod.Patch)
{
}