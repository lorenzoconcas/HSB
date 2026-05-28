using BackofficeDemo.Backend.Contracts.Requests;
using BackofficeDemo.Backend.Infrastructure;
using HSB;
using HSB.Components.Controller;
using HSB.Modules;

namespace BackofficeDemo.Backend.Controllers;

[Controller("/api/auth")]
public sealed class AuthController
{
    public Request req = null!;
    private Response res = null!;

    [Post("/login")]
    public void Login()
    {
        if (!RequestJson.TryRead<LoginRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        if (string.IsNullOrWhiteSpace(request!.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            ApiResponses.ValidationError(res, "Username and password are required.");
            return;
        }

        var login = BackofficeApplication.Current.AuthService.Login(request.Username.Trim(), request.Password);
        if (login == null)
        {
            res.Json(new
            {
                error = "Unauthorized"
            }, 401);
            return;
        }

        BackofficeApplication.Current.ActivityService.Record("auth.login", login.Username,
            "User logged in successfully.", login.Username);
        res.SendJson(login);
    }

    [Post("/logout")]
    [RequireAuth]
    public void Logout()
    {
        var authContext = req.GetAuthContext();
        BackofficeApplication.Current.AuthService.Logout(authContext?.Token ?? string.Empty);
        BackofficeApplication.Current.ActivityService.Record("auth.logout", authContext?.Username ?? "unknown",
            "User logged out.", authContext?.Username ?? "unknown");
        res.SendJson(new
        {
            ok = true
        });
    }

    [Get("/me")]
    [RequireAuth]
    public void Me()
    {
        var currentUser = BackofficeApplication.Current.AuthService.GetCurrentUser(req.GetAuthContext());
        if (currentUser == null)
        {
            res.Json(new
            {
                error = "Unauthorized"
            }, 401);
            return;
        }

        res.SendJson(currentUser);
    }
}
