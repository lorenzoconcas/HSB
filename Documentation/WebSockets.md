# WebSockets

WebSockets are instantiated like servlets, but instead of extending the Servlet class, they need to extend `HSB.Components.WebSockets.WebSocket`

The following example specifies a websocket mapped to `/websocket`, note that you still need to provide an html and javascript code for the user

```cs
using HSB;
using HSB.Components.WebSockets;


[Binding("/websocket")]
public class WebSocketHandler : WebSocket
{
    public WebSocketHandler(Request req, Response res, Configuration c) : base(req, res, c){}

    public override void OnOpen()
    {
        Terminal.INFO("New websocket connection opened");
    }

    public override void OnMessage(Message msg)
    {
        Terminal.DEBUG($"Got message : {msg.GetMessage()}");
        //echo the message
        Send(msg);

    }

    public override void OnClose()
    {
        Terminal.INFO("Websocket disconnected");
    }
}
```

### Send methods

| Name                           | Description                                                                          |
| ------------------------------ | ------------------------------------------------------------------------------------ |
| `SetMessageSentOnOpen(byte[])` | Set a message that will be sent to the client once the connection is opened          |
| `Send(byte[])`                 | Sends a binary message to the client                                                 |
| `Send(string)`                 | Send a string message to the client                                                  |
| `Send(Message)`                | Sends a message to the client based on the `HSB.Components.WebSockets.Message` class |
| `Send<T>(T, bool)`             | Sends a C# object to the client that will be serialized as JSON                      |

### Connection control methods

| Name                             | Return Type      | Description                                         |
| -------------------------------- | ---------------- | --------------------------------------------------- |
| `Close()`                        | `void`           | Close the websocket                                 |
| `SetConnectionRequirements(...)` | `void`           | Sets requirements to accept the connection requests |
| `GetState()`                     | `WebSocketState` | Return the current state of the websocket           |

#### SetConnectionRequirements parameters

| Parameter             | Type                         | Description                                                                         |
| --------------------- | ---------------------------- | ----------------------------------------------------------------------------------- |
| `requiredHeaders`     | `Dictionary<string, string>` | Sets the headers that must be present in the initial request                        |
| `requiredParameter`   | `Dictionary<string, string>` | Like requiredHeaders                                                                |
| `bearerToken`         | `string`                     | If set, a bearer token is required to estabilish the websocket connection           |
| `oAuth2Token`         | `string`                     | If set, an oAuth2.0 token is required to estabilish the websocket connection        |
| `basicAuth`           | `Tuple<string, string>?`     | If set, a basic auth information is required to estabilish the websocket connection |
| `oAuth1_0Information` | `OAuth1_0Information?`       | If set, a oAuth1.0 information is required to estabilish the websocket connection   |

### Events

| Name                 | Description                                             |
| -------------------- | ------------------------------------------------------- |
| `OnMessage(Message)` | It's called when a messages is received from the client |
| `OnOpen()`           | It's triggered when the connection opens                |
| `OnClose()`          | Triggered after a close request is received             |
