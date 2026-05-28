namespace HSB;

public delegate ValueTask MiddlewareNext();
public delegate ValueTask RequestMiddleware(RequestContext context, MiddlewareNext next);

internal delegate ValueTask RequestPipelineDelegate(RequestContext context);

public sealed class RequestContext
{
    private Dictionary<string, object?>? items;

    internal RequestContext(Request request, Response response, Configuration configuration)
    {
        Request = request;
        Response = response;
        Configuration = configuration;
    }

    public Request Request { get; }
    public Response Response { get; }
    public Configuration Configuration { get; }
    public IDictionary<string, object?> Items => items ??= new Dictionary<string, object?>(StringComparer.Ordinal);
}
