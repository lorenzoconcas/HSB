# The Request Class

In this class there are some utilities to better handle the request itself

| Name                        | Return Type                | Type     | Description                                                        |
| --------------------------- | -------------------------- | :------- | :----------------------------------------------------------------- |
| `METHOD`                    | HTTP_METHOD                | Property | The request method, if custom method is used this valus is UNKNOWN |
| `PROTOCOL`                  | HTTP_PROTOCOL              | Property | The request http procol (ex 1.1)                                   |
| `URL`                       | string                     | Property | The request URL                                                    |
| `Body`                      | string                     | Property | The body of the request, in a UTF-8 string                         |
| `ClientIP`                  | string                     | Property | The ip of the client that made the request                         |
| `ClientIPVersion`           | AddressFamily              | Property | The ip version (4 or 6) of the client                              |
| `ClientPort`                | int                        | Property | The port of the client that made the request                       |
| `DumpBody()`                | bool                       | Function | Write the body to a file named 'body.txt'                          |
| `DumpRequest()`             | bool                       | Function | Write the request to a file named 'request.txt'                    |
| `FullPrint()`               | void                       | Function | Print all information of the request to console                    |
| `GetBasicAuthInformation()` | Tuple<string, string>?     | Function | Returns the information of the basic auth, if present              |
| `GetFormData()`             | Form?                      | Function | Return the formdata, if present and the request is FormUpload()    |
| `GetMultiPartFormData()`    | MultiPartFormData?         | Function | Return the formdata, if present and the request is FileUpload()    |
| `GetOAuth1_0Information()`  | OAuth1_0Information?       | Function | Returns the information of the OAuth1.0, if present                |
| `GetRawRequestText()`       | string                     | Function | Returns the unparsed text of the request                           |
| `GetSession()`              | Session                    | Function | Returns the session associated with the request                    |
| `GetSocket()`               | Socket                     | Function | Returns the connection socket                                      |
| `RawBody`                   | string                     | Property | Returns the raw request body (useful in case like json ecc)        |
| `Headers`                   | Dictionary<string, string> | Property | Returns the parsed headers of the request                          |
| `IsAjaxRequest()`           | bool                       | Function | True if request contains ""X-Requested-With"                       |
| `IsFileUpload()`            | bool                       | Function | True if request contains "multipart/form-data"                     |
| `IsFormUpload()`            | bool                       | Function | True if request contains "application/x-www-form-urlencoded"       |
| `IsJSON()`                  | bool                       | Function | Returns whether a request contains a json file or not              |
| `IsWebSocket()`             | bool                       | Function | Returns whether a request is a websocket connection request        |
| `Parameters`                | Dictionary<string, string> | Property | Returns the parameters present in the URL                          |
| `RawBody`                   | byte[]                     | Property | Returns the raw body of the request                                |
| `RawHeaders`                | List\<string\>             | Property | Returns the raw headers of the request                             |
