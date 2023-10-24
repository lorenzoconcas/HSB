using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using HSB.Constants;
using HSB.Components;
using HSB.Exceptions;

namespace HSB;


public class Response
{
    private Socket socket;
    private Request request;
    private Configuration config;
    Dictionary<string, string> attributes = new();

    private const string NEW_LINE = "\r\n";
    public Response(Socket socket, Request request, Configuration c)
    {
        this.socket = socket;
        this.request = request;
        config = c;
    }


    //Send methods


    /// <summary>
    /// Send an un modified byte array to to the socket
    /// </summary>
    /// <param name="data"></param>
    public void Send(byte[] data, bool disconnect = true)
    {
        try
        {
            int totalBytes = socket.Send(data);
            socket.Disconnect(disconnect);
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
    public void Send(string data, string mimeType = "text/plain", int statusCode = 200, Dictionary<string, string>? customHeaders = null)
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
    public void SendHTMLPage(string path, bool process = false, Dictionary<string, string>? customHeaders = null)
    {
        try
        {
            string content = File.ReadAllText(path);
            if (process)
                content = ProcessContent(content);
            Encoding encoding = Utils.GetEncoding(path);

            Send(content, MimeTypeUtils.TEXT_HTML + $"; charset={encoding.BodyName}", customHeaders: customHeaders);
        }
        catch (Exception)
        {
            //dato che l'invio dei dati è parte nostra, se non riusciamo diamo un errore 500
            SendCode(500);
            Terminal.ERROR("Error sending file : " + path);
        }
    }
    /// <summary>
    /// Sends an html page passed as string
    /// </summary>
    /// <param name="path">HTML content</param>
    /// <param name="process">Whether or not or not process the document before sending</param>
    public void SendHTMLContent(string content, bool process = false, string encoding = "UTF-8", Dictionary<string, string>? customHeaders = null, int statusCode = 200)
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
    public void SendFile(string absPath, string? mimeType = null, int statusCode = 200, Dictionary<string, string>? customHeaders = null)
    {
        var data = File.ReadAllBytes(absPath);

        string _mime = mimeType ?? MimeTypeUtils.GetMimeType(Path.GetExtension(absPath)) ?? MimeTypeUtils.APPLICATION_OCTET;
        string headers = GetHeaders(statusCode, data.Length, _mime, customHeaders);
        byte[] headersBytes = Encoding.UTF8.GetBytes(headers);
        byte[] responseBytes = new byte[data.Length + headersBytes.Length];

        headersBytes.CopyTo(responseBytes, 0);
        data.CopyTo(responseBytes, headersBytes.Length);

        Send(responseBytes);
    }
    /// <summary>
    /// Sends data to the client
    /// </summary>
    /// <param name="data"></param>
    /// <param name="mimeType"></param>
    /// <param name="statusCode"></param>
    public void SendFile(byte[] data, string mimeType, int statusCode = 200, Dictionary<string, string>? customHeaders = null)
    {
        string _mime = mimeType;
        string headers = GetHeaders(statusCode, data.Length, _mime, customHeaders);
        byte[] headersBytes = Encoding.UTF8.GetBytes(headers);
        byte[] responseBytes = new byte[data.Length + headersBytes.Length];

        headersBytes.CopyTo(responseBytes, 0);
        data.CopyTo(responseBytes, headersBytes.Length);

        Send(responseBytes);
    }
    /// <summary>
    /// Send a FilePart to the client
    /// </summary>
    /// <param name="filePart"></param>
    /// <param name="statusCode"></param>
    /// <param name="customHeaders"></param>
    public void SendFile(FilePart filePart, int statusCode = 200, Dictionary<string, string>? customHeaders = null)
    {
        SendFile(filePart.GetBytes(), filePart.GetMimeType(), statusCode, customHeaders);
    }

    /// <summary>
    /// Send an HTTP Response with no body but with given status code
    /// </summary>
    /// <param name="statusCode"></param>
    public void SendCode(int statusCode)
    {
        string resp = GetHeaders(statusCode, 0, MimeTypeUtils.TEXT_PLAIN) + NEW_LINE;

        Send(Encoding.UTF8.GetBytes(resp));
    }
    /// <summary>
    /// Sends a code with mnemonic name
    /// </summary>
    /// <param name="code"></param>
    public void SendCode(HttpCodes code)
    {
        Send(code);
    }
    /// <summary>
    /// Shorthand for SendCode
    /// </summary>
    /// <param name="statusCode"></param>
    public void Send(int statusCode)
    {
        SendCode(statusCode);
    }
    /// <summary>
    /// Shorthand for SendCode, but with mnemonic names
    /// </summary>
    /// <param name="code"></param>
    public void Send(HttpCodes code)
    {
        SendCode((int)code);
    }
    /// <summary>
    /// Sends a redirect to the client
    /// </summary>
    /// <param name="route"></param>
    public void Redirect(string route, int statusCode = 302)
    {
        if (statusCode < 300 && statusCode > 399)
            throw new InvalidHttpCodeException(statusCode);

        string response = GetHeaders(
           statusCode,
            0,
            MimeTypeUtils.TEXT_PLAIN,
            new Dictionary<string, string>() { { "Location", route } }
        ) + NEW_LINE;

        Send(Encoding.UTF8.GetBytes(response));
    }
    /// <summary>
    /// Same as Redirect(string, int) but with HttpCodes
    /// </summary>
    /// <param name="route"></param>
    /// <param name="code"></param>
    public void Redirect(string route, HttpCodes code)
    {
        Redirect(route, (int)code);
    }
    /// <summary>
    /// Redirects to a given servlet
    /// </summary>
    /// <param name="s"></param>
    /// <param name="statusCode"></param>
    public void Redirect(Servlet s, int statusCode = 302)
    {
        Redirect(s.GetRoute(), statusCode);
    }
    /// <summary>
    /// Same as Redirect(Servlet, int)
    /// </summary>
    /// <param name="s"></param>
    /// <param name="code"></param>
    public void Redirect(Servlet s, HttpCodes code = HttpCodes.FOUND)
    {
        Redirect(s.GetRoute(), code);
    }

    //common status codes

    /// <summary>
    /// Bad Request
    /// </summary>
    public void E400()
    {
        SendCode(400);
    }
    /// <summary>
    /// Unauthorized
    /// </summary>
    public void E401()
    {
        SendCode(401);
    }
    /// <summary>
    /// Not Found
    /// </summary>
    public void E404()
    {
        SendCode(404);
    }
    /// <summary>
    /// Internal Server Error
    /// </summary>
    public void E500()
    {
        SendCode(500);
    }

    /// <summary>
    /// Sends a HTTP Response with a JSON body passed as parameter
    /// </summary>
    /// <param name="content">String of the body in JSON format</param>
    public void JSON(string content)
    {
        Send(content, "application/json");
    }
    /// <summary>
    /// Serializes and sends an Object in JSON format
    /// </summary>
    /// <param name="o">Object to be serialized and sended as response</param>
    /// <param name="options">Options for the serializer (System.Text.Json.JsonSerializer)</param>
    public void JSON<T>(T o, JsonSerializerOptions options)
    {
        JSON(JsonSerializer.Serialize(o, options));
    }
    /// <summary>
    /// Serialize and sends an Object in JSON Format
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="o"></param>
    /// <param name="includeFields">Whether or not or not include fields of the object</param>
    public void JSON<T>(T o, bool includeFields = true)
    {
        JsonSerializerOptions jo = new()
        {
            IncludeFields = includeFields,
            MaxDepth = 0
        };

        JSON(JsonSerializer.Serialize(o, jo));
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
    /// <param name="options"></param>
    public void SendJSON<T>(T o, bool includeFields = true) => JSON(o, includeFields);

    ///<summary>
    /// Alternate name for function JSON
    /// </summary>
    /// <param name="content"></param>                
    public void SendJSON(string content) => JSON(content);


    /// <summary>
    /// Calculate the header of an HTTP Response
    /// </summary>
    /// <param name="responseCode">The response status code</param>
    /// <param name="size">Size in bytes of the body</param>
    /// <param name="contentType">Mimetype of the body</param>
    /// <param name="customHeaders">Optional headers</param>
    /// <returns></returns>
    private string GetHeaders(int responseCode, int size, string contentType, Dictionary<string, string>? customHeaders = null)
    {
        CultureInfo ci = new("en-US");

        string currentTime = DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss ", ci) + "GMT";

        string headers = $"{HttpUtils.ProtocolAsString(request.PROTOCOL)} {responseCode} {request.URL} {NEW_LINE}";
        headers += "Date: " + currentTime + NEW_LINE;
        if (config.CustomServerName != "")
            headers += $"Server: {config.CustomServerName}{NEW_LINE}";
        else
            headers += $"Server: HSB-#/{Assembly.GetExecutingAssembly().GetName().Version} ({GetOSInfo()})" + NEW_LINE;

        headers += $"Last-Modified: {currentTime}{NEW_LINE}";


        if (responseCode < 300 || responseCode > 399)
        {
            headers += $"Content-Length: {size}{NEW_LINE}";
            headers += $"Content-Type: {contentType}{NEW_LINE}";
        }

        if (customHeaders != null)
        {

            foreach (var h in customHeaders)
                headers += $"{h.Key}: {h.Value}{NEW_LINE}";

            //if it's a redirect "Location" header is a must
            if (responseCode >= 300 && responseCode <= 399 && !customHeaders.ContainsKey("Location"))
            {
                throw new InvalidRedirectRoute();
            }
        }

        if (config.CustomGlobalHeaders.Any())
        {
            foreach (var h in config.CustomGlobalHeaders)
            {
                headers += $"{h.Key}: {h.Value}{NEW_LINE}";
            }
        }

        if (config.CustomGlobalCookies.Any())
        {
            foreach (var c in config.CustomGlobalCookies)
            {
                headers += $"Set-Cookie: {c.Value}{NEW_LINE}";
            }
        }

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


    //function related to a basic preprocessing feature

    /// <summary>
    /// Adds an attribute to the HTML file that will be processed
    /// </summary>
    /// <param name="name">Name of the attribute</param>
    /// <param name="value">Value of the attribute</param>
    public void AddAttribute(string name, string value)
    {
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
    /// Retrieves the value of an attribute to the HTML file that will be processed
    /// </summary>
    /// <param name="name">Name of the attribute</param>
    public string GetAttribute(string name)
    {
        return attributes[name];
    }
    /// <summary>
    /// Does a basic content-processing of a given HTML file
    /// </summary>
    /// <param name="name">Name of the attribute</param>
    /// <param name="value">Value of the attribute</param>
    private string ProcessContent(string content)
    {
        foreach (var attr in attributes)
        {
            content = content.Replace($"#{{{attr.Key}}}", attr.Value);
        }
        return content;
    }
}
