using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace HSB
{
    public class Error : Servlet
    {
        private string errorMsg;
        //http error code
        private int errorCode;
        public Error(Request req, Response res, string errorMessage, int errorCode) : base(req, res)
        {

            this.errorCode = errorCode;
            this.errorMsg = errorMessage;
        }
        public override void ProcessGet(Request req, Response res)
        {
            string content = "";
            if (errorCode >= 500)
                content = GetStacktracePage();
            else if (errorCode >= 400 && errorCode <= 499)
                content = Get4XXPage();
            content += "<hr>HSB-# Server " + Assembly.GetExecutingAssembly().GetName().Version;
            content += "<hr><h6>(c) 2021 - 2023 Lorenzo L. Concas</h6>";
            res.Send(content, MimeType.TEXT_HTML, errorCode);
        }

        private string GetStacktracePage()
        {
            string content = $"<h2>Errore {errorCode}</h2><hr>";
            content += "Stacktrace:<br>";
            content += errorMsg.Replace("\n", "<br>");

            return content;
        }

        private string Get4XXPage()
        {
            string content = $"<h2>Errore {errorCode}</h2><hr>\n<h3>La risorsa cercata non &egrave; stata trovata su questo server</h3>";
            return content;
        }
    }
}

