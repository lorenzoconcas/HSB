using Newtonsoft.Json;
using System.Reflection;

namespace HSB
{
    public class Server
    {

        private static void Main(string[] args)
        {
            Terminal.Write("HSB-# Standalone Preloader\n");

            Configuration? conf;
            String path = "config.json";
            List<string> assemblies = new List<string>();

            if (args.Length > 0)
            {
                bool verbose = true;
                foreach (string s in args)
                {
                    if (s.StartsWith("--no-verbose"))
                    {
                        verbose = false;
                    }
                    if (s.StartsWith("--config-path="))
                    {
                        path = s.Split("--config-path=")[1];
                    }
                    if (s.StartsWith("--create-default"))
                    {
                        Console.WriteLine("Creating default configuration and exiting...");

                        conf = new Configuration("127.0.0.1", 8080, "", verbose);
                        var str = JsonConvert.SerializeObject(conf);
                        File.WriteAllText("./config.json", str);
                        return;
                    }
                    if (s.StartsWith("--assembly="))
                    {
                        assemblies.Add(s.Split("--assembly=")[1]);
                    }
                    if (s.StartsWith("--info") || s.StartsWith("?"))
                    {
                        Utils.PrintLogo();
                        Terminal.WriteLine("Available commands :");
                        Terminal.WriteLine("--config-path : \tSpecifies a path for the json configuration of the server");
                        Terminal.WriteLine("--create-default : \tCreates a default configuration");
                        Terminal.WriteLine("--info, ? : \tShow this message screen");
                        Terminal.WriteLine("--no-verbose : \tDisables verbose writing");
                        Terminal.WriteLine("--assembly : \tUse it to load custom assemblies (use it to run without embedding HSB");
                        return;
                    }

                }
            }
            if (assemblies.Count > 0)
            {
                Terminal.INFO("Loading following assemblies:");
                foreach (var a in assemblies)
                {
                    Terminal.INFO("\t" + a);
                    if (a != "")
                        Assembly.LoadFile(Path.GetFullPath(a));
                }
            }

            Utils.printLoadedAssemblies();
            
            try
            {
                if (File.Exists(path))
                {
                    using (StreamReader r = new StreamReader(path))
                    {
                        string json = r.ReadToEnd();
                        conf = JsonConvert.DeserializeObject<Configuration>(json);
                        if (conf != null && conf!.verbose)
                        {

                            Console.WriteLine("Configuration file loaded");
                            Console.WriteLine(conf.ToString());
                        }
                    }
                }
                else
                {
                    Terminal.WARNING("Configuration file not found");
                    Terminal.WriteLine("Insert one or create one with the following commands:");
                    Terminal.WriteLine("Usage : \n" +
                        "\t--create-default (Create a default configuration file) \n" +
                        "\t--config-path=path (Loads configuration file from specified path)\n");

                    Terminal.WriteLine("HSB-# will start with default configuration\n");
                    conf = new Configuration("127.0.0.1", 8080, "", true);
                }

            }
            catch (Exception e)
            {

                Terminal.ERROR("Errore while starting server ->\n" + e);
                return;
            }
            try
            {
                _ = new ServerEngine(conf!);
            }
            catch (Exception e)
            {
                Terminal.ERROR("Invalid configuration file");
                Terminal.ERROR(e.Message);
            }

        }
    }

    //public class MainServlet : Servlet
    //{
    //    public MainServlet(Request req, Response res) : base(req, res)
    //    {
    //        // Console.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    //    }

    //    public override void ProcessGet(Request r, Response res)
    //    {
    //        //res.SendCode(404);
    //        res.Send("ciao!", "text/plain");
    //        //base.ProcessGet(r, res);
    //    }
    //}
}