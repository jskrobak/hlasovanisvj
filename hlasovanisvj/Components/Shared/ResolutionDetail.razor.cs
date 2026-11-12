using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using Microsoft.AspNetCore.Components;

namespace hlasovanisvj.Components.Shared;

public partial class ResolutionDetail(AppDbContext dbContext, IHxMessengerService messengerService) : ComponentBase
{
    [Parameter] public string FormId { get; set; } = "editForm";
    [Parameter] public Domain.Resolution Model { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }

    private HxOffcanvas resolutionDetailOffCanvasComponent;

    public async Task ShowAsync()
    {
        await resolutionDetailOffCanvasComponent.ShowAsync();
    }
    
    private async Task SaveResolution()
    {
        if(Model.Id == 0)
            await dbContext.AddAsync(Model);
        else
            dbContext.Update(Model);
        await dbContext.SaveChangesAsync();
        await resolutionDetailOffCanvasComponent.HideAsync();
        StateHasChanged();
        messengerService.AddInformation("Hlasování uloženo");
    }

    private async Task HandleClosed()
    {
        await OnClosed.InvokeAsync();
    }
}