namespace HSB
{
    public enum BG_COLOR
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
    public enum FG_COLOR
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
        private static string RESET = "\x1b[0m";
        /* private static string BRIGHT = "\x1b[1m";
         private static string DIM = "\x1b[2m";
         private static string UNDERSCORE = "\x1b[4m";
         private static string BLINK = "\x1b[5m";
         private static string REVERSE = "\x1b[7m";
         private static string HIDDEN = "\x1b[8m";*/

        public static void Write<T>(T o, BG_COLOR background = BG_COLOR.DEFAULT, FG_COLOR foreground = FG_COLOR.DEFAULT)
        {

            Console.Write(BG_TO_STRING(background));
            Console.Write(FG_TO_STRING(foreground));
            Console.Write(o);
            Console.Write(RESET);
        }
        public static void WriteLine<T>(T o, BG_COLOR background = BG_COLOR.DEFAULT, FG_COLOR foreground = FG_COLOR.DEFAULT)
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
            LOG(o, "I", FG_COLOR.BLUE, BG_COLOR.DEFAULT, printExtraInfo);
        }
        public static void WARNING<T>(T o, bool printExtraInfo = false)
        {
            LOG(o, "W", FG_COLOR.YELLOW, BG_COLOR.BLACK, printExtraInfo);
        }
        public static void ERROR<T>(T o, bool printExtraInfo = false)
        {
            LOG(o, "E", FG_COLOR.RED, BG_COLOR.BLACK, printExtraInfo);
        }
        public static void DEBUG<T>(T o, bool printExtraInfo = false)
        {
            LOG(o, "D", FG_COLOR.GREEN, BG_COLOR.BLACK, printExtraInfo);
        }
        private static void LOG<T>(T o, string lvl, FG_COLOR foreground,
            BG_COLOR background = BG_COLOR.BLACK, bool printExtraInfo = false)
        {
            if (printExtraInfo)
            {
                Write($"[{DateTime.Now:yyyy/MM/dd # HH:mm:ss.ffff}][{lvl}][", background, foreground);
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
        public static void StartColor(BG_COLOR background = BG_COLOR.DEFAULT, FG_COLOR foreground = FG_COLOR.DEFAULT)
        {
            Console.WriteLine(BG_TO_STRING(background));
            Console.WriteLine(FG_TO_STRING(foreground));
        }

        public static void Clear()
        {
            Console.Clear();
        }


        private static string BG_TO_STRING(BG_COLOR color) => color switch
        {
            BG_COLOR.DEFAULT => "",
            BG_COLOR.BLACK => "\x1b[40m",
            BG_COLOR.RED => "\x1b[41m",
            BG_COLOR.GREEN => "\x1b[42m",
            BG_COLOR.YELLOW => "\x1b[43m",
            BG_COLOR.BLUE => "\x1b[44m",
            BG_COLOR.MAGENTA => "\x1b[45m",
            BG_COLOR.CYAN => "\x1b[46m",
            BG_COLOR.WHITE => "\x1b[47m",
            _ => ""
        };
        private static string FG_TO_STRING(FG_COLOR color) => color switch
        {
            FG_COLOR.DEFAULT => "",
            FG_COLOR.BLACK => "\x1b[30m",
            FG_COLOR.RED => "\x1b[31m",
            FG_COLOR.GREEN => "\x1b[32m",
            FG_COLOR.YELLOW => "\x1b[33m",
            FG_COLOR.BLUE => "\x1b[34m",
            FG_COLOR.MAGENTA => "\x1b[35m",
            FG_COLOR.CYAN => "\x1b[36m",
            FG_COLOR.WHITE => "\x1b[37m",
            _ => ""
        };
    }
}
