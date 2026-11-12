using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;

namespace hlasovanisvj.Components.Shared;

public partial class MemberDetail(AttendanceService attendanceService, 
    MemberService memberService,
    PdfService pdfService,
    IMemoryCache memoryCache,
    NavigationManager navigationManager,
    IHxMessengerService messengerService,
    IHxMessageBoxService messageBoxService) : ComponentBase
{
    [Parameter] public string FormId { get; set; } = "memberEditForm";
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }

    private Member? Model { get; set; }
    private HxOffcanvas memberDetailOffCanvasComponent = null!;
    private List<int> selectedPrincipalIds = [];
    private List<Member>? allMembers;

    private string Title => Model switch
    {
        { IsGuest: true, Id: 0 } => "Nový host",
        { IsGuest: true } => "Host",
        _ => "Účastník"
    };

    public async Task ShowAsync(Member member)
    {
        Model = member;
        selectedPrincipalIds = member.Principals.Select(p => p.Id).ToList();
        allMembers = await memberService.GetPrincipalCandidatesAsync(member.SessionId, member.Id);
        StateHasChanged();
        await memberDetailOffCanvasComponent.ShowAsync();
    }
    
    private async Task SaveMember()
    {
        if (Model == null || ReadOnly)
            return;

        try
        {
            if (Model.Id == 0)
            {
                var guest = await memberService.AddGuestAsync(Model.SessionId, Model.Name);
                Model.Id = guest.Id;
            }
            else if (Model.IsGuest)
            {
                await memberService.UpdateGuestNameAsync(Model.Id, Model.Name);
            }

            await memberService.SyncPrincipalsAsync(Model.Id, selectedPrincipalIds);
            await attendanceService.SavePresenceAsync(Model.Id, Model.IsPresent);
        }
        catch (InvalidOperationException ex)
        {
            messengerService.AddError(ex.Message);
            return;
        }
       
        await memberDetailOffCanvasComponent.HideAsync();
    }

    private async Task DeleteGuest()
    {
        if (Model == null || !await messageBoxService.ConfirmAsync($"Odebrat hosta {Model.Name}? Zmocnitelé zůstanou bez zmocněnce."))
            return;

        try
        {
            await memberService.DeleteGuestAsync(Model.Id);
        }
        catch (InvalidOperationException ex)
        {
            messengerService.AddError(ex.Message);
            return;
        }

        await memberDetailOffCanvasComponent.HideAsync();
    }

    private void PrintTicket()
    {
        if (Model == null)
            return;

        var data = pdfService.GeneratePdf([Model]);
        var guid = Guid.NewGuid();
        memoryCache.Set(guid, data, TimeSpan.FromMinutes(10));
        navigationManager.NavigateTo($"download?guid={guid}", forceLoad: true);
    }
    
    private async Task HandleClosed()
    {
        await OnClosed.InvokeAsync();
    }
}
