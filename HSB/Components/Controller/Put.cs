using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.Components.Controller;

public class Put(string path) : Route(path, HttpMethod.Put)
{
}