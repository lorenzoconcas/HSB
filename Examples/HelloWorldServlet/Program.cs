using HSB;
namespace HelloWorldExample;
public class Program
{
    private static void Main()
    {

        Configuration c = new()
        {
            Port = 8080, //you must be root to listen on port 80, so 8080 will be used instead (see http alternate port)
            Address = "" //with empty string the server will still listen any address
        };

        c.Get("/", (Response res) =>
        {
            res.SendHtmlContent("<h1>Hello world</h1>");
        });


        new Server(c).Start();
    }
}
