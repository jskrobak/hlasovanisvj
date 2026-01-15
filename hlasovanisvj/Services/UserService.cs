using System.Security.Claims;
using hlasovanisvj.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class UserService(AppDbContext dbContext, AuthenticationStateProvider authProvider)
{
    public async Task<Domain.User?> GetCurrentUserAsync()
    {
        var authState = await authProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var email = user.FindFirst(ClaimTypes.Email)?.Value
                        ?? user.Identity.Name;
            
            return await dbContext.Users
                .AsNoTracking()
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email);
            //CurrentUser = dbContext.Users.FirstOrDefault(u => u.Email == email);
        }
        
        return null;
    }
}