namespace HSB
{
    public enum BgColor
    {
        DEFAULT,
        BLACK,
        RED,
        GREEN,
        YELLOW,
        BLUE,
        MAGENTA,
        CYAN,
        WHITE
    }
    public enum FgColor
    {
        DEFAULT,
        BLACK,
        RED,
        GREEN,
        YELLOW,
        BLUE,
        MAGENTA,
        CYAN,
        WHITE
    }

    public static class Terminal
    {
        private static readonly string RESET = "\e[0m";
        /* private static string BRIGHT = "\e[1m";
         private static string DIM = "\e[2m";
         private static string UNDERSCORE = "\e[4m";
         private static string BLINK = "\e[5m";
         private static string REVERSE = "\e[7m";
         private static string HIDDEN = "\e[8m";*/

        public static void Write<T>(T o, BgColor background = BgColor.DEFAULT, FgColor foreground = FgColor.DEFAULT)
        {

            Console.Write(BG_TO_STRING(background));
            Console.Write(FG_TO_STRING(foreground));
            Console.Write(o);
            Console.Write(RESET);
        }
        public static void WriteLine<T>(T o, BgColor background = BgColor.DEFAULT, FgColor foreground = FgColor.DEFAULT)
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
        public static void INFO<T>(T o, bool printExtraInfo = false)
        {
            LOG(o, "I", FgColor.BLUE, BgColor.DEFAULT, printExtraInfo);
        }
        public static void WARNING<T>(T o, bool printExtraInfo = false)
        {
            LOG(o, "W", FgColor.YELLOW, BgColor.BLACK, printExtraInfo);
        }
        public static void ERROR<T>(T o, bool printExtraInfo = false)
        {
            LOG(o, "E", FgColor.RED, BgColor.BLACK, printExtraInfo);
        }
        public static void DEBUG<T>(T o, bool printExtraInfo = false)
        {
            LOG(o, "D", FgColor.GREEN, BgColor.BLACK, printExtraInfo);
        }
        private static void LOG<T>(T o, string lvl, FgColor foreground,
            BgColor background = BgColor.BLACK, bool printExtraInfo = false)
        {
            if (printExtraInfo)
            {
                Write($"[{DateTime.Now:yyyy-MM-ddTHH:mm:ss.fffZ}][{lvl}][", background, foreground);
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
        public static void StartColor(BgColor background = BgColor.DEFAULT, FgColor foreground = FgColor.DEFAULT)
        {
            Console.WriteLine(BG_TO_STRING(background));
            Console.WriteLine(FG_TO_STRING(foreground));
        }

        public static void Clear()
        {
            Console.Clear();
        }


        private static string BG_TO_STRING(BgColor color) => color switch
        {
            BgColor.DEFAULT => "",
            BgColor.BLACK => "\e[40m",
            BgColor.RED => "\e[41m",
            BgColor.GREEN => "\e[42m",
            BgColor.YELLOW => "\e[43m",
            BgColor.BLUE => "\e[44m",
            BgColor.MAGENTA => "\e[45m",
            BgColor.CYAN => "\e[46m",
            BgColor.WHITE => "\e[47m",
            _ => ""
        };
        private static string FG_TO_STRING(FgColor color) => color switch
        {
            FgColor.DEFAULT => "",
            FgColor.BLACK => "\e[30m",
            FgColor.RED => "\e[31m",
            FgColor.GREEN => "\e[32m",
            FgColor.YELLOW => "\e[33m",
            FgColor.BLUE => "\e[34m",
            FgColor.MAGENTA => "\e[35m",
            FgColor.CYAN => "\e[36m",
            FgColor.WHITE => "\e[37m",
            _ => ""
        };
    }
}
