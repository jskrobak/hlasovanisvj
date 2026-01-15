using hlasovanisvj.Domain;

namespace hlasovanisvj.Services;

public interface ISecurityService
{
    bool VerifyPasswordHash(string passwordHash, string password);
    string HashPassword(string password);
    bool VerifyUser(User user, string password);
}