using HSB;
using HSB.Components.Attributes;
using HSB.Components.Controller;
using HSB.Constants;
using HSB.Modules;
using HSB.OpenApi.Attributes;

namespace JwtAuth;

[Controller("/")]
[ApiTag("Example Controller")]
class AuthenticationExample
{
    public Request req;
    private Response res;


    [Get("/")]
    public void Home()
    {
        res.SendHtmlContent(@"
    <html>
        <head>
            <title>Login</title>
        </head>
        <body>
            <h1>Login</h1>

            <form id='loginForm'>
                <div>
                    <label>Username</label>
                    <input type='text' id='username' />
                </div>

                <br />

                <div>
                    <label>Password</label>
                    <input type='password' id='password' />
                </div>

                <br />

                <button type='submit'>Login</button>
            </form>

            <pre id='result'></pre>

            <script>
                const form = document.getElementById('loginForm');
                const result = document.getElementById('result');

                form.addEventListener('submit', async (e) => {
                    e.preventDefault();

                    const username = document.getElementById('username').value;
                    const password = document.getElementById('password').value;

                    try {
                        const response = await fetch('/login', {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json'
                            },
                            body: JSON.stringify({
                                username,
                                password
                            })
                        });

                        const data = await response.json();

                        if (!response.ok)
                        {
                            result.innerText = JSON.stringify(data, null, 2);
                            return;
                        }

                        localStorage.setItem('accessToken', data.accessToken);

                        const adminResponse = await fetch('/admin', {
                            headers: {
                                'Authorization': 'Bearer ' + data.accessToken
                            }
                        });

                        const adminData = await adminResponse.json();

                        result.innerText = JSON.stringify(adminData, null, 2);
                    }
                    catch (err)
                    {
                        result.innerText = err.toString();
                    }
                });
            </script>
        </body>
    </html>
");
    }


    [Post("login")]
    [ApiSummary("Login endpoint, send any username and password to login")]
    [ApiDescription("")]
    public void Login([NamedParameter("username", true, true)] string username,
        [NamedParameter("password", true, true)] string password)
    {

        if (username == "" || password == "")
        {
            res.Json(new
            {
                result = "Error: Missing Username or Password"
            }, HttpCodes.BAD_REQUEST);
            return;
        }

        //your login logic

        AuthenticationManager.Instance.AddBearerToken("IMAFAKEBEARER");
        
        res.Json(new
        {
            accessToken = "IMAFAKEBEARER"
        });
    }


    [Get("admin")]
    [RequireAuth]
    public void AdminPage()
    {
        res.SendJson(new
        {
            result = "Logged"
        });
    }
}