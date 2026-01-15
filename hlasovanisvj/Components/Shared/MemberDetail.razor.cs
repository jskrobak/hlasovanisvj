using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Components.Shared;

public partial class MemberDetail(AttendanceService attendanceService) : ComponentBase
{
    [Parameter] public string FormId { get; set; } = "editForm";
    [Parameter] public Domain.Member Model { get; set; } = null!;
    [Inject] protected AppDbContext DbContext { get; set; }
    
    private HxOffcanvas memberDetailOffCanvasComponent;
    private List<int> selectedPrincipalIds = new();
    private List<Member> allMembers = [];

    protected override async Task OnInitializedAsync()
    {
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Model == null)
            return;
        
        selectedPrincipalIds = Model.Principals?.Select(p => p.Id).ToList() ?? [];
        allMembers = await DbContext.Members.Where(m => m.Id != Model.Id)
            .OrderBy(m => m.Id).ToListAsync();
    }

    public async Task ShowAsync()
    {
        await memberDetailOffCanvasComponent.ShowAsync();
    }
    
    private async Task SaveMember()
    {
        Model.Principals = await DbContext.Members
            .Where(m => selectedPrincipalIds.Contains(m.Id))
            .ToListAsync();
        
        await attendanceService.SavePresenceAsync(Model);
        //DbContext.Members.Update(Model);
        //await DbContext.SaveChangesAsync();
        await memberDetailOffCanvasComponent.HideAsync();
    }
}