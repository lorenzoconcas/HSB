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
            Terminal.Write("H", BG_COLOR.DEFAULT, FG_COLOR.ROSSO);
            Terminal.Write("S", BG_COLOR.DEFAULT, FG_COLOR.VERDE);
            Terminal.Write("B", BG_COLOR.DEFAULT, FG_COLOR.BLU);
            Terminal.Write("-", BG_COLOR.DEFAULT, FG_COLOR.BIANCO);
            Terminal.Write("#", BG_COLOR.DEFAULT, FG_COLOR.GIALLO);
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
                    Terminal.WriteLine(c.FullName, BG_COLOR.BIANCO, FG_COLOR.BLU);
            }
        }

        public static T Safe<T>(T? o, T safe) => o ?? safe!;

    }
}

