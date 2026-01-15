using System.Security.Cryptography;
using hlasovanisvj.Domain;

namespace hlasovanisvj.Services;

public class SecurityService: ISecurityService
{
    public bool VerifyPasswordHash(string passwordHash, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash); 
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool VerifyUser(User user, string password)
    {
        return !string.IsNullOrEmpty(user.PasswordHash) && VerifyPasswordHash(user.PasswordHash, password);
            return true;
    }

}