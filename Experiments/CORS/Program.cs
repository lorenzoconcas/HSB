using HSB;
var c2 = new Configuration()
{
    Port = 8082
};

c2.GET("/", (Request req, Response res) =>
{
    //an html page that make a fetch request to http://localhost:8081
    res.SendHTMLContent(
        @"<html>
            <head></head>
            <body>
                <pre id='result'></pre>
                <br/>
                <button onclick='fetchData()'>Fetch</button>
                <script>
                    fetchData = () => {
                        let pre = document.getElementById('result');
                        fetch('http://localhost:8081')
                        .then(res => res.text())
                        .then(res => pre.innerHTML = res)
                        .catch(err => pre.innerHTML = err);
                    }
                   
                </script>
            </body>
        </html>"
        
        );
});

var server2 = new Server(c2);

CORS serverCors = new CORS();
serverCors.AllowedOrigins.Add("http://localhost:8082");

var c1 = new Configuration()
{
    Port = 8081,
    //GlobalCORS = serverCors
};

c1.GET("/", (Request req, Response res) =>
{
    res.SetCORS(serverCors);
    //developer should decide if the request should be completed or if it should be rejected
    //How?
    var reject = serverCors.IsRequestAllowed(req);
    if(reject) {
        res.E404();
        return;
    }
    res.JSON(new
    {
        message = "Hello World!"
    });
    return;
});


var server1 = new Server(c1);
new Task(() =>
{
    Thread.Sleep(1000);

    server1.Start();
}).Start();

server2.Start();