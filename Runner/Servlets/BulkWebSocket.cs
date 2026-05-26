//this class is used to handle a lot of websocket connections

using HSB;
using HSB.Components.Controller;

namespace Runner.Servlets;

[Controller("/bulk_ws.html")]
public class BulkWSPage
{
    private Response res = null!;

    [Get("/")]
    private void Get()
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
        res.SendHtmlContent(html);
    }
}
