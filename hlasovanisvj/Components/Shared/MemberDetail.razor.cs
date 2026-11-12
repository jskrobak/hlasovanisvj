using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Components.Shared;

public partial class MemberDetail(AttendanceService attendanceService, 
    UserService  userService,
    MemberService  memberService,
    AppDbContext dbContext) : ComponentBase
{
    [Parameter] public string FormId { get; set; } = "editForm";
    [Parameter] public Domain.Member Model { get; set; } = null!;
    [Parameter] public EventCallback OnClosed { get; set; }
    
    private HxOffcanvas memberDetailOffCanvasComponent;
    private List<int> selectedPrincipalIds = new();
    private List<Member> allMembers = [];
    private int currentSessionId;

    protected override async Task OnInitializedAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        currentSessionId = user.Organization.CurrentSessionId ?? 0;

    }

    protected override async Task OnParametersSetAsync()
    {
        if (Model == null)
            return;
        
        selectedPrincipalIds = Model.Principals?.Select(p => p.Id).ToList() ?? [];
        allMembers = await dbContext.Members
            .Where(m => m.SessionId == currentSessionId && m.Id != Model.Id)
            .OrderBy(m => m.Id).ToListAsync();
    }

    public async Task ShowAsync()
    {
        await memberDetailOffCanvasComponent.ShowAsync();
    }
    
    private async Task SaveMember()
    {
        await memberService.SyncPrincipalsAsync(Model.Id, selectedPrincipalIds);
        
        await attendanceService.SavePresenceAsync(Model.Id, Model.IsPresent);
       
        await memberDetailOffCanvasComponent.HideAsync();
        
        StateHasChanged();
        await HandleClosed();
    }
    
    private async Task HandleClosed()
    {
        await OnClosed.InvokeAsync();
    }
}