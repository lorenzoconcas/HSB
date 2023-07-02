using System;
using System.Reflection;

namespace HSB
{
    public class Index : Servlet
    {
        public Index(Request req, Response res) : base(req, res)
        {

        }
        public override void ProcessGet(Request req, Response res)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("index.html"));
            string result;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
            using (StreamReader reader = new(stream))
            {
                result = reader.ReadToEnd();
            }
            res.AddAttribute("hsbVersion", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            res.SendHTMLContent(result, true);


            //res.Send($"<h1>Welcome to HSB-# ({Assembly.GetExecutingAssembly().GetName().Version})</h1>", MimeType.TEXT_HTML);
        }
    }
}

