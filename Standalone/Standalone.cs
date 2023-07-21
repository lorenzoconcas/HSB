using System.Reflection;
using System.Text.Json;
using HSB;
namespace HSBStandalone
{
    public class HSBStandalone
    {

        private static void Main(string[] args)
        {
            //#if DEBUG
            //string[] fakeArgs = new[] { "--create-default" };
            //HSBMain(fakeArgs);
            //#else*/
            HSBMain(args);
            //#endif

        }

        public static void HSBMain(string[] args)
        {
            Terminal.Write("HSB-# Standalone Preloader\n");

            Configuration conf = new();
            string path = "./config.json";
            List<string> assemblies = new();

            if (args.Length > 0)
            {
                //sicuramente c'è un modo migliore per parsare gli argomenti

                foreach (string s in args)
                {
                    if (s.StartsWith("--no-verbose"))
                    {
                        conf.debug = new Debugger();
                        conf.debug.verbose = false;
                    }
                    if (s.StartsWith("--config-path="))
                    {
                        path = s.Split("--config-path=")[1];
                    }
                    if (s.StartsWith("--create-default"))
                    {
                        Console.WriteLine("Creating default configuration and exiting...");

                        conf = new Configuration
                        {
                            address = "127.0.0.1",
                            port = 8080,
                            staticFolderPath = "./static"
                        };
                        JsonSerializerOptions sr = new()
                        {
                            IncludeFields = true
                        };
                        var str = JsonSerializer.Serialize(conf, sr);//JsonConvert.SerializeObject(conf);
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
                    using StreamReader r = new(path);
                    string json = r.ReadToEnd();

                    try
                    {
                        conf = new Configuration(json);
                    }
                    catch (Exception e)
                    {

                        Terminal.ERROR("Invalid configuration file");
                        Terminal.ERROR(e);
                        return;
                    }

                    if (conf.debug.verbose)
                    {
                        Terminal.INFO("Configuration file loaded successfully:");
                        Console.WriteLine(conf.ToString());
                    }
                }
                else
                {
                    Terminal.WARNING("Configuration file not found");
                    Terminal.WriteLine("Insert one or create one with the following commands:");
                    Terminal.WriteLine("Usage :");
                    Terminal.WriteLine("\t--config-path : \tSpecifies a path for the json configuration of the server");
                    Terminal.WriteLine("\t--create-default : \tCreates a default configuration");
                    Terminal.WriteLine("\t--info : \tShow this message screen");
                    Terminal.WriteLine("\t--no-verbose : \tDisables verbose writing");
                    Terminal.WriteLine("\t--port : \t Set server listening port");
                    Terminal.WriteLine("\t--address : \t Set server listening address");
                    Terminal.WriteLine("\t--assembly : \tUse it to load custom assemblies (use it to run without embedding HSB");
                    Terminal.WriteLine("HSB-# will start with default failsafe configuration\n\n");
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
                new Server(conf!).Start();
            }
            catch (Exception e)
            {
                Terminal.ERROR("Error starting server ->");
                Terminal.ERROR(e.Message);
            }

        }
    }
}