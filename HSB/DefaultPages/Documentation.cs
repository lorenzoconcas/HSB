using HSB;
using HSB.Components.Attributes;
using System.Reflection;


namespace HSB.DefaultPages;


public class Documentation(Request req, Response res, Configuration config) : Servlet(req, res, config)
{


    public class Endpoint
    {
        public string path;
        public string description;
        public List<(HSB.Constants.HTTP_METHOD, string, string)> methods;

        public Endpoint(string path, string description)
        {
            this.path = path;
            this.description = description;
            this.methods = [];
        }

        public Endpoint(string path, string description, List<(HSB.Constants.HTTP_METHOD, string, string)> methods)
        {
            this.path = path;
            this.description = description;
            this.methods = methods;
        }

        public void AddMethod(HSB.Constants.HTTP_METHOD method, string path, string description)
        {
            this.methods.Add((method, path, description));
        }

        public string GetJSON()
        {
            string json = "{";
            json += $"\"path\": \"{this.path}\",";
            json += $"\"description\": \"{this.description}\",";
            json += "\"methods\": [";

            foreach (var method in this.methods)
            {
                json += "{";
                json += $"\"method\": \"{method.Item1}\",";
                json += $"\"path\": \"{method.Item2}\",";
                json += $"\"description\": \"{method.Item3}\"";
                json += "}";
                if (!this.methods.Last().Equals(method))
                {
                    json += ",";
                }
            }
            json += "]";
            json += "}";
            return json;
        }



    }


    override public void ProcessGet()
    {
        //collect al classes with HSB.Components.Attributes.Documentation attribute
        var classes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => p.GetCustomAttributes(typeof(DocumentClass), false).Length > 0);
        // .Select(p => new { p.Name, ((HSB.Components.Attributes.Documentation)p.GetCustomAttributes(typeof(HSB.Components.Attributes.Documentation), false)[0]).Description });

        //then we need to enumerate all overidden methods of the classes

        List<Endpoint> endpoints = [];
        foreach (var c in classes)
        {
            //get all declared methods of the class that starts ith "Process"
            //BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
            var methods = c.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            Binding bind = (Binding)c.GetCustomAttributes(typeof(Binding), false)[0];
            DocumentClass doc = (DocumentClass)c.GetCustomAttributes(typeof(DocumentClass), false)[0];
            string path = bind.Path;
            string documentation = doc.Description;

            Endpoint ep = new Endpoint(path, documentation);

            foreach (var m in methods)
            {
                //generate a description for the method, this description is based on what the method returns
                //if the method returns void, we will say that the method does not return anything
                string description = "";
                if (m.ReturnType == typeof(void))
                {
                    description = "This method does not return anything";
                }
                else
                {
                    description = $"This method returns a {m.ReturnType}";
                }

                ep.AddMethod(HttpUtils.GetMethod(m.Name.Replace("Process", "").ToUpper()), "", description);
            }

            endpoints.Add(ep);
        }

        string page = ReadFromResources("documentation.html");
        string version = "v";
        if (Assembly.GetExecutingAssembly().GetName().Version != null)
        {
            version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        }



        string footer_div = "";
        string server_name;
        string logo = "";
        //string title = "";
        if (configuration.CustomServerName != "")
        {
            server_name = configuration.CustomServerName;
        }
        else
        {
            server_name = "HSB<sup>#</sup>";
            footer_div = "<div class=\"footer\">Copyright &copy; 2021-2024 Lorenzo L. Concas</div>";
            string logo_b64 = ReadFromResources("logo_b64");
            logo = $"<img width=\"32px\" src=\"{logo_b64}\" />";
            // title = "Http Server Boxed <sup>#</sup>";
        }


        //build divs
        string content = "";
        foreach (var ep in endpoints)
        {
            content += "<div class=\"endpoint\">";
            content += $"<h2>{ep.path}</h2>";
            content += $"<p>{ep.description}</p>";

            foreach (var method in ep.methods)
            {
                content += $"<div><div class='method {HttpUtils.MethodAsString(method.Item1).ToUpper()}'>{method.Item1}</div></div>";
            }

            content += "</div>";
        }


        //set attributes
        res.AddAttribute("content", content);

        res.AddAttribute("logo", logo); //this break some configurations, logo must be replaced with a smaller image
                                        //  res.AddAttribute("title", title);
        res.AddAttribute("serverName", server_name);
        res.AddAttribute("footer_div", footer_div);
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
