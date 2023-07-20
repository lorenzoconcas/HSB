using HSB;

namespace Manager.api
{
    [Binding("/api/login")]
    public class Login : Servlet
    {
        public Login(Request req, Response res) : base(req, res)
        {
        }
    }
}