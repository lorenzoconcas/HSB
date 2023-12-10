//this class is used to handle a lot of websocket connections

using HSB;
using HSB.Components.WebSockets;

namespace Runner.Servlets;

[Binding("/bulk_ws.html")]
public class BulkWSPage(Request req, Response res) : Servlet(req, res)
{

    public override void ProcessGet()
    {
        //returns and html page that spawns 20 websockets connections
        //and send a message to the server every 2 seconds
        var html = @"<html>
                        <head>
                            <script>
                                var ws = [];
                                for(let i = 0; i < 20; i++){
                                    let wsx =  new WebSocket('ws://localhost:8080/ws_bulk');
                                    wsx.onopen = function(){
                                        console.log('opened');
                                    }
                                    wsx.onmessage = function(e){
                                        console.log(e.data);
                                    }
                                    wsx.onclose = function(){
                                        console.log('closed');
                                    }   

                                    setTimeout(function(){
                                        setInterval(function(){
                                            if(wsx.readyState == 1){
                                                console.log('sending message from : ' + i)
                                            wsx.send('hello from ' + i );
                                            }
                                        }, 100 * i);
                                    
                                    }, 100 * i);  

                                    ws.push(wsx);                            
                                }

                               
                            </script>
                        </head>
                        <body>
                            <h1>Websocket Test</h1>
                        </body>
                    </html>";
        res.SendHTMLContent(html);
    }
}

//web socket handler
[Binding("/ws_bulk")]
public class BulkWebSocket(Request req, Response res, Configuration c) : WebSocket(req, res, c)
{

    public override void OnOpen()
    {
        Console.WriteLine("opened");
    }

    public override void OnMessage(Message message)
    {
        Console.WriteLine(message.GetMessage());
    }

    public override void OnClose()
    {
        Console.WriteLine("closed");
    }
}
