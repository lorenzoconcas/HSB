using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.Components.Controller;

public class Delete(string path) : Route(path, HttpMethod.Delete)
{
}