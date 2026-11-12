using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Hubs;
using hlasovanisvj.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class VoteService(IDbContextFactory<AppDbContext> dbFactory,
    IHubContext<VoteHub> hub)
{
    /// <summary>
    /// Živé výsledky hlasování.
    /// *Share = součet podílů (v % celého domu, stejně jako Member.ShareValue),
    /// *SharePercent = podíl v % z přítomných (totalPresentShare).
    /// </summary>
    public async Task<VoteLiveStats> GetLiveStatsAsync(int resolutionId, double totalPresentShare)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();

        var rows = await dbContext.Votes
            .AsNoTracking()
            .Where(v => v.ResolutionId == resolutionId && !v.Member.IsGuest) // host má nulový podíl
            .Select(v => new { v.Choice, v.Member.ShareValue })
            .ToListAsync();

        var totalShareVoted = rows.Sum(r => r.ShareValue);
        var inFavorShare = Share(VoteChoice.InFavor);
        var againstShare = Share(VoteChoice.Against);
        var abstainedShare = Share(VoteChoice.Abstained);

        return new VoteLiveStats(
            resolutionId,
            rows.Count,
            (float)totalShareVoted,
            Percent(totalShareVoted),
            Count(VoteChoice.InFavor),
            (float)inFavorShare,
            Percent(inFavorShare),
            Count(VoteChoice.Against),
            (float)againstShare,
            Percent(againstShare),
            Count(VoteChoice.Abstained),
            (float)abstainedShare,
            Percent(abstainedShare));

        int Count(VoteChoice c) => rows.Count(r => r.Choice == c);

        double Share(VoteChoice c) => rows.Where(r => r.Choice == c).Sum(r => r.ShareValue);

        float Percent(double share) => totalPresentShare > 0 ? (float)(share / totalPresentShare * 100.0) : 0f;
    }

    /// <summary>
    /// Zaznamená hlas člena a stejný hlas i za všechny, kdo mu (i nepřímo) udělili plnou moc.
    /// </summary>
    public async Task MakeVoteAsync(int memberId, int resolutionId, VoteChoice choice)
    {
        await using var ctx = await dbFactory.CreateDbContextAsync();

        // Hlasovat lze jen o usnesení, které je ve schůzi člena právě otevřené (a schůze není uzavřená).
        var isOpen = await ctx.Members
            .AnyAsync(m => m.Id == memberId
                           && m.Session.OpenedResolutionId == resolutionId
                           && m.Session.ClosedAt == null);
        if (!isOpen)
            throw new InvalidOperationException("Hlasování o tomto usnesení není otevřené.");

        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(memberId);

        // BFS přes řetězec plných mocí; visited chrání před cykly (A → B → A).
        while (queue.TryDequeue(out var id))
        {
            if (!visited.Add(id))
                continue;

            var existingVote = await ctx.Votes
                .FirstOrDefaultAsync(v => v.MemberId == id && v.ResolutionId == resolutionId);
            if (existingVote != null)
            {
                existingVote.Choice = choice;
                existingVote.Date = DateTime.Now;
            }
            else
            {
                ctx.Votes.Add(new Domain.Vote
                {
                    MemberId = id,
                    ResolutionId = resolutionId,
                    Choice = choice
                });
            }

            var principalIds = await ctx.Members
                .Where(m => m.ProxyId == id)
                .Select(m => m.Id)
                .ToListAsync();

            foreach (var principalId in principalIds)
                queue.Enqueue(principalId);
        }

        await ctx.SaveChangesAsync();

        // notify dashboard (jednou za celé hlasování včetně zmocnitelů)
        await hub.Clients.Group($"res:{resolutionId}")
            .SendAsync("VoteChanged", resolutionId);
    }
}
