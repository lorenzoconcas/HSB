
using System.Reflection;
using System.Text.Json;

namespace HSB
{
    public class HSBStandalone
    {

        private static void Main(string[] args)
        {
            HSBMain(args);
        }

        public static void HSBMain(string[] args)
        {
            Terminal.Write("HSB-# Standalone Preloader\n");

            Configuration conf = new Configuration();
            String path = "config.json";
            List<string> assemblies = new List<string>();

            if (args.Length > 0)
            {
                //sicuramente c'è un modo migliore per parsare gli argomenti
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

                        conf = new Configuration();
                        var str = JsonSerializer.Serialize(conf);//JsonConvert.SerializeObject(conf);
                        File.WriteAllText("./config.json", str);
                        return;
                    }
                    if (s.StartsWith("--assembly="))
                    {
                        assemblies.Add(s.Split("--assembly=")[1]);
                    }
                    if (s.StartsWith("--port="))
                    {
                        conf.port = int.Parse(s.Split("--port=")[1]);
                    }
                    if (s.StartsWith("--address="))
                    {
                        conf.address = s.Split("--address=")[1];
                    }
                    if (s.StartsWith("--info") || s.StartsWith("?"))
                    {
                        Utils.PrintLogo();
                        Terminal.WriteLine("Available commands :");
                        Terminal.WriteLine("--config-path : \tSpecifies a path for the json configuration of the server");
                        Terminal.WriteLine("--create-default : \tCreates a default configuration");
                        Terminal.WriteLine("--info, ? : \tShow this message screen");
                        Terminal.WriteLine("--no-verbose : \tDisables verbose writing");
                        Terminal.WriteLine("--port : \t Set server listening port");
                        Terminal.WriteLine("--address : \t Set server listening address");
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

            // Utils.printLoadedAssemblies();

            try
            {
                if (File.Exists(path))
                {
                    using (StreamReader r = new StreamReader(path))
                    {
                        string json = r.ReadToEnd();
                        Configuration? _conf = JsonSerializer.Deserialize<Configuration>(json);//JsonConvert.DeserializeObject<Configuration>(json);
                        if (_conf == null)
                        {
                            Terminal.ERROR("Invalid configuration file");
                            return;
                        }
                        conf = _conf;

                        if (conf.verbose)
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
                    //conf = new Configuration();
                }

            }
            catch (Exception e)
            {

                Terminal.ERROR("Errore while starting server ->\n" + e);
                return;
            }
            try
            {
                _ = new Server(conf!);
            }
            catch (Exception e)
            {
                Terminal.ERROR("Error starting server ->");
                Terminal.ERROR(e.Message);
            }

        }
    }
}