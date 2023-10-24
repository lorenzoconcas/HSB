using System;
using System.Reflection;

namespace HSB
{
    public class Index : Servlet
    {
        public Index(Request req, Response res) : base(req, res)
        {

        }
        public override void ProcessGet()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("index.html"));
            string result;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
            using (StreamReader reader = new(stream))
            {
                result = reader.ReadToEnd();
            }

            string version = "";
            if(Assembly.GetExecutingAssembly().GetName().Version != null)
            {
                version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            }
            res.AddAttribute("hsbVersion", version);
            res.SendHTMLContent(result, true);

        }
    }
}

