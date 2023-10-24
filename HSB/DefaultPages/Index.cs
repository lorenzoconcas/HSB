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
                version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
            }
            string footer_div = "";
            string server_name = "";
            if (configuration.CustomServerName != "")
            {
                server_name = configuration.CustomServerName;
            }
            else
            {
                server_name = "HSB<sup>#</sup>";
                footer_div = "<div class=\"footer\">Copyright &copy; 2021-2023 Lorenzo L. Concas</div>";
            }
            res.AddAttribute("serverName", server_name);
            res.AddAttribute("footer_div", footer_div);
            res.AddAttribute("hsbVersion", version);
            res.SendHTMLContent(result, true);

        }
    }
}

