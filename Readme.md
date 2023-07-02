# HSB-#
*HSB-Sharp*
## 🇬🇧🇺🇸 Description

This is a toy project written to study how a web server works

It is written in C-Sharp, compiles and runs on any platform where the dotnet tool is available

## 🇮🇹 Descrizione

Questo è un progetto giocattolo scritto per studiare il funzionamento di un server web

E' scritto in C-Sharp, compila e gira su ogni piattaforma in cui è disponibile il tool dotnet

-----

## 🇬🇧🇺🇸 How to use this as library


First of all you need to include the library as dipendency, in Visual Studio for Windows you must add it as Shared COM Object and load the DLL.
After that you can use it in any C# Project.

There are two fundamental components to make it run, the Configuration class and the Server class.
The first holds any information about the server and the second effectively run the server itself. The Server class expects a configuration to be instantied and to run.
So the minimal code to run the server is something like:

```
import HSB;

Configuration c = new();
_ = new Server(c);
```

The server will start as soon as you instantiate the class

The default Configuration constructor set the following properties:

`address : string = 127.0.0.1` (localhost), the server will be visibile only to the running machine, to expose it to the network you must provide a valid ip address or pass an empty string (?)

`port : string = 8080`

`staticFolderPath : string = "./static"`, this is the folder where the server will attempt to find public files

`requestMaxSize : int = 1024`, set the max size of an HTTP request

`verbose : boolean = true` -> whether or not print the log to the console

-------

## How to use it standalone?

You must first download the solution then build the project "Standalone"
When you run it without arguments an empty default configuration will be created and written to disk, as "config.json" file.
All valid arguments are available passing the "--info" argument like this:
`HSB_Standalone --info`

The standalone launcher can load assemblies passed as argument, without the needing of recompilation, however you won't be habel to use the ExpressJS-like routing

## How to run tests?

Similar to standalone, download the solution then run the "TestRunner" project

----
# API Info


## How to define a route?

There are two ways, one is like the nodejs library ExpressJS, the other is follows a more classical approach.

### ExpressJS-like

The configuration class provides 5 methods to handle the various HTTP methods :  `GET; POST; HEAD; PUT; DELETE`
These methods require two arguments, one is the path and the other one is a delegate that will handle the call.
Example:

```
using HSB;
Configuration c = new();
c.GET("/", (Request req, Response res) =>{
    res.SendHTMLContent("<h1>Hello World</h1>");
});
_ = new Server(c);
```

will route the index page to that delegate, printing 'Hello World' while visiting the root page


### "Classic" method

To create a routing in the classic method you create a class that extends the Servlet base class, the constructor MUST have two parameters : Request and Response, and pass them to the base class
The new class also needs the Binding attribute, that requires a string (the path) and optionally a boolean indicating whether or not that servlet must handle all routes starting with the given path.
Example: 

```
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

This class maps to the root path and responds to the GET and POST methods,
other methods reply, by default, with code 405 (Method not allowed)


## The Response Class

### Included methods
```
void Send(byte[]) : Sends a byte array as response
void Send(string, string?, int, Dictionary<string, string>) : Sends a string a content, with optional mimetype, custom status code (default is 200), optional custom headers

void Send(int) : Send a response with empty body and custom status code, short-hand for SendCode(int)
void SendCode(int) : Like Send(int)

void E400() : Shorthand for SendCode(400)
void E401() : //
void E404() : //
void E500() : //

void SendHTMLPage(string, bool, Dictionary<string, string>?) : Sends and HTML Page loaded from disk as given path, optionally processes it (default false), optional custom headers
void SendHTMLContent(string, bool, string, Dictionary<string, string>?) : Sends an HTML content, optional processing, optional custom headers, is analog to SendHTMLPage but content is passed as parameter
void SendFile(string, string?, int, Dictionary<string, string>?) : Send a file loaded from the given path (absolute), optional mimetype (default autodetected), optionl headers
void SendFile(byte[] data, string?, int, Dictionary<string, string>?) : Same as the other SendFile, but instead of path takes the byte array to be sent

public void JSON(string) : Sends a string as JSON
public void JSON<T>(T, bool) : Sends an object that will be serialized and sent as JSON string, boolean to set if include fields or not, default true
public void JSON<T>(T, JsonSerializerOptions) : Same as JSON<T>(T, bool) but with possibily to set option of the serializer
```
Methods realated to the process of the HTML files, it works like JSP SetAttribute 
The values inside this characters:  #{} will be replaced
Example : 
```
//C# Code
res.AddAttribute("valueToBeReplaced", "value");
```

```
//HTML Code
<h1>#{valueToBeReplaced}</h1>
```
Will result In :

```
//HTML Code
<h1>value</h1>
```

An example usage of this function is in the default pages of the server, located at HSB.DefaultPages, in the classes Index.cs and Error.cs, where the HSB version is set this way

Methods
```
public void AddAttribute(string, string) : Adds a parameter to be replaced with the given value
public void RemoveAttribute(string) : Remove a parameter
public void GetAttribute(string) : Retrieves the value of the given parameter name
```

## Examples
You can base your code on files of TestRunner

