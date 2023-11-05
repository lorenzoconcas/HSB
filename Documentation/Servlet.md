## Servlet class

The servlet is the core of the server, inside is defined the logic of the pages

The simplest servlet looks like this:

```cs
using HSB;

namespace Example
{
    [Binding("/")] //route the root page
    public class HelloWorld : Servlet
    {
        public HelloWorld(Request req, Response res) : base(req, res)
        {
        }

        //we override the function that handle the GET response processing
        public override void ProcessGet()
        {
            res.SendHTMLContent("<h1>Hello world</h1>");
        }

    }
}
```

This example prints an "Hello World" message when the root page is visited
Every method defined in HTTP_METHODS has an ovveridable event, for custom method the `AddCustomMethodHandler` must be used

### Events

| Name             | Trigger by which HTTP_METHOD |
| ---------------- | ---------------------------- |
| ProcessGet()     | GET                          |
| ProcessPOST()    | POST                         |
| ProcessDelete()  | DELETE                       |
| ProcessPut()     | PUT                          |
| ProcessHead()    | HEAD                         |
| ProcessPatch()   | PATCH                        |
| ProcessOptions() | OPTIONS                      |
| ProcessTrace()   | TRACE                        |
| ProcessConnect() | CONNECT                      |

By default, if a method is not overridden, it response `HTTP_CODES.METHOD_NOT_ALLOWED` (405)

### Custom Methods

| Name                                     | Description                                               |
| ---------------------------------------- | --------------------------------------------------------- |
| AddCustomMethodHandler(string, Delegate) | Defines an handler for the custom method passed as string |
| RemoveCustomMethodHandler(string)        | Remove a previously defined handler                       |

At the moment is possibile to specify only ONE custom method handler

### Attributes
#### BindingAttribute
To be mapped the servlet must use the `Binding Attribute`

```cs
[BindingAttribute(string path, bool startsWith = false)]
```
Without this the servlet won't be served because there is no route!
The second paramter indicates if all the routes that start with the path must be catched, else only the exact path will respond.
Example 

if startsWith == false and path == "/route"

"/route" -> served 
"/route/2" -> not served 

if startsWith == true and path = "/route"

"/route" -> served 
"/route/2" -> served 

----
#### AssociatedFile
This attributes makes the servlet behave like a static file, for example
if path is set to "index.html", when the this path is requested the servlet is called instead of serving (if enabled) a static file 

```cs
[AssociatedFile(string path, HTTP_METHOD method = HTTP_METHOD.GET)]
[AssociatedFile(string path, HTTP_METHOD[] method)]
[AssociatedFile(string path, string customMethod)]
[AssociatedFile(string path, string[] customMethod)]
```
