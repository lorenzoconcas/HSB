using HSB;
using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace Runner;


[Binding("/associatedfile1")]
[AssociateFile("./static/main.html", HttpMethod.Get)]
public class AssociatedFileServletOneMethod : Servlet
{
    public AssociatedFileServletOneMethod(Request req, Response res) : base(req, res)
    {

    }
}
// Test these with postman, curl or custom scripts/pages

[Binding("/associatedfile2")]
[AssociateFile("./static/main.html", new HttpMethod[] { HttpMethod.Get, HttpMethod.Trace })]
public class AssociatedFileServletMultipleMethods : Servlet
{
    public AssociatedFileServletMultipleMethods(Request req, Response res) : base(req, res)
    {

    }
}

//test with postman, curl or custom scripts/pages
[Binding("/associatedfile3")]
[AssociateFile("./static/main.html", "HSBCUSTOMHTTPMETHOD")]
public class AssociatedFileServletCustomMethods : Servlet
{
    public AssociatedFileServletCustomMethods(Request req, Response res) : base(req, res)
    {

    }
}

[Binding("/associatedfile4")]
[AssociateFile("./static/main.html", new string[] { "GET", "HSBCUSTOMHTTPMETHOD" })] //standard methods are still valid
public class AssociatedFileServletMultipleCustomMethods : Servlet
{
    public AssociatedFileServletMultipleCustomMethods(Request req, Response res) : base(req, res)
    {

    }
}
