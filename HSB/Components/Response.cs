using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using HSB.Constants;
using HSB.Components;
using HSB.Exceptions;
using System.Net.Security;

namespace HSB;

public class Response(Socket socket, Request request, Configuration c, SslStream? sslStream)
{
    private const string NEW_LINE = "\r\n";

    private readonly Socket socket = socket;
    private SslStream? sslStream = sslStream;
    private readonly Request request = request;
    private readonly Configuration config = c;
    readonly Dictionary<string, string> attributes = [];

    private CORS? cors = null;


    #region Global Send methods

    /// <summary>
    /// Send an un modified byte array to the socket
    /// </summary>
    /// <param name="data"></param>
    /// <param name="disconnect"></param>
    public void Send(byte[] data, bool disconnect = true)
    {
        try
        {
            if (sslStream != null)
            {
                sslStream.Write(data);
                if (disconnect)
                    sslStream.Close();
            }
            else
            {
                var totalBytes = socket.Send(data);
                if (disconnect)
                    socket.Disconnect(disconnect);
            }

            data = [];
        }
        catch (Exception e)
        {
            Terminal.ERROR($"Error sending data ->\n {e}");
        }
    }


    /// <summary>
    /// Sends an HTTP Response with the body passed as parameter
    /// </summary>
    /// <param name="data">Body of the response</param>
    /// <param name="mimeType">MimeType of the body</param>
    /// <param name="statusCode">Response status code</param>
    public void Send(string data, string mimeType = "text/plain", int statusCode = HTTP_CODES.OK,
        Dictionary<string, string>? customHeaders = null)
    {
        string _mime = mimeType;

        string resp = GetHeaders(statusCode, Encoding.UTF8.GetBytes(data).Length, _mime, customHeaders) + data;

        Send(Encoding.UTF8.GetBytes(resp));
    }

    /// <summary>
    /// Loads and HTML file from path and sends it as HTTP Response with mimeType = text/html
    /// Optionally it can provides a basic processor function
    /// </summary>
    /// <param name="path">Path of the HTML file</param>
    /// <param name="process">Whether or not or not process the document before sending</param>
    public void SendHTMLFile(string path, bool process = false, Dictionary<string, string>? customHeaders = null)
    {
        try
        {
            string content = File.ReadAllText(path);
            if (process)
                content = ProcessContent(content);
            Encoding encoding = Utils.GetEncoding(path);

            Send(content, MimeTypeUtils.TEXT_HTML + $"; charset={encoding.BodyName}", customHeaders: customHeaders);

            content = "";
        }
        catch (Exception)
        {
            //dato che l'invio dei dati è parte nostra, se non riusciamo diamo un errore 500
            SendCode(HTTP_CODES.INTERNAL_SERVER_ERROR);
            Terminal.ERROR("Error sending file : " + path);
        }
    }

    /// <summary>
    /// Sends an html page passed as string
    /// </summary>
    /// <param name="path">HTML content</param>
    /// <param name="process">Whether or not or not process the document before sending</param>
    /// <param name="statusCode">Response status code</param>
    /// <param name="encoding">Encoding of the document</param>
    /// <param name="customHeaders">Optional headers</param>
    public void SendHTMLContent(string content, bool process = false, int statusCode = HTTP_CODES.OK,
        string encoding = "UTF-8", Dictionary<string, string>? customHeaders = null)
    {
        if (process)
            content = ProcessContent(content);
        Send(content, MimeTypeUtils.TEXT_HTML + $"; charset={encoding}", statusCode, customHeaders);
    }

    /// <summary>
    /// Loads a file from a given path and sends an HTTP Response
    /// </summary>
    /// <param name="absPath">Path (absolute) of the file</param>
    /// <param name="mimeType">MimeType of the file</param>
    /// <param name="statusCode">Response status code</param>
    public void SendFile(string absPath, string? mimeType = null, int statusCode = HTTP_CODES.OK,
        Dictionary<string, string>? customHeaders = null)
    {
        var data = File.ReadAllBytes(absPath);

        string _mime = mimeType ??
                       MimeTypeUtils.GetMimeType(Path.GetExtension(absPath)) ?? MimeTypeUtils.APPLICATION_OCTET;
        string headers = GetHeaders(statusCode, data.Length, _mime, customHeaders);
        byte[] headersBytes = Encoding.UTF8.GetBytes(headers);
        byte[] responseBytes = new byte[data.Length + headersBytes.Length];

        headersBytes.CopyTo(responseBytes, 0);
        data.CopyTo(responseBytes, headersBytes.Length);

        Send(responseBytes);

        //clear memory after sending
        data = [];
        headersBytes = [];
        responseBytes = [];
    }

    /// <summary>
    /// Sends data to the client
    /// </summary>
    /// <param name="data"></param>
    /// <param name="mimeType"></param>
    /// <param name="statusCode"></param>
    public void SendFile(byte[] data, string mimeType, int statusCode = HTTP_CODES.OK,
        Dictionary<string, string>? customHeaders = null)
    {
        string _mime = mimeType;
        string headers = GetHeaders(statusCode, data.Length, _mime, customHeaders);
        byte[] headersBytes = Encoding.UTF8.GetBytes(headers);
        byte[] responseBytes = new byte[data.Length + headersBytes.Length];

        headersBytes.CopyTo(responseBytes, 0);
        data.CopyTo(responseBytes, headersBytes.Length);

        Send(responseBytes);
        //clear memory after sending
        data = [];
        headersBytes = [];
        responseBytes = [];
    }

    /// <summary>
    /// Sends a generic object to the client, with possible optimization (string, byte[], FilePart, generic object)
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="fileName"></param>
    /// <param name="statusCode"></param>
    /// <param name="customHeaders"></param>
    /// <param name="mime"></param>
    public void SendObject(object? obj, string fileName = "", int statusCode = 200, string mime = "text/plain", Dictionary<string, string>? customHeaders = null)
    {
        switch (obj)
        {
            case null:
                return;
            case string str when fileName != "":
                Send(str, MimeTypeUtils.GetMimeType(Path.GetExtension(fileName)) ?? mime, statusCode, customHeaders);
                break;
            case string str:
                Send(str, mimeType: mime, statusCode: statusCode, customHeaders: customHeaders);
                break;
            case byte[] bytes:
                Send(bytes);
                break;
            case FilePart filePart:
                SendFile(filePart, statusCode, customHeaders);
                break;
            default:
                SendJSON(obj, true, statusCode);
                break;
        }
    }

    /// <summary>
    /// Send a FilePart to the client
    /// </summary>
    /// <param name="filePart"></param>
    /// <param name="statusCode"></param>
    /// <param name="customHeaders"></param>
    public void SendFile(FilePart filePart, int statusCode = HTTP_CODES.OK,
        Dictionary<string, string>? customHeaders = null)
    {
        SendFile(filePart.GetBytes(), filePart.GetMimeType(), statusCode, customHeaders);
    }

    /// <summary>
    /// Send an HTTP Response with no body but with given status code
    /// </summary>
    /// <param name="statusCode"></param>
    public void SendCode(int statusCode)
    {
        var resp = GetHeaders(statusCode, 0, MimeTypeUtils.TEXT_PLAIN) + NEW_LINE;

        Send(Encoding.UTF8.GetBytes(resp));
    }

    /// <summary>
    /// Shorthand for SendCode
    /// </summary>
    /// <param name="statusCode"></param>
    public void Send(int statusCode)
    {
        SendCode(statusCode);
    }

    #endregion

    #region Redirect methods
    /// <summary>
    /// Sends a redirect to the client
    /// </summary>
    /// <param name="route"></param>
    /// <param name="statusCode"></param>
    public void Redirect(string route, int statusCode = HTTP_CODES.FOUND)
    {
        if (statusCode < 300 || statusCode > 399)
            throw new InvalidHttpCodeException(statusCode);

        var response = GetHeaders(
            statusCode,
            0,
            MimeTypeUtils.TEXT_PLAIN,
            new Dictionary<string, string>() {{"Location", route}}
        ) + NEW_LINE;

        Send(Encoding.UTF8.GetBytes(response));
    }

    /// <summary>
    /// Redirects to a given servlet
    /// </summary>
    /// <param name="s"></param>
    /// <param name="statusCode"></param>
    public void Redirect(Servlet s, int statusCode = HTTP_CODES.FOUND)
    {
        Redirect(s.GetRoute(), statusCode);
    }

    #endregion

    #region Staus codes shorthand methods

    //common status codes
    /// <summary>
    /// Bad Request
    /// </summary>
    public void E400()
    {
        SendCode(HTTP_CODES.BAD_REQUEST);
    }

    /// <summary>
    /// Unauthorized
    /// </summary>
    public void E401()
    {
        SendCode(HTTP_CODES.UNAUTHORIZED);
    }

    /// <summary>
    /// Not Found
    /// </summary>
    public void E404()
    {
        SendCode(HTTP_CODES.NOT_FOUND);
    }

    /// <summary>
    /// Internal Server Error
    /// </summary>
    public void E500()
    {
        SendCode(HTTP_CODES.INTERNAL_SERVER_ERROR);
    }

    #endregion
    
    #region Json sending related methods
    /// <summary>
    /// Sends an HTTP Response with a JSON body passed as parameter
    /// </summary>
    /// <param name="content">String of the body in JSON format</param>
    /// <param name="statusCode">Set a custom status code if provided
    public void JSON(string content, int statusCode)
    {
        Send(content, "application/json", statusCode);
    }
    
    /// <summary>
    /// Sends an HTTP response with a JSON body 
    /// </summary>
    /// <param name="content"></param>
    public void JSON(string content)
    {
        Send(content, "application/json");
    }

    /// <summary>
    /// Serializes and sends an Object in JSON format
    /// </summary>
    /// <param name="o">Object to be serialized and sended as response</param>
    /// <param name="options">Options for the serializer (System.Text.Json.JsonSerializer)</param>
    public void JSON<T>(T o, JsonSerializerOptions options, int statusCode = HTTP_CODES.OK)
    {
        JSON(JsonSerializer.Serialize(o, options), statusCode);
    }

    /// <summary>
    /// Serialize and sends an Object in JSON Format
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="o"></param>
    /// <param name="includeFields">Whether include fields of the object</param>
    /// <param name="writeIndented"></param>
    /// <param name="statusCode"></param>
    public void JSON<T>(T o, bool includeFields = true, bool writeIndented = true, int statusCode = HTTP_CODES.OK)
    {
        JsonSerializerOptions jo = new()
        {
            IncludeFields = includeFields,
            MaxDepth = 0,
            WriteIndented = writeIndented
        };

        JSON(JsonSerializer.Serialize(o, jo), statusCode);
    }

    /// <summary>
    /// Alternate name for function JSON
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="o"></param>
    /// <param name="options"></param>
    public void SendJSON<T>(T o, JsonSerializerOptions options) => JSON(o, options);

    /// <summary>
    /// Alternate name for function JSON
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="o"></param>
    /// <param name="includeFields"></param>
    public void SendJSON<T>(T o, bool includeFields = true) => JSON(o, includeFields);


    /// <summary>
    /// Alternate name for function JSON
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="o"></param>
    /// <param name="includeFields"></param>
    /// <param name="statusCode"></param>
    public void SendJSON<T>(T o, bool includeFields = true, int statusCode = HTTP_CODES.OK)
    {
        SendJSON<T>(o, includeFields, true, statusCode);
    }

    /// <summary>
    /// Alternate name for function JSON
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="o"></param>
    /// <param name="includeFields"></param>
    /// <param name="writeIndented"></param>
    /// <param name="statusCode"></param>
    public void SendJSON<T>(T o, bool includeFields = true, bool writeIndented = true, int statusCode = HTTP_CODES.OK) => JSON(o, includeFields, writeIndented, statusCode);
    
    ///<summary>
    /// Alternate name for function JSON
    /// </summary>
    /// <param name="content"></param>                
    public void SendJSON(string content) => JSON(content, HTTP_CODES.OK);

    
    #endregion
    
    #region attributes methods
    //function related to a basic preprocessing feature

    /// <summary>
    /// Adds an attribute to the HTML file that will be processed, if it already exists it will be overwritten
    /// </summary>
    /// <param name="name">Name of the attribute</param>
    /// <param name="value">Value of the attribute</param>
    public void AddAttribute(string name, string value)
    {
        if (attributes.ContainsKey(name))
            attributes[name] = value;
        else
            attributes.Add(name, value);
    }

    /// <summary>
    /// Removes an attribute to the HTML file that will be processed
    /// </summary>
    /// <param name="name">Name of the attribute</param>
    public void RemoveAttribute(string name)
    {
        attributes.Remove(name);
    }

    /// <summary>
    /// Retrieves the value of an attribute to the HTML file that will be processed, if doesn't exists it will return an empty string
    /// </summary>
    /// <param name="name">Name of the attribute</param>
    public string GetAttribute(string name)
    {
        return attributes[name] ?? "";
    }

    #endregion
    
    #region Utils
    
    public void SetCORS(CORS cors)
    {
        this.cors = cors;
    }
    
    /// <summary>
    /// Does a basic content-processing of a given HTML file
    /// </summary>
    /// <param name="content">Name of the attribute</param>
    private string ProcessContent(string content)
    {
        foreach (var attr in attributes)
        {
            content = content.Replace($"#{{{attr.Key}}}", attr.Value);
        }

        return content;
    }
    
        
    /// <summary>
    /// Calculate the header of an HTTP Response
    /// </summary>
    /// <param name="responseCode">The response status code</param>
    /// <param name="size">Size in bytes of the body</param>
    /// <param name="contentType">Mimetype of the body</param>
    /// <param name="customHeaders">Optional headers</param>
    /// <returns></returns>
    private string GetHeaders(int responseCode, int size, string contentType,
        Dictionary<string, string>? customHeaders = null)
    {
        CultureInfo ci = new("en-US");

        var currentTime = DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss ", ci) + "GMT";

        var headers = $"{HttpUtils.ProtocolAsString(request.PROTOCOL)} {responseCode} {request.URL} {NEW_LINE}";
        headers += "Date: " + currentTime + NEW_LINE;
        if (config.CustomServerName != "")
            headers += $"Server: {config.CustomServerName}{NEW_LINE}";
        else
            headers += $"Server: HSB-#/{Assembly.GetExecutingAssembly().GetName().Version} ({GetOSInfo()})" + NEW_LINE;

        headers += $"Last-Modified: {currentTime}{NEW_LINE}";

        if (responseCode is < 300 or > 399)
        {
            headers += $"Content-Length: {size}{NEW_LINE}";
            headers += $"Content-Type: {contentType}{NEW_LINE}";
        }

        if (customHeaders != null)
        {
            foreach (var h in customHeaders)
            {
                headers += $"{h.Key}: {h.Value}{NEW_LINE}";
            }

            //if it's a redirect "Location" header is a must
            if (responseCode is >= 300 and <= 399 && !customHeaders.ContainsKey("Location"))
            {
                throw new InvalidRedirectRoute();
            }
        }

        if (config.CustomGlobalHeaders.Count != 0)
        {
            foreach (var h in config.CustomGlobalHeaders)
            {
                headers += $"{h.Key}: {h.Value}{NEW_LINE}";
            }
        }

        if (config.CustomGlobalCookies.Count != 0)
        {
            foreach (var c in config.CustomGlobalCookies)
            {
                headers += $"Set-Cookie: {c.Value}{NEW_LINE}";
            }
        }

        //CORS
        config.GlobalCORS?.AllowedOrigins.ForEach(origin =>
        {
            headers += $"Access-Control-Allow-Origin: {origin}{NEW_LINE}";
        });
        cors?.AllowedOrigins.ForEach(origin => { headers += $"Access-Control-Allow-Origin: {origin}{NEW_LINE}"; });

        /*   if (request.GetHeaders["Connection"] != null)
           {
               headers += $"Connection: {request.GetHeaders["Connection"]}";
           }
           else
           {*/
        //visit https://httpwg.org/specs/rfc9113.html#ConnectionSpecific, p8.2.2 (27-Jun-23)
        if (request.PROTOCOL == HTTP_PROTOCOL.HTTP1_0 || request.PROTOCOL == HTTP_PROTOCOL.HTTP1_1)
            headers += "Connection: Close";

        //}
        headers += NEW_LINE + NEW_LINE;
        return headers;
    }

    private static object GetOSInfo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Environment.OSVersion.ToString().Replace("Unix", "macOS");
        }

        return Environment.OSVersion;
    }

    
    
    #endregion

    #region Streaming Response Methods

    // Streaming Response methods
    /// <summary>
    /// Inizializza una risposta HTTP in modalità chunked streaming.
    /// Invia gli header HTTP con Transfer-Encoding: chunked.
    /// Deve essere chiamata prima di AddStreamChunk/EndStream.
    /// </summary>
    public async Task InitStream(string mimeType = "text/plain", int statusCode = HTTP_CODES.OK,
        Dictionary<string, string>? customHeaders = null)
    {
        var headers = new Dictionary<string, string>
        {
            {"Transfer-Encoding", "chunked"},
            {"Content-Type", mimeType},
            {"Connection", "keep-alive"}
        };

        if (customHeaders != null)
        {
            foreach (var h in customHeaders)
                headers[h.Key] = h.Value;
        }

        string headerStr = GetHeaders(statusCode, 0, mimeType, headers);
        await WriteRaw(Encoding.UTF8.GetBytes(headerStr));
    }

    /// <summary>
    /// Invia un blocco (chunk) di dati al client secondo la codifica HTTP chunked.
    /// Può essere chiamata più volte dopo InitStream per inviare contenuti progressivi.
    /// </summary>
    public async Task AddStreamChunk(string data)
    {
        byte[] chunkBytes = Encoding.UTF8.GetBytes(data);
        string chunkSize = chunkBytes.Length.ToString("X"); // dimensione in esadecimale
        string chunk = $"{chunkSize}\r\n{data}\r\n";
        await WriteRaw(Encoding.UTF8.GetBytes(chunk));
    }

    /// <summary>
    /// Termina una risposta HTTP in streaming inviando il chunk finale.
    /// Obbligatorio per chiudere correttamente la connessione secondo lo standard.
    /// </summary>
    public async Task EndStream()
    {
        await WriteRaw(Encoding.UTF8.GetBytes("0\r\n\r\n"));
    }

    /// <summary>
    /// Scrive direttamente byte sulla connessione attiva (SSL o socket).
    /// Utilizzato internamente da InitStream/AddStreamChunk/EndStream.
    /// </summary>
    private async Task WriteRaw(byte[] data)
    {
        try
        {
            if (sslStream != null)
                await sslStream.WriteAsync(data);
            else
                await socket.SendAsync(data, SocketFlags.None);
        }
        catch (Exception e)
        {
            Terminal.ERROR("Errore durante WriteRaw: " + e);
        }
    }

    #endregion
}