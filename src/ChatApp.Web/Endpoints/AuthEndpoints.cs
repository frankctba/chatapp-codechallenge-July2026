using System.Security.Claims;
using ChatApp.Web.Data;
using ChatApp.Web.Models;
using ChatApp.Web.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Web.Endpoints;

public record RegisterRequest(string Username, string Password);
public record LoginRequest(string Username, string Password);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").RequireRateLimiting("auth");

        group.MapPost("/register", async (RegisterRequest req, ChatDbContext db) =>
        {
            if (!CredentialValidator.IsValidUsername(req.Username))
            {
                return Results.BadRequest($"Invalid username. {CredentialValidator.UsernameRulesDescription}");
            }

            if (!CredentialValidator.IsValidPassword(req.Password))
            {
                return Results.BadRequest($"Invalid password. {CredentialValidator.PasswordRulesDescription}");
            }

            var exists = await db.Users.AnyAsync(u => u.Username == req.Username);
            if (exists)
            {
                return Results.Conflict("That username's already taken.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = req.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok();
        });

        group.MapPost("/login", async (LoginRequest req, HttpContext http, ChatDbContext db) =>
        {
            var user = await db.Users.SingleOrDefaultAsync(u => u.Username == req.Username);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var claims = new List<Claim> { new(ClaimTypes.Name, user.Username) };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return Results.Ok(new { username = user.Username });
        });

        group.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok();
        });

        group.MapGet("/me", (HttpContext http) =>
            http.User.Identity?.IsAuthenticated == true
                ? Results.Ok(new { username = http.User.Identity!.Name })
                : Results.Unauthorized());
    }
}
