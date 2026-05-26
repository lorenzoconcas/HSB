using HSB;

namespace Runner.Servlets;

public static class RegexRoute
{
    public static void Register(Configuration config)
    {
        config.Get("/regex", (Request req, Response res) =>
        {
            res.Send($@"<h1>Hi</h1><h4> regex-style route migrated to /regex, matched {req.Url}</h4>", "text/html");
        });
    }
}
