using System;
using System.Reflection;

namespace HSB
{
    public class Index : Servlet
    {
        public Index(Request req, Response res, Configuration config) : base(req, res, config)
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

            string version = "v";
            if (Assembly.GetExecutingAssembly().GetName().Version != null)
            {
                version = Assembly.GetExecutingAssembly().GetName().Version!.ToString();
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
            res.SendHTMLContent(result, true);

        }
    }
}

