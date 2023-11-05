# Library
Here will be explained how to use the core library (HSB.dll)

### Basic information
This information is up to date until commit [1da2a6f](https://github.com/lorenzoconcas/HSB/commit/1da2a6f0a73124c63f5853276c6f6b3bcef7af64) (5 November 2023)

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

This will make the server listen to any address (IPv4 and IPv6) on port 8080
All parameters are documented [here](./Configuration.md)


## API & Quick F.A.Q.


### How to define a route?

There are two ways, one is like the nodejs library ExpressJS, the other is follows a more classical approach.


### ExpressJS-like

The configuration class provides 9 methods to handle the various HTTP methods :  `GET; POST; HEAD; PUT; DELETE; PATCH; TRACE; OPTIONS; CONNECT`
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

Note that custom HTTP Methods are not supported by this routing style, therefore the Servlet method must be used

### "Classic" method

To create a routing in the classic method you create a class that extends the Servlet base class, the constructor MUST have two parameters : Request and Response, and pass them to the base class
The new class also needs the **Binding Attribute,** that requires a string (the path) and optionally a boolean indicating whether or not that servlet must handle all routes starting with the given path.
Example: 

```cs
import HSB;

namespace Test{
    [Binding("/")]
    public class Route: Servlet{
        
        public Route(Request req, Response res): base(req, res){}
        
        public override void ProcessGet()
        {
             res.SendHTMLContent("<h1>Hello World</h1>");
        }
    
        public override void ProcessPost()
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
        
        public Route(Request req, Response res, Configuration c): base(req, res, c){}
        
        public override void ProcessGet()
        {
             res.SendHTMLContent("<h1>Hello World</h1>");
        }
    
        public override void ProcessPost()
        {
            res.SendHTMLContent("<h1>Hello World</h1>");
        }
    }
}
```

### Custom method handling
To handle a request with a non standard HTTP Request method, yuor server must map the handler function.
For example:
```cs
import HSB;

namespace Test{
    [Binding("/")]
    public class Route: Servlet{
        
        public Route(Request req, Response res, Configuration c): base(req, res, c){
            AddCustomMethodHandler("MyCustomHTTPMethod", ProcessMyCustomHTTPMethod);
        }
        
        private void ProcessMyCustomHTTPMethod()
        {
            res.SendHTMLContent("<h1>Hello</h1>");
        }
        
    }
}
```

This code will handle ONLY the requests that have the method "MyCustomHTTPMethod".
Example request header:
```
MyCustomHTTPMethod / HTTP/1.1
```

Both the ExpressJS-like mapping and Servlet class contains a reference to the "Request" and "Response" class, optionally they can also hold the Configuration.
Details about the Request class can be found [here](./Request.md), while for Response [here](./Response.md)

All information about ExpressJS mapping and Servlet are available [here](./servlets.md)

----------
## SSL 
To make HSB use the SSL/TLS mode you must provide a valid certificate and modify the configuration. See [set SSL ](./SSL.md) for more information


## WebSocket
HSB support the creation of WebSockets, these follow a structure similar to the servlets but with different functions for sending data
A complete guide is available [here](./WebSockets.md) 