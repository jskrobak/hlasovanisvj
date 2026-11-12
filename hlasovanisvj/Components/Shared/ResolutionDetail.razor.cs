using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Components.Shared;

public partial class ResolutionDetail(IDbContextFactory<AppDbContext> dbFactory, IHxMessengerService messengerService) : ComponentBase
{
    [Parameter] public string FormId { get; set; } = "editForm";
    [Parameter] public Domain.Resolution Model { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }
    [Parameter] public bool ReadOnly { get; set; }

    private HxOffcanvas resolutionDetailOffCanvasComponent;

    public async Task ShowAsync()
    {
        await resolutionDetailOffCanvasComponent.ShowAsync();
    }
    
    private async Task SaveResolution()
    {
        if (ReadOnly)
            return;

        await using var dbContext = await dbFactory.CreateDbContextAsync();
        try
        {
            await dbContext.EnsureSessionOpenAsync(Model.SessionId);
        }
        catch (InvalidOperationException ex)
        {
            messengerService.AddError(ex.Message);
            return;
        }

        if (Model.Id == 0)
        {
            dbContext.Resolutions.Add(Model);
        }
        else
        {
            var resolution = await dbContext.Resolutions.FindAsync(Model.Id);
            if (resolution == null)
                return;
            resolution.Title = Model.Title;
            resolution.Question = Model.Question;
        }
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