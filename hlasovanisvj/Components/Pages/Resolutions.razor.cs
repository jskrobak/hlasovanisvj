using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Components.Shared;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Components.Pages;

public partial class Resolutions(UserService userService, 
    IDbContextFactory<AppDbContext> dbFactory,
    IHxMessengerService messengerService,
    NavigationManager navigationManager,
    SessionService sessionService) : ComponentBase
{
    private Resolution? currentResolution = null;
    private ResolutionFilter filterModel = new();
    private HxGrid<Resolution> gridComponent;
    private int currentSessionId;
    private int? openedResolutionId;
    private ResolutionDetail resolutionDetail;
    private bool isReadOnly;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
            return;
        var currentOrganizationId = user.OrganizationId;
        var currentSession = await sessionService.GetCurrentSessionAsync(currentOrganizationId);

        currentSessionId = currentSession?.Id ?? 0;
        if (currentSessionId == 0)
        {
            navigationManager.NavigateTo("/");
            return;
        }
        openedResolutionId = currentSession?.OpenedResolutionId;
        isReadOnly = currentSession?.IsClosed == true;
    }


    async Task<GridDataProviderResult<Resolution>> GetGridData(GridDataProviderRequest<Resolution> req)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        var data = dbContext.Resolutions
            .AsNoTracking()
            .Where(r => r.SessionId == currentSessionId);
        
        if(!string.IsNullOrWhiteSpace(filterModel.Name))
            data = data.Where(p => EF.Functions.ILike(p.Title, $"%{filterModel.Name}%"));

        var count = await data.CountAsync();
        data = data.ApplyGridDataProviderRequest(req);
		
        return new GridDataProviderResult<Resolution>
        {
            Data = await data.ToListAsync(),
            TotalCount = count
        };
    }

    private async Task HandleSelectedDataItemChanged()
    {
        if (currentResolution == null)
            return;
        resolutionDetail.Model = currentResolution;
        await resolutionDetail.ShowAsync();
    }

    private async Task HandleNewResolutionClick()
    {
        resolutionDetail.Model = new Resolution
        {
            SessionId = currentSessionId,
            Title = "Nové hlasování"
        };

        await resolutionDetail.ShowAsync(); 
    }

    private async Task HandleOpenResolutionButtonClick(int resolutionId)
    {
        try
        {
            await sessionService.OpenResolutionAsync(currentSessionId, resolutionId);
            openedResolutionId = resolutionId;
        }
        catch (InvalidOperationException ex)
        {
            messengerService.AddError(ex.Message);
            return;
        }

        navigationManager.NavigateTo("/VoteLive");
    }

    private async Task HandleCloseResolutionButtonClick()
    {
        await sessionService.CloseResolutionAsync(currentSessionId);
        openedResolutionId = null;
        
        messengerService.AddInformation("Hlasování uzavřeno");
        
        await gridComponent.RefreshDataAsync();
    }

    private async Task HandleDetailClosed()
    {
        await gridComponent.RefreshDataAsync();
    }
}
