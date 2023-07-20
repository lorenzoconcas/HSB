# Library
Here will be explained how to use the core library (HSB.dll)

### Basic information
This information is up to date until commit [cb7ad79](https://github.com/lorenzoconcas/HSB-Sharp/commit/cb7ad799ded3a7bc948ec6e5f4b1ba2e229d15fa)

To include the library you must follow your IDE instruction

Although HSB is primarily intended for creating complete standalone web servers, it is possible to integrate the library into other types of projects


There are two fundamental components to make it run, the Configuration class and the Server class.
The first holds any information about the server and the second effectively run the server itself. The Server class expects a configuration to be instantied and to run.
So the minimal code to run the server is something like:

```cs
import HSB;

Configuration c = new();
new Server(c).Start();
```
-----
### The Configuration Class
The default Configuration constructor holds the following properties:

| Property name    | Default value | C# Type | Description                                                                                                                                          |
|------------------|---------------|---------|:-----------------------------------------------------------------------------------------------------------------------------------------------------|
| ```address```          | ```127.0.0.1```     | string  | The server will be visibile only to the running machine, to expose it to the network you must provide a valid ip address or pass an empty string (?) |
| ```port```             | ```8080```          | int     | The listening port of the server                                                                                                                     |
| ```staticFolderPath``` | ```./static```      | string  | This is the folder where the server will attempt to find public files                                                                                |
| ```requestMaxSize```   | ```1024``` (1MB)    | int     | Set the max size of an HTTP request                                                                                                                  |
| ```verbose```          | ```true```          | boolean | Whether or not print the log to the console                                                                                                          |

The configuration class provides also some utilities, like object sharing between servlets (so you can avoid to use the singleton tecnique) and global headers (used to append custom headers to ALL responses)

##### Shared Objects

| Signature                                  | Description                              |
|--------------------------------------------|------------------------------------------|
| ```void AddSharedObject(string, object)``` | Add an object shared between all servlet |
| ```object GetSharedObject(string)``` | Get an object shared between all servlet |
| ```object RemoveSharedObject(string)``` | Remove an object shared between all servlet |

##### Global Headers


| Signature                                  | Description                              |
|--------------------------------------------|------------------------------------------|
| ```void AddCustomGlobalHeader(string, string)``` | Add an HTTP Response header that will be added to ALL the responses |
| ```void RemoveCustomGlobalHeader(string, string)``` | Remove a global HTTP Response header previously added |
| ```void GetCustomGlobalHeader(string, string)``` | Gets the value of a global HTTP Response header previously added |
| ```void Dictionary<string, string> GetCustomGlobalHeaders``` |  Gets all globabl HTTP Response headers  |

-----

## API & F.A.Q.

HSB provides different function to create routes and to send replys to the client


### How to define a route?

There are two ways, one is like the nodejs library ExpressJS, the other is follows a more classical approach.


### ExpressJS-like

The configuration class provides 5 methods to handle the various HTTP methods :  `GET; POST; HEAD; PUT; DELETE`
These methods require two arguments, one is the path and the other one is a delegate that will handle the call.
Example:

```cs
using HSB;
Configuration c = new();
c.GET("/", (Request req, Response res) =>{
    res.SendHTMLContent("<h1>Hello World</h1>");
});
new Server(c).Start();
```

will route the index page to that delegate, printing 'Hello World' while visiting the root page


### "Classic" method

To create a routing in the classic method you create a class that extends the Servlet base class, the constructor MUST have two parameters : Request and Response, and pass them to the base class
The new class also needs the **Binding attribute,** that requires a string (the path) and optionally a boolean indicating whether or not that servlet must handle all routes starting with the given path.
Example: 

```cs
import HSB;

namespace Test{
    [Binding("/")]
    public class Route: Servlet{
        
        Route(Request req, Response res): base(req, res){}
        
        public override void ProcessGet(Request req, Response res)
        {
             res.SendHTMLContent("<h1>Hello World</h1>");
        }
    
        public override void ProcessPost(Request req, Response res)
        {
            res.SendHTMLContent("<h1>Hello World</h1>");
        }
    }
}
```

This class maps to the root path and responds to the GET and POST methods, **other methods reply, by default, with code 405 (Method not allowed)**

Additionally the Servlet can have one more argument in is constructor, the configuration:


```cs
import HSB;

namespace Test{
    [Binding("/")]
    public class Route: Servlet{
        
        Route(Request req, Response res, Configuration c): base(req, res, c){}
        
        public override void ProcessGet(Request req, Response res)
        {
             res.SendHTMLContent("<h1>Hello World</h1>");
        }
    
        public override void ProcessPost(Request req, Response res)
        {
            res.SendHTMLContent("<h1>Hello World</h1>");
        }
    }
}
```

# The Request Class

In this class there are some utilities to better handle the request itself


| Signature                                | Type     | Description                                                 |
|------------------------------------------|----------|:------------------------------------------------------------|
| ```HTTP_METHOD METHOD```                      | Property | Returns an enum reppresentating the request method          |
| ```HTTP_PROTOCOL PROTOCOL```                   | Property | Returns an enum reppresentating the request protocol        |
| ```string URL```                               | Property | Returns the request URL                                     |
| ```string RawBody```                           | Property | Returns the raw request body (useful in case like json ecc) |
| ```Dictionary<string, string> GetHeaders```    | Property | Returns the parsed headers of the request                   |
| ```List<string> GetRawHeaders```              | Property | Returns the raw headers of the request                      |
| ```Dictionary<string, string> GetParameters``` | Property | Returns the parameters present in the URL                   |
| ```bool IsJSON()```                         | Function | Returns whether a request contains a json file or not       |



# The Response Class

### Included methods

#### Complex methods
These methods are more used by the library then by users, but are still available, for example user can bypass the entire elaboration usually made by the library and send what it wants to the client.
All methods have void return type, so it's omitted here.

| Signature               | Description                    |
|-------------------------|--------------------------------|
| ```Send(byte[])``` | Sends a byte array as response |
| ```Send(string, string?, int, Dictionary<string, string>)``` | Sends a string a content, with optional mimetype, custom status code (default is 200), optional custom headers|
| ```Send(int)``` | Send a response with empty body and custom status code, short-hand for SendCode(int) |
| ```SendCode(int)``` | Like Send(int) |

#### Shorts and for common HTTP Status response codes

| Signature               | Description                    |
|-------------------------|--------------------------------|
| ```E400()``` | Shorthand for SendCode(400) |
| ```E401()``` | Shorthand for SendCode(401) |
| ```E404()``` | Shorthand for SendCode(404) |
| ```E500()``` | Shorthand for SendCode(500) |


#### Common methods

| Signature               | Description                    |
|-------------------------|--------------------------------|
| ```SendHTMLPage(string, bool, Dictionary<string, string>?)``` | Sends and HTML Page loaded from disk as given path, optionally processes it (default false), optional custom headers |
| ```SendHTMLContent(string, bool, string, Dictionary<string, string>?)``` | Sends an HTML content, optional processing, optional custom headers, is analog to SendHTMLPage but content is passed as parameter |
| ```SendFile(string, string?, int, Dictionary<string, string>?)``` | Send a file loaded from the given path (absolute), optional mimetype (default autodetected), optionl headers |
| ```SendFile(byte[] data, string?, int, Dictionary<string, string>?)``` | Same as the other SendFile, but instead of path takes the byte array to be sent |

##### JSON-related response methods

| Signature               | Description                    |
|-------------------------|--------------------------------|
| ```JSON(string)``` | Sends a string as JSON |
| ```JSON<T>(T, bool)``` | Sends an object that will be serialized and sent as JSON string, boolean to set if include fields or not, default true |
| ```JSON<T>(T, JsonSerializerOptions)``` | Same as JSON<T>(T, bool) but with possibily to set option of the serializer |


##### HTML-processing related response methods
Methods realated to the process of the HTML files, it works like JSP SetAttribute 
The values inside this characters:  #{} will be replaced

```cs
res.AddAttribute("valueToBeReplaced", "value");
```

```html
<h1>#{valueToBeReplaced}</h1>
```
Will result In :

```html
<h1>value</h1>
```
An example usage of this function is in the default pages of the server, located at HSB.DefaultPages, in the classes Index.cs and Error.cs, where the HSB version is set this way

| Signature               | Description                    |
|-------------------------|--------------------------------|
| ```AddAttribute(string, string)``` | Adds a parameter to be replaced with the given value |
| ```RemoveAttribute(string)``` | Remove a parameter |
| ```GetAttribute(string)``` | Retrieves the value of the given parameter name |


## Examples

You can use the TestRunner project as example, more will be added in future