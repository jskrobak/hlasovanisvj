using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;

namespace hlasovanisvj.Components.Pages;

public partial class Org(UserService userService, AppDbContext dbContext, IHxMessengerService messengerService) : ComponentBase
{
    private User user;
    private Organization organization;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        user = await userService.GetCurrentUserAsync();
        organization = user.Organization;
    }


    private async Task SaveOrg()
    {
        dbContext.Organizations.Update(organization);
        await dbContext.SaveChangesAsync();
        messengerService.AddInformation("Uloženo");
    }
}