using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Components.Shared;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;

namespace hlasovanisvj.Components.Pages;

public partial class Sessions(UserService userService,
    OrganizationService orgService,
    SessionService sessionService,
    IHxMessengerService messengerService,
    NavigationManager navigationManager,
    IHxMessageBoxService messageBoxService) : ComponentBase
{
    private Session? currentSession = null;
    private SessionFilter filterModel = new();
    private HxGrid<Session> gridComponent;
    private int? currentSessionId;
    private int organizationId;
    private SessionDetail sessionDetail;   

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
            return;
        organizationId = user.OrganizationId;
        currentSession = await sessionService.GetCurrentSessionAsync(organizationId);
        currentSessionId = currentSession?.Id;
        
        if(currentSessionId == null)
            messengerService.AddWarning("Vaše organizace nemá otevřenou žádnou schůzi. Pro otevření schůze klikněte na tlačítko 'Nová schůze'.");
    }

    async Task<GridDataProviderResult<Session>> GetGridData(GridDataProviderRequest<Session> req)
    {
        return await sessionService.GetGridData(req, organizationId, filterModel);
    }

    private async Task HandleSelectedDataItemChanged()
    {
        if (currentSession == null)
            return;
        sessionDetail.Model = currentSession;
        await sessionDetail.ShowAsync();
    }

    private async Task HandleNewSessionClick()
    {
        sessionDetail.Model = new Session
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            Name = "Nová schůze",
            OrganizationId =  organizationId
        };

        await sessionDetail.ShowAsync(); 
    }

    private async Task HandleOpenSessionButtonClick(int sessionId)
    {
        await orgService.SetCurrentSessionAsync(organizationId, sessionId);
        currentSessionId = sessionId;
        
        messengerService.AddInformation("Schůze otevřena");
        await gridComponent.RefreshDataAsync();
    }

    private async Task HandleCloseSessionButtonClick(int sessionId)
    {
        if (!await messageBoxService.ConfirmAsync(
                "Uzavřít schůzi? Případné probíhající hlasování se ukončí a schůzi už půjde jen prohlížet (nelze měnit účast, účastníky ani hlasovat)."))
            return;

        await sessionService.CloseSessionAsync(sessionId);
        messengerService.AddInformation("Schůze uzavřena");
        await gridComponent.RefreshDataAsync();
    }

    private async Task HandleDetailClosed()
    {
        await gridComponent.RefreshDataAsync();
    }

    private async Task HandleDeleteSessionButtonClick(int sessionId)
    {
        if (await messageBoxService.ConfirmAsync(
                "Opravdu chcete smazat tuto schůzi? Tato akce je nevratná a smaže všechny související záznamy (členy, hlasování)."))
        {
            await sessionService.DeleteSessionAsync(sessionId);
            await gridComponent.RefreshDataAsync();     
        }
    }
}