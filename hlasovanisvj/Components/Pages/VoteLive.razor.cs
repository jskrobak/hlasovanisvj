using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Models;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Components.Pages;

public partial class VoteLive(UserService userService, 
    VoteService voteService, 
    AppDbContext dbContext,
    NavigationManager navigationManager,
    MemberService memberService,
    AttendanceService attendanceService,
    SessionService sessionService,
    IHxMessengerService messengerService) : ComponentBase, IAsyncDisposable
{
    private int resolutionId;
    private Resolution? _resolution;

    private HubConnection? _hub;
    private VoteLiveStats? _stats;
    //private Resolution? _;
    private Member? currentMember = null;
    private HxGrid<Member> gridComponent;
    private double totalPresentShare = 0;
    private int currentSessionId;
    private MemberFilter filterModel = new();
    private List<Member> presentMembers = [];

    protected override async Task OnInitializedAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
            return;

        var session = await sessionService.GetCurrentSessionAsync(user.OrganizationId);
        if (session?.OpenedResolutionId == null)
            return;

        resolutionId = session.OpenedResolutionId.Value;
        currentSessionId = session.Id;

        _resolution = dbContext.Resolutions.FirstOrDefault(r => r.Id == resolutionId);
        
        await LoadAsync();

        _hub = new HubConnectionBuilder()
            .WithUrl(navigationManager.ToAbsoluteUri("/hubs/vote"))
            .WithAutomaticReconnect()
            .Build();

        _hub.On<int>("VoteChanged", async _ =>
        {
            await LoadAsync();
            await InvokeAsync(gridComponent.RefreshDataAsync);
            await InvokeAsync(StateHasChanged);
        });

        await _hub.StartAsync();
        await _hub.InvokeAsync("JoinResolution", resolutionId);
    }

    private async Task LoadAsync()
    {
        presentMembers = await memberService.GetPresentWithChoiseAsync(currentSessionId);
        totalPresentShare = await attendanceService.GetTotalPresentShareAsync(currentSessionId);
        _stats = await voteService.GetLiveStatsAsync(resolutionId, totalPresentShare);
    }
    

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
        {
            try { await _hub.InvokeAsync("LeaveResolution", resolutionId); } catch { }
            await _hub.DisposeAsync();
        }
    }
    
    private Task<GridDataProviderResult<Member>> GetGridData(GridDataProviderRequest<Member> req)
    {
        return Task.FromResult(req.ApplyTo(presentMembers));
        //return memberService.GetPresent(req, currentOrganizationId);
    }

    private async Task HandleCloseResolution()
    {
        await sessionService.CloseResolutionAsync(currentSessionId);
        navigationManager.NavigateTo("/Resolutions");
    }

    private Task HandleVoteInFavor(Member member) => VoteAsync(member, VoteChoice.InFavor);
    private Task HandleVoteAgainst(Member member) => VoteAsync(member, VoteChoice.Against);
    private Task HandleVoteAbstained(Member member) => VoteAsync(member, VoteChoice.Abstained);

    private async Task VoteAsync(Member member, VoteChoice choice)
    {
        try
        {
            await voteService.MakeVoteAsync(member.Id, resolutionId, choice);
        }
        catch (InvalidOperationException ex)
        {
            messengerService.AddError(ex.Message);
        }
    }
}
