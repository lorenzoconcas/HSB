using HSB;
internal class Program
{
    private static void Main(string[] args)
    {

        Configuration c = new()
        {
            port = 8080, //you must be root to listen on port 80, so 8080 will be used instead (see http alternate port)
            address = "" //with empty string the server will still listen to localhost but won't be available externally
        };
        // all servlets must contain the Binding decorator, which will tell
        // the server how to do the routing
        // the two routing "styles" (express and servlet) can be combined
        // but remember that in case of overlapping routes, the expressjs-like
        // will have priority


        new Server(c).Start();
    }
}