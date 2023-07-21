using System;
using System.Reflection;
using System.Runtime.InteropServices;
using MimeTypes;

namespace HSB
{
    public static class Utils
    {
        /* public static string GetClassName(this object caller)
         {
             return System.Reflection.MethodBase.GetCurrentMethod().DeclaringType;
         }*/
        public static void PrintLogo()
        {
            Terminal.Write("Welcome to ");
            Terminal.Write("H", BG_COLOR.DEFAULT, FG_COLOR.RED);
            Terminal.Write("S", BG_COLOR.DEFAULT, FG_COLOR.GREEN);
            Terminal.Write("B", BG_COLOR.DEFAULT, FG_COLOR.BLUE);
            Terminal.Write("-", BG_COLOR.DEFAULT, FG_COLOR.WHITE);
            Terminal.Write("#", BG_COLOR.DEFAULT, FG_COLOR.YELLOW);
            Terminal.WriteLine($" Server ({Assembly.GetExecutingAssembly().GetName().Version})");
        }

        public static void printLoadedAssemblies(bool filter = true)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            List<Assembly> assems = currentDomain.GetAssemblies().ToList();
            if (filter)
            {
                assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
                assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
                assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));
            }

            foreach (Assembly assem in assems)
            {
                List<Type> classes = assem.GetTypes().ToList();

                foreach (var c in classes)
                    Terminal.WriteLine(c.FullName, BG_COLOR.WHITE, FG_COLOR.BLUE);
            }
        }

        public static T Safe<T>(T? o, T safe) => o ?? safe!;

        public static string DictToString(this Dictionary<string, string> obj)
        {
            string s = "";
            foreach (var v in obj)
            {
                s += v.Key + " - " + v.Value + "\n";
            }
            return s;
        }

        public static string LoadResourceString(string resName)
        {
            var assembly = Assembly.GetCallingAssembly();
            string resourceName;

            try
            {

                resourceName = assembly.GetManifestResourceNames().First(str => str.EndsWith(resName));
            }
            catch (Exception)
            {
                return "";
            }


            string result;

            Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return "";
            }

            using (StreamReader reader = new(stream))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

    }
}

