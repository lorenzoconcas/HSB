using HSB;
using HSB.Constants;
using HSB.OpenApi;
using HSB.Utils;

namespace Runner;

public class Runner
{
    private static void Main()
    {
        SslConfiguration ssl = new()
        {
            PortMode = HSB.Constants.TLS.SSL_PORT_MODE.DUAL_PORT,
            SslPort = 8081,
            UseDebugCertificate = false,
            UpgradeUnsecureRequests = false
        };


        var server = new Server(new Configuration
        {
            Address = "", //listen any address
            Port = 8080,
            RequestMaxSize = Configuration.MEGABYTE * 2,
            ListeningMode = IpMode.Ipv4, //valid only if address == "",
            StaticFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static"),
            //CustomServerName = "Runner powered by HSB",
            SslSettings = ssl,
            OpenApiSettings = new OpenApiSettings()
            {
                Mode = Mode.SwaggerOnly
            }
        });

        var c = server.GetConfiguration();

        //test expressjs-like routing
        //note that these are controlled first, so eventual servlet
        //with same routing will be ignored if they respond to that http method
        //ex a Servlet that responds to the GET call of route "/test" will be ignored
        //but a POST call to the same route will be handled by the servlet

        c.Get("/expressget", TestExpressRoutingGet);
        c.Post("/expresspost", TestExpressRoutingPost);
        c.Get("/printheaders", PrintHeaders);
        c.Get("/echo", Echo);
        c.Post("/echo", Echo);


        c.Get("/500",
            () =>
            {
                throw new Exception("\nThis is a test exception, should only be visible when compiled in debug mode\n");
            });

        c.AddSharedObject("test",
            1996); //this object is available to all servlets, and accessed by "Servlets/SharedObjects.cs" 

        //redirect example
        c.Get("/redirect", (Response res) => { res.Redirect("/"); });

        c.Get("/websocketpage", (Response res) =>
        {
            //return a page to test websocket
            // a text showing the connection status
            //a button that connects to the server
            //the page contains a textarea that displays message from the server
            //a text input to send messages to the server
            // a button to send the message

            const string html = @"<html><head></head><body><h1>Websocket test page</h1>
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
            res.SendHtmlContent(html);
        });

        c.Get("/", (Request req, Response res) =>
        {
            //return an HTML page with all the routes and a link

            var html = "</head><body><h1>Runner HomePage</h1><h3>Available routes:</h3>";
            var routes = server
                .GetRoutes()
                .Where(r => r.Path != "/")
                .ToList();

            html += "<table><thead><tr><th>Route</th><th>Method</th></tr></thead><tbody>";
            //print a table with route link and method
            foreach (var route in routes)
            {
                html = route.SubRoutes.Aggregate(html,
                    (current, subRoute) =>
                        current +
                        $"<tr><td><a href=\"{PathUtils.JoinPath(route.Path, subRoute.Path)}\">{route.Path}</a></td><td>{subRoute.HttpMethod}</td></tr>");
            }

            html += "</tbody></table>";

            //print all available static files
            var staticFilePath = c.StaticFolderPath;
            if (staticFilePath != "" && Directory.Exists(staticFilePath))
            {
                html += "<h3>Available static files:</h3>";
                html += "<table><thead><tr><th>Static files</th></tr></thead><tbody>";
                html = Directory.GetFiles(staticFilePath).Select(file => file.Replace(staticFilePath, ""))
                    .Aggregate(html,
                        (current, filePath) => current + $"<tr><td><a href=\"{filePath}\">{filePath}</a></td></tr>");
            }

            html += "</tbody></table>";
            var favicon = "<link rel=\"icon\" type=\"image/png\" href=\"/favicon.ico\" />";

            if ((c.SslSettings.IsEnabled() || c.SslSettings.IsDebugModeEnabled()) &&
                !c.SslSettings.UpgradeUnsecureRequests)
            {
                html += "<hr/>";
                int port;
                if (req.IsTls)
                {
                    port = c.SslSettings.PortMode == HSB.Constants.TLS.SSL_PORT_MODE.SINGLE_PORT
                        ? c.Port
                        : c.SslSettings.SslPort;

                    html += "<script>function ssl(){ return window.location.href.replace('https', 'http').replace('" +
                            port + "', '" + c.Port + "');}</script>";
                    html +=
                        "<a href='javascript:document.location.href=ssl()'>🔓Also available in non-SSL (plain) version </a>";
                    favicon = "<link rel=\"icon\" type=\"image/png\" href=\"/favicon_ssl.ico\" />";
                }
                else
                {
                    port = c.SslSettings.PortMode == HSB.Constants.TLS.SSL_PORT_MODE.SINGLE_PORT
                        ? c.Port
                        : c.SslSettings.SslPort;

                    html += "<script>function ssl(){ return window.location.href.replace('http', 'https').replace('" +
                            c.Port + "', '" + port + "');}</script>";
                    html += "<a href='javascript:document.location.href=ssl()'>🔐Also available in SSL version </a>";
                    favicon = "<link rel=\"icon\" type=\"image/png\" href=\"/favicon_non_ssl.ico\" />";
                }
            }

            var currentYear = DateTime.Now.Year;
            html += $"<br/><hr><footer>HSB-# Runner &copy; 2021-{currentYear}</footer>";
            html += "<script> console.log(window.location.href)</script>";
            html += "</body></html>";
            html = "<html><head>" + favicon + html;
            res.SendHtmlContent(html);
        });


        c.Get("/favicon.ico", (Response res) =>
        {
            var resource = ResourceUtils.LoadResource<byte[]?>("favicon.png");

            if (resource == null)
            {
                res.Send(HttpCodes.NOT_FOUND);
                return;
            }

            res.SendFile(resource, "image/x-icon");
        });
        c.Get("/favicon_ssl.ico", (Response res) =>
        {
            var resource = ResourceUtils.LoadResource<byte[]?>("favicon_ssl.ico");

            if (resource == null)
            {
                res.Send(HttpCodes.NOT_FOUND);
                return;
            }

            res.SendFile(resource, "image/x-icon");
        });
        c.Get("/favicon_non_ssl.ico", (Response res) =>
        {
            var resource = ResourceUtils.LoadResource<byte[]?>("favicon_nonssl.ico");
            if (resource == null)
            {
                res.Send(HttpCodes.NOT_FOUND);
                return;
            }

            res.SendFile(resource, "image/x-icon");
        });
        server.Start();
    }

    private static void TestExpressRoutingGet(Request req, Response res)
    {
        res.Send("<html><head></head><body onload='loaded()'><h1>Hello there from quick routing</h1>" +
                 "<script src=\"/utils.js\"></script></body></html>", "text/html");
    }

    private static void TestExpressRoutingPost(Request req, Response res)
    {
        res.Send(@"{""value"": 1}", "application/json");
    }

    private static void PrintHeaders(Request req, Response res)
    {
        res.Json(req.Headers);
    }


    private static void Echo(Request req, Response res)
    {
        Console.Clear();
        req.FullPrint();
        res.Send(req.Body);
    }
}