# The Response Class

### Included methods

#### Complex methods

These methods are more used by the library then by users, but are still available to the developer, for example is possibile to bypass the entire elaboration usually made by the library and send what it wants to the client.
All methods have void return type, so it's omitted here.

##### Send methods

| Signature                                                | Description                                                                                                    |
| -------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| `Send(byte[])`                                           | Sends a byte array as response                                                                                 |
| `Send(string, string?, int, Dictionary<string, string>)` | Sends a string a content, with optional mimetype, custom status code (default is 200), optional custom headers |
| `Send(int)`                                              | Send a response with empty body and custom status code, short-hand for SendCode(int)                           |
| `SendCode(int)`                                          | Like Send(int)                                                                                                 |

##### Redirect Methods

| Signature                | Description                                                      |
| ------------------------ | ---------------------------------------------------------------- |
| `Redirect(string, int)`  | Redirect the client to a given url with optional http code       |
| `Redirect(servlet, int)` | Similiar to normal Redirect but with a servlet instead of an url |

#### Shorts and for common HTTP Status response codes

| Signature | Description                 |
| --------- | --------------------------- |
| `E400()`  | Shorthand for SendCode(400) |
| `E401()`  | Shorthand for SendCode(401) |
| `E404()`  | Shorthand for SendCode(404) |
| `E500()`  | Shorthand for SendCode(500) |

#### Common methods

| Signature                                                            | Description                                                                                                                       |
| -------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- | --- |
| `SendHTMLPage(string, bool, Dictionary<string, string>?)`            | Sends and HTML Page loaded from disk as given path, optionally processes it (default false), optional custom headers              |
| `SendHTMLContent(string, bool, string, Dictionary<string, string>?)` | Sends an HTML content, optional processing, optional custom headers, is analog to SendHTMLPage but content is passed as parameter |
| `SendFile(string, string?, int, Dictionary<string, string>?)`        | Send a file loaded from the given path (absolute), optional mimetype (default autodetected), optionl headers                      |     |
| `SendObject(object obj, string)`                                     | Send a generic object to the client, with possibile optimization based on the type of the object                                  |
| `SendFile(byte[], string, int, Dictionary<string, string>)``         | Send a file to the client, passed to the function as byte array, note that you must set the mimeType                              |
| `SendFile(FilePart, int)``                                           | Send a FilePart to the client, with optional http code                                                                            |     |

##### JSON-related response methods

| Signature                               | Description                                                                                                            |
| --------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `JSON(string)`                          | Sends a string as JSON                                                                                                 |
| `JSON<T>(T, bool)`                      | Sends an object that will be serialized and sent as JSON string, boolean to set if include fields or not, default true |
| `JSON<T>(T, JsonSerializerOptions)`     | Same as JSON<T>(T, bool) but with possibily to set option of the serializer                                            |
| `SendJSON(string)`                      | Not-so shorthand for JSON(string)                                                                                      |
| `SendJSON<T>(T, bool)`                  | Not-so shorthand for JSON<T>(T, bool)                                                                                  |
| `SendJSON<T>(T, JsonSerializerOptions)` | Not-so shorthand for JSON<T>(T, JsonSerializerOptions)                                                                 |

##### HTML-processing related response methods

Methods realated to the process of the HTML files, it works like JSP SetAttribute
The values inside this characters: #{} will be replaced

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

| Signature                      | Description                                          |
| ------------------------------ | ---------------------------------------------------- |
| `AddAttribute(string, string)` | Adds a parameter to be replaced with the given value |
| `RemoveAttribute(string)`      | Remove a parameter                                   |
| `GetAttribute(string)`         | Retrieves the value of the given parameter name      |

## Examples

You can use the Runner project as example, or check the Examples folder 
