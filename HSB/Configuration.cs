using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HSB
{
    public class Configuration
    {
        public string address;
        public int port;
        public string staticFolderPath = "";
        public bool verbose = true;

        //utile per condividere oggetti fra le servlet
        protected Dictionary<string, object> sharedObjects = new();

        //expressjs-like routing (es in expressjs you map pages and path like : app.get(path, (req, res){})

        private List<Tuple<string, Tuple<HTTP_METHOD, Delegate>>> expressMapping = new();

        public Configuration()
        {
            address = "127.0.0.1";
            port = 8080;
            staticFolderPath = "./static";
            verbose = true;
        }

        public Configuration(string address, int port, String staticPath, bool verbose)
        {

            this.address = address;
            this.port = port;
            staticFolderPath = staticPath;
            this.verbose = verbose;
        }

        private void AddExpressMapping(string path, HTTP_METHOD method, Delegate func)
        {
            Tuple<HTTP_METHOD, Delegate> x = new(method, func);
            Tuple<string, Tuple<HTTP_METHOD, Delegate>> tuple = new(path, x);
            expressMapping.Add(tuple);
        }




        public void Process(Request req, Response res)
        {
            new Task(() =>
            {
                try
                {
                    Terminal.INFO($"Requested '{req.URL}'", true);

                    if (RunIfExpressMapping(req, res))
                        return;

                    object? o = GetInstance(req, res);
                    if (o != null)
                    {
                        Servlet servlet = (Servlet)o;
                        servlet.Process();
                    }
                    else
                    {
                        //controlliamo se esistono routing che usano regex


                        //non è stata trovata una mappatura valida
                        //se siamo qui non c'è una pagina di root preimpostata, restituiamo quella di default
                        if (req.URL == "/")
                            //controlliamo che ci sia un file "index.html" else base servlet
                            if (File.Exists(staticFolderPath + "/index.html"))
                                res.SendFile(staticFolderPath + "/index.html", "text/html");
                            else
                                new Index(req, res).Process();
                        else
                        {
                            //controlliamo se si cerca una risorsa, altrimenti 404 non trovato

                            //usiamo la regex usata nella libreria send.js ()
                            //see: https://github.com/pillarjs/send/blob/master/index.js#L63
                            var UNSAFE_PATH_REGEX = "/(?:^|[\\\\/])\\.\\.(?:[\\\\/]|$)/";
                            Regex rgx = new(UNSAFE_PATH_REGEX);
                            if (rgx.Match(req.URL).Success)
                            {
                                Terminal.WARNING("Requested unsafe path");
                                new Error(req, res, "", 404).Process();

                            }
                            if (File.Exists(staticFolderPath + "/" + req.URL))
                            {
                                Terminal.INFO($"Static file found, serving '{req.URL}'", true);
                                res.SendFile(staticFolderPath + "/" + req.URL);

                            }
                            else
                            {
                                //potrebbe non venire trovata la giusta servlet, rimandiamo un errore 404
                                Terminal.WARNING($"No servlet or static found for URL : {req.URL}", true);
                                new Error(req, res, "Page not found", 404).Process();
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Terminal.ERROR("Error handling request ->\n " + e, true);
                    // Terminal.ERROR(e.Message);

                    new Error(req, res, e.ToString(), 500).Process();
                    return;
                }
            }).Start();
        }

        private bool RunIfExpressMapping(Request req, Response res)
        {
            var e = expressMapping.Find(e => (e.Item1 == req.URL && e.Item2.Item1 == req.METHOD));

            if (e != null)
            {
                (e.Item2.Item2).DynamicInvoke(new object[] { req, res });
                return true;
            }
            return false;
        }


        private static Dictionary<Tuple<string, bool>, Type> CollectStaticRoutes()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            List<Assembly> assems = currentDomain.GetAssemblies().ToList();

            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));

            Dictionary<Tuple<string, bool>, Type> routes = new();


            // List<Tuple<string, Type, bool>> routes = new();


            foreach (Assembly assem in assems)
            {
                List<Type> classes = assem.GetTypes().ToList();

                foreach (var c in classes)
                {
                    try
                    {
                        IEnumerable<Binding> multiBindings = c.GetCustomAttributes<Binding>(false);

                        //se non ci sono classi che hanno più binding, cerchiamo chi ne ha una sola
                        if (multiBindings.Any())
                        {
                            foreach (Binding b in multiBindings)
                            {
                                if (b.Path != "")
                                    routes.Add(new(b.Path, b.StartsWith), c);
                                // routes.Add(new(b.Path, c, b.StartsWith));
                            }

                        }
                        else
                        {
                            Binding? attr = c.GetCustomAttribute<Binding>(false);
                            if (attr != null && attr.Path != "")
                                routes.Add(new(attr.Path, attr.StartsWith), c);

                        }


                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return routes;
        }


        private object? GetInstance(Request req, Response res)
        {

            Dictionary<Tuple<string, bool>, Type> routes = CollectStaticRoutes();


            if (routes.ContainsKey(new(req.URL, false)))
            {
                Type c = routes[new(req.URL, false)];
                var x = c.GetConstructors()[0];
                return x.GetParameters().Length switch
                {
                    3 => Activator.CreateInstance(c, req, res, this),
                    2 => Activator.CreateInstance(c, req, res),
                    _ => throw new Exception($"Invalid servlet constructor found {x.Name}"),
                };
            }
            else
            {
                //controllare se esiste un url che inizia come quello della richiesta
                foreach (var r in routes)
                {
                    string path = r.Key.Item1;
                    if (req.URL.StartsWith(path))
                    {
                        Type c = r.Value;
                        var x = c.GetConstructors()[0];
                        return x.GetParameters().Length switch
                        {
                            3 => Activator.CreateInstance(c, req, res, this),
                            2 => Activator.CreateInstance(c, req, res),
                            _ => throw new Exception($"Invalid servlet constructor found {x.Name}"),
                        };
                    }
                }



                //WIP
                /* //controllare se usa una regex
                 Dictionary<string, Type> regexRoutes = new();
                 foreach (var r in routes)
                     regexRoutes.Add(r.Key.Replace("/", @"\/"), r.Value);


                 IEnumerable<Match> p = regexRoutes.Keys.Select(s => new Regex(s).Match(req.URL));

                 if (p.First().Success)
                 {
                     //trovato il routing regex
                     Terminal.INFO("lol");
                 }*/
            }
            return null;

        }

        public override string ToString()
        {
            return $"Port {port}\nStatic path : {staticFolderPath}\nMapping:\n";
        }


        public void GET(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.GET, func);

        public void POST(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.POST, func);

        public void HEAD(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.HEAD, func);

        public void PUT(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.PUT, func);

        public void DELETE(string path, Delegate func) => AddExpressMapping(path, HTTP_METHOD.DELETE, func);

        public void AddSharedObject(string name, object o) => sharedObjects.Add(name, o);

        public object GetSharedObject(string name) => sharedObjects[name];

    }
}
