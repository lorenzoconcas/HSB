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
                    Object? o = GetInstance(req, res);
                    if (o != null)
                    {
                        Servlet servlet = (Servlet)o;
                        servlet.Process();
                    }
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
                            res.SendCode(404);
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
            Assembly[] assems = currentDomain.GetAssemblies();

            foreach (Assembly assem in assems)
            {
                var classes = assem.GetTypes();
                foreach (var c in classes)
                {
                    try
                    {
                        Binding? attr = c.GetCustomAttribute<Binding>(false);
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
            //se siamo qui non c'è una pagina di root preimpostata, restituiamo quella di default
            return new Index(req, res);
        }

        public override string ToString()
        {
            return $"Port {port}\nStatic path : {staticFolderPath}\nMapping:\n";
        }
    }
}
