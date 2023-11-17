using System.Reflection;
using System.Security.Cryptography;
using HSB;
using HSB.Constants;

namespace Runner;

public class HSBRunner
{

    private static void Main()
    {

        SslConfiguration ssl = new()
        {
            PortMode = HSB.Constants.TLS.SSL_PORT_MODE.DUAL_PORT,
            SslPort = 8081,
            UseDebugCertificate = true,
            UpgradeUnsecureRequests = false
        };

        Configuration c = new()
        {
            Address = "", //listen any address
            Port = 8080,
            RequestMaxSize = Configuration.MEGABYTE * 2,
            CustomServerName = "Runner powered by HSB",
            ListeningMode = HSB.Constants.IPMode.ANY, //valid only if address == "",
            StaticFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static"),
            SslSettings = ssl,
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

        c.GET("/websocketpage", (Request req, Response res) =>
        {
            //return a page to test websocket
            // a text showing the connection status
            //a button that connects to the server
            //the page contains a textarea that displays message from the server
            //a text input to send messages to the server
            // a button to send the message

            string html = @"<html><head></head><body><h1>Websocket test page</h1>
            <div id='status'>Not connected</div>
            <button onclick='connect()'>Connect</button>
            <button onclick='disconnect()'>Disconnect</button>
            <br/>
            <textarea id='messages' row='10' col='15'></textarea>
            <br/>
            <input type='text' id='message' />
            <button onclick='send()'>Send</button>
            <script> 
            var ws = null;
            function connect(){
                ws = new WebSocket('ws://' + window.location.host + '/websocket');
                ws.onmessage = function (event) {
                    var messages = document.getElementById('messages');
                    console.log(event.data);
                    messages.value += event.data + '\n';
                };
                ws.onopen = function (event) {
                    var status = document.getElementById('status');
                    status.innerHTML = 'Connected';
                };
                ws.onclose = function (event) {
                    var status = document.getElementById('status');
                    status.innerHTML = 'Not connected';
                };
            }
            function disconnect(){
                ws.close();
            }
            function send(){
                var message = document.getElementById('message').value;
                ws.send(message);
            }

            </script>
            </body></html>";
            res.SendHTMLContent(html);


        });

        c.GET("/", (Request req, Response res) =>
            {
                //return an html page with all the routes and a link

                string html = "</head><body><h1>Runner HomePage</h1><h3>Available routes:</h3>";
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
                string favicon = "<link rel=\"icon\" type=\"image/png\" href=\"/favicon.ico\" />";

                if ((c.SslSettings.IsEnabled() || c.SslSettings.IsDebugModeEnabled()) && !c.SslSettings.UpgradeUnsecureRequests)
                {
                    html += "<hr/>";
                    int port;
                    if (req.IsTLS)
                    {
                        if (c.SslSettings.PortMode == HSB.Constants.TLS.SSL_PORT_MODE.SINGLE_PORT)
                        {
                            port = c.Port;
                        }
                        else
                        {
                            port = c.SslSettings.SslPort;
                        }

                        html += "<script>function ssl(){ return window.location.href.replace('https', 'http').replace('" + port + "', '" + c.Port + "');}</script>";
                        html += "<a href='javascript:document.location.href=ssl()'>🔓Also available in non-SSL (plain) version </a>";
                        favicon = "<link rel=\"icon\" type=\"image/png\" href=\"/favicon_non_ssl.ico\" />";
                    }
                    else
                    {
                        if (c.SslSettings.PortMode == HSB.Constants.TLS.SSL_PORT_MODE.SINGLE_PORT)
                        {
                            port = c.Port;
                        }
                        else
                        {
                            port = c.SslSettings.SslPort;
                        }

                        html += "<script>function ssl(){ return window.location.href.replace('http', 'https').replace('" + c.Port + "', '" + port + "');}</script>";
                        html += "<a href='javascript:document.location.href=ssl()'>🔐Also available in SSL version </a>";
                        favicon = "<link rel=\"icon\" type=\"image/png\" href=\"/favicon_ssl.ico\" />";
                    }
                }
                html += "<br/><hr><footer>HSB-# Runner &copy; 2021-2023</footer>";
                html += "<script> console.log(window.location.href)</script>";
                html += "</body></html>";
                html = "<html><head>" + favicon + html;
                res.SendHTMLContent(html);
            });


        c.GET("/favicon.ico", (Request req, Response res) =>
        {
            Terminal.INFO("hello");
            var resource = HSB.Utils.LoadResource<byte[]?>("favicon.png");

            if (resource == null) { res.Send(HTTP_CODES.NOT_FOUND); return; }

            res.SendFile(resource, "image/x-icon");
        });
        c.GET("/favicon_ssl.ico", (Request req, Response res) =>
        {

            var resource = HSB.Utils.LoadResource<byte[]?>("favicon_ssl.ico");

            if (resource == null) { res.Send(HTTP_CODES.NOT_FOUND); return; }

            res.SendFile(resource, "image/x-icon");
        });
        c.GET("/favicon_non_ssl.ico", (Request req, Response res) =>
        {
            var resource = HSB.Utils.LoadResource<byte[]?>("favicon_nonssl.ico");
            if (resource == null) { res.Send(HTTP_CODES.NOT_FOUND); return; }
            res.SendFile(resource, "image/x-icon");
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

