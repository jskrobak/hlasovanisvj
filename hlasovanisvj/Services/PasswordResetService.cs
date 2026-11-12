using System.Net;
using System.Security.Cryptography;
using System.Text;
using hlasovanisvj.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class PasswordResetService(
    IDbContextFactory<AppDbContext> dbFactory,
    ISecurityService securityService,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<PasswordResetService> logger)
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(1);

    /// <summary>
    /// Pošle odkaz na reset hesla, pokud účet existuje. Volající nedostane informaci,
    /// zda účet existuje (aby nešlo zjišťovat registrované e-maily).
    /// </summary>
    public async Task RequestResetAsync(string email)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim());
        if (user == null)
            return;

        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = Hash(token);
        user.PasswordResetExpiresAt = DateTime.UtcNow.Add(TokenLifetime);
        await db.SaveChangesAsync();

        var baseUrl = configuration["BaseUrl"]?.TrimEnd('/');
        var link = $"{baseUrl}/ResetPassword?uid={user.Id}&token={token}";

        try
        {
            await emailSender.SendAsync(user.Email, "Obnovení hesla – Hlasování SVJ",
                $"""
                <p>Dobrý den,</p>
                <p>někdo (pravděpodobně vy) požádal o obnovení hesla k účtu {WebUtility.HtmlEncode(user.Email)}.</p>
                <p><a href="{link}">Nastavit nové heslo</a></p>
                <p>Odkaz platí 1 hodinu. Pokud jste o obnovení nežádali, e-mail ignorujte.</p>
                """);
        }
        catch (Exception ex)
        {
            // Uživateli chybu neprozrazujeme (viz výše), jen ji zalogujeme.
            logger.LogError(ex, "Nepodařilo se odeslat e-mail pro obnovení hesla.");
        }
    }

    /// <summary>Ověří token a nastaví nové heslo. Vrací false pro neplatný nebo prošlý odkaz.</summary>
    public async Task<bool> ResetPasswordAsync(int userId, string token, string newPassword)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user?.PasswordResetTokenHash == null
            || user.PasswordResetExpiresAt < DateTime.UtcNow
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(user.PasswordResetTokenHash),
                Encoding.ASCII.GetBytes(Hash(token))))
            return false;

        user.PasswordHash = securityService.HashPassword(newPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiresAt = null;
        await db.SaveChangesAsync();
        return true;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
