using HSB;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace HSB.DefaultPages;


public class Documentation(Request req, Response res, Configuration config) : Servlet(req, res, config)
{


    public class Endpoint
    {
        string path;
        string description;
        List<(HSB.Constants.HTTP_METHOD, string, string)> methods;

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
            .Where(p => p.GetCustomAttributes(typeof(HSB.Components.Attributes.Documentation), false).Length > 0);
        // .Select(p => new { p.Name, ((HSB.Components.Attributes.Documentation)p.GetCustomAttributes(typeof(HSB.Components.Attributes.Documentation), false)[0]).Description });

        //then we need to enumerate all overidden methods of the classes

        List<Endpoint> endpoints = [];
        foreach (var c in classes)
        {
            //get all declared methods of the class that starts ith "Process"
            //BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly
            var methods = c.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            Binding bind = ((Binding)c.GetCustomAttributes(typeof(Binding), false)[0]);
            HSB.Components.Attributes.Documentation doc = ((HSB.Components.Attributes.Documentation)c.GetCustomAttributes(typeof(HSB.Components.Attributes.Documentation), false)[0]);
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

                ep.AddMethod(HttpUtils.GetMethod(m.Name.Replace("Process", "")), "", description);
            }

            endpoints.Add(ep);
            Console.WriteLine(methods);
        }



        var json = "";
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


        // res.SendJSON("{}");
    }
}
