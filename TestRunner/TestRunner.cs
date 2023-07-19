using System;
using HSB;
namespace TestRunner
{
    public class HSBTestRunner
    {
        private static void Main(string[] args)
        {
            Configuration c = new()
            {
                address = "127.0.0.1",
                port = 8080
            };


            //test expressjs-like routing
            //note that these are controlled first, so eventual servlet
            //with same routing will be ignored if they respond to that http method
            //ex a Servlet that responds to the GET call of route "/test" will be ignored
            //but a POST call to the same route will be handled by the servlet
            c.GET("/expressget", TestExpressRoutingGET);
            c.POST("/expresspost", TestExpressRoutingPOST);
            c.AddSharedObject("test", 1996);
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

    }


}

