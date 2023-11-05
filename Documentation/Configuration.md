# Configuration Class

This class holds all properties that models the behaviour of HSB

### The Configuration Class

The default Configuration constructor holds the following properties:

| Name                           | Default value                | C# Type                | Description                                                                                                                                                                                        |
| ------------------------------ | ---------------------------- | ---------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Address`                      | `""`                         | string                 | The server will listen by default to all addresses, to restrict to a specific interface set this field. Please note that the value must be coherent with the 'Listening Mode' property             |
| `Port`                         | `8080`                       | int                    | The listening port of the server                                                                                                                                                                   |
| `StaticFolderPath`             | `./static`                   | string                 | This is the folder where the server will attempt to find public files                                                                                                                              |
| `RequestMaxSize`               | `1024` (1KB)                 | int                    | Set the max size of an HTTP request                                                                                                                                                                |
| `DefaultSessionExpirationTime` | `TimeSpan.FromDays(1).Ticks` | long                   | The lifespan in Ticks of the Session                                                                                                                                                               |
| `ListeningMode`                | `ANY`                        | IPMode enum            | Defines the listening mode of the server (IPv4, 6, or both)                                                                                                                                        |
| `ServeEmbeddedResource`        | `false`                      | boolean                | If set embedded resources are treated as static files                                                                                                                                              |
| `EmbeddedResourcePrefix`       | `""`                         | string                 | A string that will be prepended to the requested resource. Ex: if requested res. is '/index.html' and the prefix is set to 'www' the server will search for the assembly resource 'www/index.html' |
| `BlockMode`                    | `NONE`                       | BLOCK_MODE enum        | When is set the server will work in oklist/banlist mode                                                                                                                                            |
| `HideBranding`                 | `false`                      | boolean                | When set the server won't print the HSB logo on startup                                                                                                                                            |
| `CustomServerName`             | `""`                         | string                 | If this string is not empty all HSB related strings are replaced by this value, even in the "Server" response header                                                                               |
| `Debug`                        | `new Debugger()`             | Debugger Class         | Hold all debug information and functions, like logging to disk and to Console                                                                                                                      |
| `SslSettings`                  | `new SslConfiguration()`     | SslConfiguration Class | This class contains all ssl-related settings, by default is not enabled                                                                                                                            |

The configuration class provides also some utilities, like object sharing between servlets (so you can avoid to use the singleton tecnique) and global headers (used to append custom headers to ALL responses)

##### Shared Objects

| Signature                              | Description                                 |
| -------------------------------------- | ------------------------------------------- |
| `void AddSharedObject(string, object)` | Add an object shared between all servlet    |
| `object GetSharedObject(string)`       | Get an object shared between all servlet    |
| `object RemoveSharedObject(string)`    | Remove an object shared between all servlet |

##### Global Headers

| Signature                                                | Description                                                         |
| -------------------------------------------------------- | ------------------------------------------------------------------- |
| `void AddCustomGlobalHeader(string, string)`             | Add an HTTP Response header that will be added to ALL the responses |
| `void RemoveCustomGlobalHeader(string, string)`          | Remove a global HTTP Response header previously added               |
| `void GetCustomGlobalHeader(string, string)`             | Gets the value of a global HTTP Response header previously added    |
| `void Dictionary<string, string> GetCustomGlobalHeaders` | Gets all globabl HTTP Response headers                              |

##### Global Cookies

Cookies added to each request

| Signature                                                | Description                                                        |
| -------------------------------------------------------- | ------------------------------------------------------------------ |
| `void AddCustomGlobalCookie(string, string)`             | Add (Or replaces) a cookie that will be added to ALL the responses |
| `void RemoveCustomGlobalCookie(string, string)`          | Remove a global cookie previously added                            |
| `void GetCustomGlobalCookie(string, string)`             | Gets the value of a global cookie previously added                 |
| `void Dictionary<string, string> GetCustomGlobalCookies` | Gets all global cookies                                            |

#### Other utilities

| Signature                                  | Description                                 |
| ------------------------------------------ | ------------------------------------------- |
| `void GetRawArguments()`                   | Returns the unparsed command line arguments |
| `void HideBrandingOnStartup()`             | Hides the logo printing on startup          |
| `List<Tuple<string, string>> GetAllRoutes` | Return the list of detected routes          |

---
