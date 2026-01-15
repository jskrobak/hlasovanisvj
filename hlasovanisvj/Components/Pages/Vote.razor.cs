using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Components.Pages;

public partial class Vote(AppDbContext dbContext) : ComponentBase
{
    [Parameter]
    [SupplyParameterFromQuery]
    public Guid? Guid { get; set; }
    
    private Member _member = null!;
    private Resolution _resolution = null!;

    protected override async Task OnInitializedAsync()
    {
        var resolution = await dbContext.Resolutions
            .FirstOrDefaultAsync();
        
        if (Guid == null)
            return;
        
        _member = await dbContext.Members.FirstOrDefaultAsync(m => m.GlobalId == Guid);;
        if (_member is not { IsPresent: true } )
            return;
        
    }

    private Task HandleInFavorClick()
    {
        throw new NotImplementedException();
    }

    private Task HandleAgainstClick()
    {
        throw new NotImplementedException();
    }

    private Task HandleAbstainedClick()
    {
        throw new NotImplementedException();
    }
}