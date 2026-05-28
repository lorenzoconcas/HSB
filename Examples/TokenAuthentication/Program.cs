using HSB;
using HSB.Components.Controller;
using HSB.Modules;
using HSB.Utils;

var c = new Configuration();

AuthenticationSettings.Instance.EnableBearer = true;
AuthenticationManager.Instance.AddBasicUser("admin", "password123", ["admin"]);
AuthenticationManager.Instance.AddBearerToken("demo-admin-token", "admin", ["admin"]);

new Server(c).Start();
