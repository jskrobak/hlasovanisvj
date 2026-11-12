using System.Security.Claims;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj;

/// <summary>Zda je přihlášení přes Google nakonfigurované (Authentication:Google:ClientId/ClientSecret).</summary>
public record ExternalAuthInfo(bool GoogleEnabled);

/// <summary>
/// Přihlášení a registrace přes Google.
/// Google identitu nejdřív uložíme do krátkodobé cookie <see cref="ExternalScheme"/>;
/// teprve po spárování s účtem (nebo dokončení registrace) se přihlásí hlavní cookie.
/// </summary>
public static class GoogleAuth
{
    public const string ExternalScheme = "External";

    public static AuthenticationBuilder AddGoogleIfConfigured(this AuthenticationBuilder auth, IConfiguration config, out bool enabled)
    {
        var clientId = config["Authentication:Google:ClientId"];
        var clientSecret = config["Authentication:Google:ClientSecret"];
        enabled = !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);

        auth.AddCookie(ExternalScheme, options =>
        {
            options.Cookie.Name = ".hlasovanisvj.external";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        });

        if (enabled)
        {
            auth.AddGoogle(options =>
            {
                options.SignInScheme = ExternalScheme;
                options.ClientId = clientId!;
                options.ClientSecret = clientSecret!;
                options.ClaimActions.MapJsonKey("email_verified", "email_verified");
            });
        }

        return auth;
    }

    public static void MapGoogleAuth(this WebApplication app, bool enabled)
    {
        if (!enabled)
            return;

        // Start přihlášení / registrace přes Google.
        app.MapGet("/login/google", (string? returnUrl) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = $"/login/google/complete?returnUrl={Uri.EscapeDataString(LocalUrl(returnUrl))}" },
                [GoogleDefaults.AuthenticationScheme]));

        // Návrat z Google: spárovat s účtem, nebo poslat na dokončení registrace.
        app.MapGet("/login/google/complete", async (HttpContext http, string? returnUrl, AppDbContext db) =>
        {
            var external = await http.AuthenticateAsync(ExternalScheme);
            var identity = GoogleIdentity.From(external.Principal);
            if (!external.Succeeded || identity == null)
                return Results.Redirect("/login?loggedout=1&error=google");

            var user = await db.Users.FirstOrDefaultAsync(u => u.GoogleSubject == identity.Subject);
            if (user == null && identity.EmailVerified)
            {
                // Existující účet se stejným (Googlem ověřeným) e-mailem – propojit.
                user = await db.Users.FirstOrDefaultAsync(u => u.Email == identity.Email);
                if (user != null)
                {
                    user.GoogleSubject = identity.Subject;
                    await db.SaveChangesAsync();
                }
            }

            if (user == null)
                return Results.Redirect("/RegisterGoogle");

            await SignInAsync(http, user);
            return Results.Redirect(LocalUrl(returnUrl));
        });

        // Dokončení registrace (název organizace) pro nový účet Google.
        app.MapPost("/register/google", async (HttpContext http, AppDbContext db) =>
        {
            var external = await http.AuthenticateAsync(ExternalScheme);
            var identity = GoogleIdentity.From(external.Principal);
            if (!external.Succeeded || identity == null)
                return Results.Redirect("/login?loggedout=1&error=google");

            var form = await http.Request.ReadFormAsync();
            var organizationName = form["organizationName"].ToString().Trim();
            if (organizationName.Length is < 2 or > 200)
                return Results.Redirect("/RegisterGoogle?error=name");

            if (await db.Users.AnyAsync(u => u.Email == identity.Email || u.GoogleSubject == identity.Subject))
                return Results.Redirect("/RegisterGoogle?error=exists");

            var user = new User
            {
                Email = identity.Email,
                GoogleSubject = identity.Subject,
                Roles = ["User"],
                Organization = new Organization { Name = organizationName }
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            user.OrganizationId = user.Organization.Id;
            await db.SaveChangesAsync();

            await SignInAsync(http, user);
            return Results.Redirect("/Sessions");
        }).DisableAntiforgery(); // chráněno krátkodobou SameSite=Lax cookie s Google identitou

        // Zrušení rozpracované registrace přes Google.
        app.MapGet("/register/google/cancel", async (HttpContext http) =>
        {
            await http.SignOutAsync(ExternalScheme);
            return Results.Redirect("/login?loggedout=1");
        });
    }

    private static async Task SignInAsync(HttpContext http, User user)
    {
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, user.CreatePrincipal());
        await http.SignOutAsync(ExternalScheme);
    }

    /// <summary>Jen lokální cesty – ochrana proti open redirectu.</summary>
    private static string LocalUrl(string? url) =>
        !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\")
            ? url
            : "/Sessions";

    private sealed record GoogleIdentity(string Subject, string Email, bool EmailVerified)
    {
        public static GoogleIdentity? From(ClaimsPrincipal? principal)
        {
            var subject = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = principal?.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email))
                return null;

            var verified = string.Equals(principal!.FindFirstValue("email_verified"), "true", StringComparison.OrdinalIgnoreCase);
            return new GoogleIdentity(subject, email, verified);
        }
    }
}
