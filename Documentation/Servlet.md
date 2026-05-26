# Servlet Removal

The servlet-style API has been removed.

Use modern controller routes:

```cs
using HSB.Components.Controller;

[Controller("/")]
public class HomeController
{
    private Response res = null!;

    [Get("/")]
    private void Home()
    {
        res.SendHtmlContent("<h1>Hello world</h1>");
    }
}
```

Or use configuration routes:

```cs
var config = new Configuration();

config.Get("/", (Response res) =>
{
    res.SendHtmlContent("<h1>Hello world</h1>");
});
```

Removed APIs:

| Removed | Replacement |
| ------- | ----------- |
| `Servlet` inheritance | Controller classes or `Configuration` routes |
| `[Binding]` | `[Controller]` + `[Get]`, `[Post]`, ... |
| `GET()` / `POST()` overrides | Controller methods with route attributes |
| `AddCustomMethodHandler` | Explicit route registration for supported HTTP methods |
| `AssociateFile` | `Response.SendHtmlFile(...)` inside a route handler |
