using System.Security.Claims;
using hlasovanisvj.Domain;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace hlasovanisvj;

public static class Extensions
{
    public  static ClaimsPrincipal CreatePrincipal(this User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Email),   // used by IUserRepository.GetUserAsync
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
    
    public static VoteChoice? GetVoteChoice(this Member member, int resolutionId)
    {
        return member.Votes.FirstOrDefault(v => v.ResolutionId == resolutionId)?.Choice;
    }
}