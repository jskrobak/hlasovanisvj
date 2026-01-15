using System.Security.Claims;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj;

public abstract class AuthenticatedPageBase(AuthenticationStateProvider authenticationStateProvider, AppDbContext dbContext): ComponentBase
{
    protected User? CurrentUser { get; private set; }
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var email = user.FindFirst(ClaimTypes.Email)?.Value
                        ?? user.Identity.Name;
            
            CurrentUser = await dbContext.Users
                .AsNoTracking()
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == email);
            //CurrentUser = dbContext.Users.FirstOrDefault(u => u.Email == email);
        }
    }
}