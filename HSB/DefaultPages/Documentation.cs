using HSB.Components.Attributes;
using System.Reflection;
using HSB.Utils;


namespace HSB.DefaultPages;

public class Documentation(Response res, Configuration configuration)
{
    public class Endpoint(string path, string description, List<(Constants.HttpMethod, string, string)> methods)
    {
        public string Path = path;
        public string Description = description;
        public List<(Constants.HttpMethod, string, string)> Methods = methods;

        public Endpoint(string path, string description) : this(path, description, [])
        {
        }

        public void AddMethod(Constants.HttpMethod method, string path, string description)
        {
            this.Methods.Add((method, path, description));
        }
        
    }


    public void Get()
    {
        //collect al classes with HSB.Components.Attributes.Documentation attribute
        var classes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => p.GetCustomAttributes(typeof(DocumentClass), false).Length > 0);
        // .Select(p => new { p.Name, ((HSB.Components.Attributes.Documentation)p.GetCustomAttributes(typeof(HSB.Components.Attributes.Documentation), false)[0]).Description });

        //then we need to enumerate all overridden methods of the classes

        List<Endpoint> endpoints = [];
        foreach (var c in classes)
        {
            //get all declared methods of the class that starts ith "Process"
            //BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
            var methods = c.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            var bind = (Binding) c.GetCustomAttributes(typeof(Binding), false)[0];
            var doc = (DocumentClass) c.GetCustomAttributes(typeof(DocumentClass), false)[0];
            var path = bind.Path;
            var documentation = doc.Description;

            var ep = new Endpoint(path, documentation);

            foreach (var m in methods)
            {
                //generate a description for the method, this description is based on what the method returns
                //if the method returns void, we will say that the method does not return anything
                var description = m.ReturnType == typeof(void) ? "This method does not return anything" : $"This method returns a {m.ReturnType}";
                ep.AddMethod(HttpUtils.GetMethod(m.Name.Replace("Process", "").ToUpper()), "", description);
            }

            endpoints.Add(ep);
        }

        var page = ResourceUtils.LoadResourceAsString("documentation.html");
        var version = "v";
        if (Assembly.GetExecutingAssembly().GetName().Version != null)
        {
            version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        }


        var footerDiv = "";
        string serverName;
        var logo = "";
        //string title = "";
        if (configuration.CustomServerName != "")
        {
            serverName = configuration.CustomServerName;
        }
        else
        {
            serverName = "HSB<sup>#</sup>";
            footerDiv = "<div class=\"footer\">Copyright &copy; 2021-2025 Lorenzo L. Concas</div>";
            var logoB64 = ResourceUtils.LoadResourceAsString("logo_b64");
            logo = $"<img width=\"32px\" src=\"{logoB64}\" />";
            // title = "Http Server Boxed <sup>#</sup>";
        }


        //build divs
        var content = "";
        foreach (var ep in endpoints)
        {
            content += "<div class=\"endpoint\">";
            content += $"<h2>{ep.Path}</h2>";
            content += $"<p>{ep.Description}</p>";

            foreach (var method in ep.Methods)
            {
                content +=
                    $"<div><div class='method {HttpUtils.MethodAsString(method.Item1).ToUpper()}'>{method.Item1}</div></div>";
            }

            content += "</div>";
        }


        //set attributes
        res.AddAttribute("content", content);

        res.AddAttribute("logo", logo); //this break some configurations, logo must be replaced with a smaller image
        //  res.AddAttribute("title", title);
        res.AddAttribute("serverName", serverName);
        res.AddAttribute("footer_div", footerDiv);
        res.AddAttribute("hsbVersion", version);

        res.SendHTMLContent(page, true);


        /*   var json = "";
          json += "[";
          foreach (var ep in endpoints)
          {
              json += ep.GetJSON();
              if (!endpoints.Last().Equals(ep))
              {
                  json += ",";
              }
          }
          json += "]";


          res.JSON(json);
   */

        // res.SendJSON("{}");
    }
}