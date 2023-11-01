using HSB;

namespace Runner;

public class HSBRunner
{

    private static void Main()
    {


        Configuration c = new()
        {
            Address = "", //listen any address
            Port = 8080,
            RequestMaxSize = Configuration.MEGABYTE * 2,
            CustomServerName = "Runner powered by HSB",
            ListeningMode = HSB.Constants.IPMode.ANY, //valid only if address == "",
            StaticFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "/static")
        };

        //test expressjs-like routing
        //note that these are controlled first, so eventual servlet
        //with same routing will be ignored if they respond to that http method
        //ex a Servlet that responds to the GET call of route "/test" will be ignored
        //but a POST call to the same route will be handled by the servlet

        c.GET("/expressget", TestExpressRoutingGET);
        c.POST("/expresspost", TestExpressRoutingPOST);
        c.GET("/printheaders", PrintHeaders);
        c.GET("/echo", Echo);
        c.POST("/echo", Echo);

        c.AddSharedObject("test", 1996); //this object is available to all servlets, and accessed by "Servlets/SharedObjects.cs" 

        //redirect example
        c.GET("/redirect", (Request req, Response res) =>
        {
            res.Redirect("/");
        });

        c.GET("/", (Request req, Response res) =>
            {
                //return an html page with all the routes and a link

                string html = "<html><head></head><body><h1>Runner HomePage</h1><h3>Available routes:</h3>";
                var routes = c.GetAllRoutes();
                routes.Remove(new Tuple<string, string>("/", "GET"));

                html += "<table><thead><tr><th>Route</th><th>Method</th></tr></thead><tbody>";
                //print a table with route link and method
                foreach (var route in routes)
                {
                    html += $"<tr><td><a href=\"{route.Item1}\">{route.Item1}</a></td><td>{route.Item2}</td></tr>";
                }
                html += "</tbody></table>";

                //print all available static files
                var staticFilePath = c.StaticFolderPath;               
                if (staticFilePath != "" && Directory.Exists(staticFilePath))
                {

                    html += "<h3>Available static files:</h3>";
                    html += "<table><thead><tr><th>Static files</th></tr></thead><tbody>";
                    foreach (var file in Directory.GetFiles(staticFilePath))
                    {
                        var filePath = file.Replace(staticFilePath, "");
                        html += $"<tr><td><a href=\"{filePath}\">{filePath}</a></td></tr>";
                    }
                }
                html += "</tbody></table>";
                html += "<br/><hr><footer>HSB-# Runner &copy; 2021-2023</footer>";
                html += "</body></html>";
                res.SendHTMLContent(html);
            });

        new Server(c).Start();
    }

    private static void TestExpressRoutingGET(Request req, Response res)
    {
        res.Send("<html><head></head><body onload='loaded()'><h1>Hello there from quick routing</h1>" +
            "<script src=\"/utils.js\"></script></body></html>", "text/html");
    }

    private static void TestExpressRoutingPOST(Request req, Response res)
    {
        res.Send(@"{""value"": 1}", "application/json");
    }

    private static void PrintHeaders(Request req, Response res)
    {
        res.JSON(req.Headers);
    }


    private static void Echo(Request req, Response res)
    {
        Console.Clear();
        req.FullPrint();
        res.Send(req.Body);
    }
}

