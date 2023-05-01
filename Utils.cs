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
        internal static void PrintLogo()
        {
            Terminal.Write("Welcome to ");
            Terminal.Write("H", BG_COLOR.DEFAULT, FG_COLOR.ROSSO);
            Terminal.Write("S", BG_COLOR.DEFAULT, FG_COLOR.VERDE);
            Terminal.Write("B", BG_COLOR.DEFAULT, FG_COLOR.BLU);
            Terminal.Write("-", BG_COLOR.DEFAULT, FG_COLOR.BIANCO);
            Terminal.Write("#", BG_COLOR.DEFAULT, FG_COLOR.GIALLO);
            Terminal.WriteLine($" Server ({Assembly.GetExecutingAssembly().GetName().Version})");
        }

        public static void printLoadedAssemblies()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            Assembly[] assems = currentDomain.GetAssemblies();

            foreach (Assembly assem in assems)
            {
                var classes = assem.GetTypes();
                foreach (var c in classes)
                    Terminal.WriteLine(c.FullName, BG_COLOR.BIANCO, FG_COLOR.BLU);
            }
        }
    }
}

