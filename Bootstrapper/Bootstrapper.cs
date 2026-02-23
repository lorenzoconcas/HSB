//you can use this as launch point for you projects

using System.Reflection;
using System.Text.Json;
using HSB;
using HSB.Utils;

namespace Bootstrapper;

public class Bootstrapper
{
    private static readonly JsonSerializerOptions sr = new()
    {
        IncludeFields = true,
        WriteIndented = true,
    };

    private static void Main(string[] args)
    {
        //#if DEBUG
        string[] fakeArgs = args;
#if DEBUG
        fakeArgs = ["--create-default"];
        // fakeArgs = new[] { "--address=''" };
#endif
        HsbMain(fakeArgs);
        //#endif
    }

    public static void HsbMain(string[] args)
    {
        List<string> assemblies = [];
        List<string> _args = new(args);

        Terminal.Write("====================================================");
        Terminal.Write("\nHSB-# Standalone Bootstrapper\n");

        Configuration conf = new();
        string path = "./config.json";

        if (_args.Count != 0)
        {
            //print args
            Terminal.Info("Arguments passed to the bootstrap:");
            foreach (string s in _args)
            {
                Terminal.Info("\t" + s);
            }

            foreach (var s in _args)
            {
                if (s.StartsWith("--no-verbose"))
                {
                    conf.Debug = new Debugger
                    {
                        verbose = false
                    };
                }

                if (s.StartsWith("--config-path="))
                {
                    path = s.Split("--config-path=")[1];
                }

                if (s.StartsWith("--create-default"))
                {
                    Console.WriteLine("Creating default configuration and exiting...");

                    conf = new Configuration();
                    conf.AddCustomGlobalHeader("content-encoding", "utf-8");

                    var str = JsonSerializer.Serialize(conf, sr); //JsonConvert.SerializeObject(conf);
                    File.WriteAllText(path, str);
                    Terminal.Info("This is the default configuration : ");
                    Terminal.Info(str);
                    Terminal.WriteLine("Goodbye\n====================================================");
                    return;
                }

                if (s.StartsWith("--assembly="))
                {
                    assemblies.Add(s.Split("--assembly=")[1]);
                }

                if (s.StartsWith("--port="))
                {
                    conf.Port = ushort.Parse(s.Split("--port=")[1]);
                }

                if (s.StartsWith("--address="))
                {
                    conf.Address = s.Split("--address=")[1];
                }

                if (!s.StartsWith("--info") && !s.StartsWith('?')) continue;
                CliUtils.PrintLogo();
                Terminal.WriteLine("Available commands :");
                Terminal.WriteLine("--config-path : \tSpecifies a path for the json configuration of the server");
                Terminal.WriteLine("--create-default : \tCreates a default configuration");
                Terminal.WriteLine("--info, ? : \tShow this message screen");
                Terminal.WriteLine("--no-verbose : \tDisables verbose writing");
                Terminal.WriteLine("--port : \t Set server listening port");
                Terminal.WriteLine("--address : \t Set server listening address");
                Terminal.WriteLine(
                    "--assembly : \tUse it to load custom assemblies (use it to run without embedding HSB");
                return;
            }
        }

        if (assemblies.Count > 0)
        {
            Terminal.Info("Loading following assemblies:");
            foreach (var a in assemblies)
            {
                Terminal.Info("\t" + a);
                if (a != "")
                    Assembly.LoadFile(Path.GetFullPath(a));
            }
        }

        // Utils.printLoadedAssemblies();

        if (File.Exists(path))
        {
            using StreamReader r = new(path);
            var json = r.ReadToEnd();

            try
            {
                conf = new Configuration(json);
            }
            catch (Exception e)
            {
                Terminal.Error("Invalid configuration file");
                Terminal.Error(e);
                return;
            }

            if (conf.Debug.verbose)
            {
                Terminal.Info("Configuration file loaded successfully, preview:");
                Terminal.Info(json);
            }
        }
        else
        {
            Terminal.Warning("Configuration file not found");
            Terminal.WriteLine("Insert one or create one with the following commands:");
            Terminal.WriteLine("Usage :");
            Terminal.WriteLine("\t--config-path : \tSpecifies a path for the json configuration of the server");
            Terminal.WriteLine("\t--create-default : \tCreates a default configuration");
            Terminal.WriteLine("\t--info : \tShow this message screen");
            Terminal.WriteLine("\t--no-verbose : \tDisables verbose writing");
            Terminal.WriteLine("\t--port : \t Set server listening port");
            Terminal.WriteLine("\t--address : \t Set server listening address");
            Terminal.WriteLine(
                "\t--assembly : \tUse it to load custom assemblies (use it to run without embedding HSB");
            Terminal.WriteLine("HSB-# will start with default failsafe configuration\n\n");
            //conf = new Configuration();
        }

        Terminal.Info("The bootstrap has finished, HSB will start now");
        Terminal.Write("====================================================\n");


        try
        {
            new Server(conf).Start();
        }
        catch (Exception e)
        {
            Terminal.Error("Error starting server ->");
            Terminal.Error(e.Message);
            Terminal.Error("The server has crashed! Exiting...");
            return;
        }
    }
}