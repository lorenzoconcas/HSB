using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
            Terminal.Write(" (Http Server Boxed)");
            Terminal.WriteLine($" v{Assembly.GetExecutingAssembly().GetName().Version}");
        }

        public static void PrintLoadedAssemblies(bool filter = true)
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


        //source:https://stackoverflow.com/questions/3825390/effective-way-to-find-any-files-encoding

        /// <summary>
        /// Determines a text file's encoding by analyzing its byte order mark (BOM).
        /// Defaults to ASCII when detection of the text file's endianness fails.
        /// </summary>
        /// <param name="filename">The text file to analyze.</param>
        /// <returns>The detected encoding.</returns>
        public static Encoding GetEncoding(string filename)
        {
            // Read the BOM
            var bom = new byte[4];
            using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            return GetEncoding(bom);

        }

        public static Encoding GetEncoding(byte[] file)
        {
            var bom = new byte[4];
            bom[0] = file[0];
            bom[1] = file[1];
            bom[2] = file[2];
            bom[3] = file[3];

            // Analyze the BOM

            // if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
            if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE



            // We actually have no idea what the encoding is if we reach this point, so
            // you may wish to return null instead of defaulting to ASCII
            return Encoding.ASCII;
        }


        public static string GenerateRandomString(int size, bool onlyUpperCase = false, bool useNumbers = true, bool useSpecialCharacters = true)
        {

            Random random = new();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (!onlyUpperCase)
                chars += "abcdefghijklmnopqrstuvwxyz";

            if (useNumbers)
                chars += "0123456789";

            if (useSpecialCharacters)
                chars += "!@#$%^&*()_+-=[]{}|;:,.<>?";


            string rndStr = "";
            for (int i = 0; i < size; i++)
            {
                rndStr += chars[random.Next(chars.Length)];
            }

            return rndStr;
        }



    }
}

