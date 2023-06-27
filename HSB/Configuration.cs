using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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

        public Configuration()
        {
            address = "127.0.0.1";
            port = 8080;
            staticFolderPath = "";
            verbose = true;
        }

        public Configuration(string address, int port, String staticPath, bool verbose)
        {

            this.address = address;
            this.port = port;
            staticFolderPath = staticPath;
            this.verbose = verbose;
        }


        public void Process(Request req, Response res)
        {
            new Task(() =>
            {
                try
                {
                    Terminal.INFO($"Requested '{req.URL}'", true);
                    //qui va scritta una logica di ricerca delle servlet migliore
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
                            new Index(req, res).Process();
                        else
                        {
                            //controlliamo se si cerca una risorsa, altrimenti 404 non trovato
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
                    Terminal.ERROR("Error handling request ->\n " + e);
                    Terminal.ERROR(e.Message);

                    new Error(req, res, e.ToString(), 500).Process();
                    return;
                }
            }).Start();
        }

        private object? GetInstance(Request req, Response res)
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
                        if (multiBindings.Any())
                        {
                            //non ci sono classi che hanno più binding, cerchiamo chi ne ha una sola
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
    }
}
