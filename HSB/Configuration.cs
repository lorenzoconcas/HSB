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

        private static object? GetInstance(Request req, Response res)
        {


            AppDomain currentDomain = AppDomain.CurrentDomain;
            List<Assembly> assems = currentDomain.GetAssemblies().ToList();

            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));


            foreach (Assembly assem in assems)
            {
                List<Type> classes = assem.GetTypes().ToList();

                foreach (var c in classes)
                {
                    try
                    {
                        IEnumerable<Binding> multiBindings = c.GetCustomAttributes<Binding>(false);
                        Binding? attr;
                        //se non ci sono classi che hanno più binding, cerchiamo chi ne ha una sola
                        if (multiBindings.Any())
                        {
                            attr = multiBindings.First();
                        }
                        else
                        {
                            attr = c.GetCustomAttribute<Binding>(false);
                        }

                        if (attr != null && c.FullName != null && attr.Path == req.URL)
                            return Activator.CreateInstance(c, req, res);



                    }
                    catch (Exception e)
                    {
                        Terminal.WARNING(e);
                        //si è verificata un eccezione durante la creazione della servlet, mostriamo errore
                        return new Error(req, res, e.ToString(), 500);
                    }

                }
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

    }
}
