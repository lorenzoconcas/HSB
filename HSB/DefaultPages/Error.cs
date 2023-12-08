using System.Reflection;

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
            var debugMode = false;
#if DEBUG
            debugMode = true;
#endif

            string title = $"Error {errorCode}";
            string content = "";
            if (errorCode >= 500 && errorCode <= 599)
            {

                if (debugMode)
                    content = GetStacktracePage();
                else content = Get5XXPage();
            }

            else if (errorCode >= 400 && errorCode <= 499)
                content = Get4XXPage();

            Send(title, content, errorCode);
        }

        private string GetStacktracePage() => "Stacktrace:<br>" + errorMsg.Replace("\n", "<br>");
        private static string Get4XXPage() => $"<h3>The requested resource was not found on this server</h3>";
        private static string Get5XXPage() => $"<h3>An internal error occurred while elaborating the request</h3>";

        private void Send(string title, string msg, int statusCode)
        {
            var page = ReadFromResources("error.html");

            string version = "v";
            if (Assembly.GetExecutingAssembly().GetName().Version != null)
            {
                version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            }

            string footer_div = "";
            string server_name;
            if (configuration.CustomServerName != "")
            {
                server_name = configuration.CustomServerName;
            }
            else
            {
                server_name = "HSB<sup>#</sup>";
                footer_div = "<div class=\"footer\">Copyright &copy; 2021-2023 Lorenzo L. Concas</div>";
            }
            res.AddAttribute("page_title", "Error " + errorCode);
            res.AddAttribute("serverName", server_name);
            res.AddAttribute("footer_div", footer_div);
            res.AddAttribute("hsbVersion", version);
            res.AddAttribute("title", title);
            res.AddAttribute("errorMsg", msg);
            res.SendHTMLContent(page, true, statusCode: statusCode);

        }

    }
}

