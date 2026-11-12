using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;

namespace hlasovanisvj.Components.Shared;

public partial class SessionDetail(SessionService sessionService, IHxMessengerService messengerService) : ComponentBase
{
    [Parameter] public string FormId { get; set; } = "editForm";
    [Parameter] public Domain.Session Model { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }

    private HxOffcanvas sessionDetailOffCanvasComponent;

    public async Task ShowAsync()
    {
        await sessionDetailOffCanvasComponent.ShowAsync();
    }
    
    private async Task SaveResolution()
    {
        await sessionService.SaveSessionAsync(Model);
        
        await sessionDetailOffCanvasComponent.HideAsync();
        StateHasChanged();
        messengerService.AddInformation("Schůze uložena");
    }

    private async Task HandleClosed()
    {
        await OnClosed.InvokeAsync();
    }
}