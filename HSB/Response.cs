using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MimeTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HSB
{

    public class Response
    {
        Socket socket;
        Request request;

        Dictionary<string, string> attributes = new();

        private const string NEW_LINE = "\r\n";
        public Response(Socket socket, Request request)
        {
            this.socket = socket;
            this.request = request;
        }


        //Send methods


        /// <summary>
        /// Send an un modified byte array to to the socket
        /// </summary>
        /// <param name="data"></param>
        public void Send(byte[] data)
        {
            try
            {
                int totalBytes = socket.Send(data);
                socket.Disconnect(true);
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
        public void Send(string data, string? mimeType = null, int statusCode = 200)
        {
            string _mime = mimeType ?? MimeTypeMap.GetMimeType(data);

            string resp = GetHeaders(statusCode, Encoding.UTF8.GetBytes(data).Length, _mime) + data;

            Send(Encoding.UTF8.GetBytes(resp));
        }
        /// <summary>
        /// Loads and HTML file from path and sends it as HTTP Response with mimeType = text/html
        /// Optionally it can provides a basic processor function
        /// </summary>
        /// <param name="path">Path of the HTML file</param>
        /// <param name="process">Whether or not or not process the document before sending</param>
        public void SendHTMLPage(string path, bool process = false)
        {
            try
            {
                string content = File.ReadAllText(path);
                if (process)
                    content = ProcessContent(content);
                Send(content, MimeType.TEXT_HTML);
            }
            catch (Exception)
            {
                //dato che l'invio dei dati è parte nostra, se non riusciamo diamo un errore 500
                SendCode(500);
                Terminal.ERROR("Error sending file : " + path);
            }
        }
        /// <summary>
        /// Loads a file from a given path and sends an HTTP Response
        /// </summary>
        /// <param name="absPath">Path (absolute) of the file</param>
        /// <param name="mimeType">MimeType of the file</param>
        /// <param name="statusCode">Response status code</param>
        public void SendFile(string absPath, string? mimeType = null, int statusCode = 200)
        {
            var data = File.ReadAllBytes(absPath);

            string _mime = mimeType ?? MimeType.GetMimeType(Path.GetExtension(absPath));
            string headers = GetHeaders(statusCode, data.Length, _mime);
            byte[] headersBytes = Encoding.UTF8.GetBytes(headers);
            byte[] responseBytes = new byte[data.Length + headersBytes.Length];

            headersBytes.CopyTo(responseBytes, 0);
            data.CopyTo(responseBytes, headersBytes.Length);

            Send(responseBytes);
        }
        /// <summary>
        /// Send an HTTP Response with no body but with given status code
        /// </summary>
        /// <param name="httpCode"></param>
        public void SendCode(int httpCode)
        {
            string resp = GetHeaders(httpCode, 0, "text/plain") + "\r\n";

            Send(Encoding.UTF8.GetBytes(resp));
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
                IncludeFields = includeFields
            };

            JSON(JsonSerializer.Serialize(o, jo));
        }


        private string GetHeaders(int responseCode, int size, string contentType)
        {
            CultureInfo ci = new("en-US");

            string currentTime = DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss GMT", ci);

            string headers = $"{HttpUtils.ProtocolAsString(request.PROTOCOL)} {responseCode} {request.URL} {NEW_LINE}";
            headers += "Date: " + currentTime + NEW_LINE;
            headers += $"Server : HSB-#/{Assembly.GetExecutingAssembly().GetName().Version} ({Environment.OSVersion})" + NEW_LINE;
            headers += $"Last-Modified: {currentTime}{NEW_LINE}";
            headers += $"Content-Length: {size}{NEW_LINE}";
            headers += $"Content-Type: {contentType}{NEW_LINE}";


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


        //function related to a basic preprocessing feature
        public void AddAttribute(string name, string value)
        {
            attributes.Add(name, value);
        }
        public void RemoveAttribute(string name)
        {
            attributes.Remove(name);
        }
        public string GetAttribute(string name)
        {
            return attributes[name];
        }
        private string ProcessContent(string content)
        {
            foreach (var attr in attributes)
            {
                //  Terminal.WriteLine($"Replacing key: {attr.Key} with {attr.Value}", BG_COLOR.DEFAULT, FG_COLOR.BLU);
                content = content.Replace($"#{{{attr.Key}}}", attr.Value);
            }
            return content;
        }
    }
}
