namespace HSB
{
    public enum BgColor
    {
        Default,
        Black,
        Red,
        Green,
        Yellow,
        Blue,
        Magenta,
        Cyan,
        White
    }
    public enum FgColor
    {
        Default,
        Black,
        Red,
        Green,
        Yellow,
        Blue,
        Magenta,
        Cyan,
        White
    }

    public static class Terminal
    {
        public static readonly string RESET = "\e[0m";
        /* private static string BRIGHT = "\e[1m";
         private static string DIM = "\e[2m";
         private static string UNDERSCORE = "\e[4m";
         private static string BLINK = "\e[5m";
         private static string REVERSE = "\e[7m";
         private static string HIDDEN = "\e[8m";*/

        public static void Write<T>(T o, BgColor background = BgColor.Default, FgColor foreground = FgColor.Default)
        {

            Console.Write(BG_TO_STRING(background));
            Console.Write(FG_TO_STRING(foreground));
            Console.Write(o);
            Console.Write(RESET);
        }
        public static void WriteLine<T>(T o, BgColor background = BgColor.Default, FgColor foreground = FgColor.Default)
        {

            Console.Write(BG_TO_STRING(background));
            Console.Write(FG_TO_STRING(foreground));
            Console.Write(o);
            Console.WriteLine(RESET);
        }
        public static void WriteLine()
        {
            Console.WriteLine();
        }
        public static void WriteLine<T>(T o)
        {
            Console.WriteLine(o);
        }
        public static void Write<T>(T o)
        {
            Console.Write(o);
        }
        public static void Info<T>(T o, bool printExtraInfo = false)
        {
            Log(o, $"{FG_TO_STRING(FgColor.Blue)}I{RESET}", FgColor.White, BgColor.Default, printExtraInfo);
        }
        public static void Warning<T>(T o, bool printExtraInfo = false)
        {
            Log(o, $"{FG_TO_STRING(FgColor.Yellow)}W{RESET}", FgColor.White, BgColor.Default, printExtraInfo);
        }
        public static void Error<T>(T o, bool printExtraInfo = false)
        {
            Log(o, $"{FG_TO_STRING(FgColor.Red)}E{RESET}", FgColor.White, BgColor.Default, printExtraInfo);
        }
        public static void Debug<T>(T o, bool printExtraInfo = false)
        {
            Log(o, $"{FG_TO_STRING(FgColor.Green)}I{RESET}", FgColor.White, BgColor.Default, printExtraInfo);
        }
        private static void Log<T>(T o, string lvl, FgColor foreground,
            BgColor background = BgColor.Black, bool printExtraInfo = false)
        {
            if (printExtraInfo)
            {
                Write($"[HSB {Environment.ProcessId}][{DateTime.Now:yyyy-MM-ddTHH:mm:ssZ}][{lvl}][", background, foreground);
            }
            Write(o, background, foreground);
            if (printExtraInfo)
                Write("]", background, foreground);
            WriteLine();
        }
        public static void Reset()
        {
            Console.ResetColor();
        }
        public static void EndColor()
        {
            Console.Write(RESET);
        }
        public static void StartColor(BgColor background = BgColor.Default, FgColor foreground = FgColor.Default)
        {
            Console.WriteLine(BG_TO_STRING(background));
            Console.WriteLine(FG_TO_STRING(foreground));
        }

        public static void Clear()
        {
            Console.Clear();
        }


        public static string BG_TO_STRING(BgColor color) => color switch
        {
            BgColor.Default => "",
            BgColor.Black => "\e[40m",
            BgColor.Red => "\e[41m",
            BgColor.Green => "\e[42m",
            BgColor.Yellow => "\e[43m",
            BgColor.Blue => "\e[44m",
            BgColor.Magenta => "\e[45m",
            BgColor.Cyan => "\e[46m",
            BgColor.White => "\e[47m",
            _ => ""
        };
        public static string FG_TO_STRING(FgColor color) => color switch
        {
            FgColor.Default => "",
            FgColor.Black => "\e[30m",
            FgColor.Red => "\e[31m",
            FgColor.Green => "\e[32m",
            FgColor.Yellow => "\e[33m",
            FgColor.Blue => "\e[34m",
            FgColor.Magenta => "\e[35m",
            FgColor.Cyan => "\e[36m",
            FgColor.White => "\e[37m",
            _ => ""
        };
    }
}
