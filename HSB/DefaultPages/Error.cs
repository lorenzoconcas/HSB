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
            string title = $"Error {errorCode}";
            string content = "";
            if (errorCode >= 500)

                content = GetStacktracePage();


            else if (errorCode >= 400 && errorCode <= 499)
                content = Get4XXPage();


            Send(title, content);


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

            string content = $"<h3>La risorsa cercata non &egrave; stata trovata su questo server</h3>";
            return content;
        }

        private void Send(string title, string msg)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("error.html"));
            string result;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
            using (StreamReader reader = new(stream))
            {
                result = reader.ReadToEnd();
            }
            res.AddAttribute("hsbVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            res.AddAttribute("title", title);
            res.AddAttribute("errorMsg", msg);
            res.SendHTMLContent(result, true);

        }

    }
}

