using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace HSB
{
    public class Error : Servlet
    {
        private readonly string errorMsg;
        //http error code
        private readonly int errorCode;
        public Error(Request req, Response res, Configuration config, string errorMessage, int errorCode) : base(req, res, config)
        {
            this.errorCode = errorCode;
            errorMsg = errorMessage;

            handlerFallback = ProcessGet;
        }
        public override void ProcessGet()
        {
            string title = $"Error {errorCode}";
            string content = "";
            if (errorCode >= 500)

                content = GetStacktracePage();

            else if (errorCode >= 400 && errorCode <= 499)
                content = Get4XXPage();

            Send(title, content, errorCode);
        }

        private string GetStacktracePage()
        {
            string content = "";// = $"<h2>Error {errorCode}</h2><hr>";
            content += "Stacktrace:<br>";
            content += errorMsg.Replace("\n", "<br>");

            return content;
        }

        private static string Get4XXPage()
        {

            string content = $"<h3>The searched resource was not found on this server</h3>";
            return content;
        }

        private void Send(string title, string msg, int statusCode)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("error.html"));
            string result;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
            using (StreamReader reader = new(stream))
            {
                result = reader.ReadToEnd();
            }

            string version = "v";
            if (Assembly.GetExecutingAssembly().GetName().Version != null)
            {
                version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            }

            if (configuration.CustomServerName != "")
            {
                res.AddAttribute("serverName", configuration.CustomServerName);
            }
            else
            {
                res.AddAttribute("serverName", "HSB<sup>#</sup>");
            }
            res.AddAttribute("hsbVersion", version);
            res.AddAttribute("title", title);
            res.AddAttribute("errorMsg", msg);
            res.SendHTMLContent(result, true, statusCode: statusCode);

        }

    }
}

