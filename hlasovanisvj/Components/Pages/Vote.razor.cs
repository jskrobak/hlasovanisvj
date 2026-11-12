using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Components.Pages;

public partial class Vote(AppDbContext dbContext,
    IHxMessengerService messengerService,
    VoteService voteService) : ComponentBase
{
    [Parameter]
    [SupplyParameterFromQuery]
    public Guid? Guid { get; set; }

    private HxButton btnInFavor;
    private HxButton btnAgainst;
    private HxButton btnAbstained;
    
    private Member? _member = null!;
    private Resolution? _resolution = null!;

    protected override async Task OnInitializedAsync()
    {
        if(Guid == null)
        {
            messengerService.AddError("No member identifier provided.");
            return;
        }

        _member = await dbContext.Members
            .Include(m => m.Session)
            .ThenInclude(o => o.OpenedResolution)
            .FirstOrDefaultAsync(m => m.GlobalId == Guid && m.IsPresent); //TODO: exclude not presented members

        if(_member == null)
        {
            messengerService.AddError("Member not found.");
            return;
        }

        if (!_member.IsPresent)
        {
            messengerService.AddError("Member is not present.");
            return;
        }
        
        if (_member.ProxyId != null)
        {
            // Za zmocnitele hlasuje zmocněnec – vlastním lístkem by jeho hlas přepsal.
            messengerService.AddWarning("Udělil(a) jste plnou moc, hlasuje za vás zmocněnec.");
            _member = null;
            return;
        }

        _resolution = _member?.Session.OpenedResolution;
        
        if(_resolution == null)
        {
            messengerService.AddError("No open resolution found.");
            return;
        }
    }
    
    private async Task HandleInFavorClick()
    {
        await SaveVote(VoteChoice.InFavor);
        
    }

    private async Task HandleAgainstClick()
    {
        await SaveVote(VoteChoice.Against); 
    }

    private async Task HandleAbstainedClick()
    {
        await SaveVote(VoteChoice.Abstained);
    }
    
    private async Task SaveVote(VoteChoice choice)
    {
        try{
            await voteService.MakeVoteAsync(_member!.Id, _resolution!.Id, choice);
            messengerService.AddInformation("Vote recorded. Thank you!");
        }
        catch (Exception ex)
        {
            messengerService.AddError("Hlas se nepodařilo uložit: " + ex.Message);
        }
    }
}