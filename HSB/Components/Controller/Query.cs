using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.Components.Controller;

/// <summary>
/// Declares a safe, idempotent HTTP QUERY route whose query is supplied in the request body.
/// </summary>
public class Query(string path) : Route(path, HttpMethod.Query)
{
}
