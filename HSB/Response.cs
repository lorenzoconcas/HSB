using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
        //Send methods
        public void Send(byte[] data)
        {
            try
            {
                int totalBytes = socket.Send(data);
            }
            catch (Exception e)
            {
                Terminal.ERROR($"Error sending data ->\n {e}");
            }
        }
        public void SendHTMLPage(string path, bool process = false)
        {
            try
            {
                String content = File.ReadAllText(path);
                if (process)
                    content = processContent(content);
                Send(content, MimeType.TEXT_HTML);
            }
            catch (Exception)
            {
                //dato che l'invio dei dati è parte nostra, se non riusciamo diamo un errore 500
                SendCode(500);
                Terminal.ERROR("Error sending file : " + path);
            }
        }

        public void Send(string data, string? mimeType = null, int statusCode = 200)
        {
            string _mime = mimeType ?? MimeType.TEXT_PLAIN; //qui va messo l'autodetect


            string resp = GetHeaders(statusCode, data.Length + 2, _mime) + "\r\n" + data;

            Send(Encoding.UTF8.GetBytes(resp));
        }
        public void SendFile(string absPath, string? mimeType = null, int statusCode = 200)
        {

            var data = File.ReadAllBytes(absPath);

            string _mime = mimeType ?? MimeType.GetMimeType(Path.GetExtension(absPath)); ; //qui va messo l'autodetect

            string headers = GetHeaders(statusCode, data.Length + 2, _mime);
            byte[] headersBytes = Encoding.UTF8.GetBytes(headers);
            byte[] responseBytes = new byte[data.Length + headersBytes.Length];




            headersBytes.CopyTo(responseBytes, 0);
            data.CopyTo(responseBytes, headersBytes.Length);

            Send(responseBytes);


        }
        public void SendCode(int httpCode)
        {
            string resp = GetHeaders(httpCode, 0, "text/plain") + "\r\n";

            Send(Encoding.UTF8.GetBytes(resp));
        }
        public string GetHeaders(int responseCode, int size, string contentType)
        {
            CultureInfo ci = new CultureInfo("en-US");

            string currentTime = DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss GMT", ci);
            string headers = HttpUtils.ProtocolAsString(request.PROTOCOL) + " " + responseCode + " " + request.URL + NEW_LINE;
            headers += "Date: " + currentTime + NEW_LINE;
            headers += $"Server : HSB-#/{Assembly.GetExecutingAssembly().GetName().Version} ({Environment.OSVersion.ToString()})" + NEW_LINE;
            headers += "Last-Modified: " + currentTime + NEW_LINE;
            headers += "Content-Length: " + size + NEW_LINE;
            headers += "Content-Type: " + contentType + NEW_LINE;
            headers += "Connection: Closed";
            headers += NEW_LINE + NEW_LINE;
            return headers;
        }

        private string processContent(string content)
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
