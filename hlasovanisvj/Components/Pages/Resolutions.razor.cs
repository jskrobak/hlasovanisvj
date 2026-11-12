using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Components.Shared;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;

namespace hlasovanisvj.Components.Pages;

public partial class Resolutions(UserService userService, 
    AppDbContext dbContext,
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

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var user = await userService.GetCurrentUserAsync();
        var currentOrganizationId = user.OrganizationId;
        var currentSession = await sessionService.GetCurrentSessionAsync(currentOrganizationId);

        currentSessionId = currentSession?.Id ?? 0;
        openedResolutionId = currentSession?.OpenedResolutionId;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(currentSessionId == 0)
            navigationManager.NavigateTo("/");
    }

    async Task<GridDataProviderResult<Resolution>> GetGridData(GridDataProviderRequest<Resolution> req)
    {
        var data = dbContext.Resolutions
            .Where(r => r.SessionId == currentSessionId);
        
        if(!string.IsNullOrWhiteSpace(filterModel.Name))
            data = data.Where(p => p.Title.Contains(filterModel.Name));

        var count = data.Count();
        data = data.ApplyGridDataProviderRequest(req);
		
        return new GridDataProviderResult<Resolution>
        {
            Data = data,
            TotalCount = count
        };
    }

    private async Task HandleSelectedDataItemChanged()
    {
        resolutionDetail.Model = currentResolution!;
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
            await sessionService.SetOpenedResolutionAsync(currentSessionId, resolutionId);
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
        await NullCurrentResolutionAsync();
        
        messengerService.AddInformation("Hlasování uzavřeno");
        
        await gridComponent.RefreshDataAsync();
    }

    private async Task HandleDetailClosed()
    {
        await gridComponent.RefreshDataAsync();
    }
    
    private async Task NullCurrentResolutionAsync()
    {
        await sessionService.SetOpenedResolutionAsync(currentSessionId, null);
        openedResolutionId = null;
    }
}