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
    SessionService sessionService) : ComponentBase, IAsyncDisposable
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
        await sessionService.SetOpenedResolutionAsync(currentSessionId, null);
        navigationManager.NavigateTo("/Resolutions");
    }

    private async Task HandleVoteInFavor(Member member)
    {
        await voteService.MakeVoteAsync(member.Id, resolutionId, VoteChoice.InFavor);
        //await gridComponent.RefreshDataAsync();
    }

    private async Task HandleVoteAgainst(Member member)
    {
        await voteService.MakeVoteAsync(member.Id, resolutionId, VoteChoice.Against);
        //UpdateMemberVote(member, VoteChoice.Against);
    }

    private async Task HandleVoteAbstained(Member member)
    {
        await voteService.MakeVoteAsync(member.Id, resolutionId, VoteChoice.Abstained);
        //UpdateMemberVote(member, VoteChoice.Abstained);
    }

}