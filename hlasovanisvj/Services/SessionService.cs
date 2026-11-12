using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class SessionService(AppDbContext dbContext, IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<GridDataProviderResult<Session>> GetGridData(GridDataProviderRequest<Session> req, int organizationId, SessionFilter filterModel)
    {
        // Vlastní kontext: virtualizovaný grid může volat souběžně se zbytkem stránky.
        await using var gridContext = await dbFactory.CreateDbContextAsync();
        var data = gridContext.Sessions.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId);

        if(!string.IsNullOrWhiteSpace(filterModel.Name))
            data = data.Where(p => EF.Functions.ILike(p.Name, $"%{filterModel.Name}%"));

        var count = await data.CountAsync();
        data = data.ApplyGridDataProviderRequest(req);

        return new GridDataProviderResult<Session>
        {
            Data = await data.ToListAsync(),
            TotalCount = count
        };
    }

    public async Task<Session?> GetByIdAsync(int sessionId)
    {
        return await dbContext.Sessions.FindAsync(sessionId);
    }

    public async Task<Session?> GetCurrentSessionAsync(int organizationId)
    {
        return await dbContext.Organizations
            .Where(o => o.Id == organizationId)
            .Select(o => o.CurrentSession)
            .FirstOrDefaultAsync();
    }

    public async Task SaveSessionAsync(Session session)
    {
        if (session.Id == 0)
        {
            await dbContext.AddAsync(session);
        }
        else
        {
            // Grid předává netrackovanou kopii; měníme jen editovatelná pole.
            var existing = await dbContext.Sessions.FindAsync(session.Id)
                           ?? throw new InvalidOperationException("Schůze nenalezena.");
            existing.Name = session.Name;
            existing.Date = session.Date;
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Otevře hlasování o usnesení. Případné jiné otevřené hlasování ve schůzi se nejdřív uzavře.
    /// </summary>
    public async Task OpenResolutionAsync(int sessionId, int resolutionId)
    {
        await dbContext.EnsureSessionOpenAsync(sessionId);

        var session = await dbContext.Sessions.FindAsync(sessionId)
                      ?? throw new InvalidOperationException("Schůze nenalezena.");

        var resolution = await dbContext.Resolutions
                             .FirstOrDefaultAsync(r => r.Id == resolutionId && r.SessionId == sessionId)
                         ?? throw new InvalidOperationException("Usnesení nepatří do této schůze.");

        if (session.OpenedResolutionId != null && session.OpenedResolutionId != resolutionId)
            await SnapshotResultAsync(session.OpenedResolutionId.Value);

        // Znovuotevřené hlasování: výsledek se přepočítá při dalším uzavření.
        resolution.ClosedAt = null;
        session.OpenedResolutionId = resolutionId;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Uzavře právě otevřené hlasování ve schůzi a zafixuje jeho výsledek.
    /// </summary>
    public async Task CloseResolutionAsync(int sessionId)
    {
        var session = await dbContext.Sessions.FindAsync(sessionId)
                      ?? throw new InvalidOperationException("Schůze nenalezena.");

        if (session.OpenedResolutionId == null)
            return;

        await SnapshotResultAsync(session.OpenedResolutionId.Value);
        session.OpenedResolutionId = null;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Uzavře schůzi – od té doby je jen pro čtení.
    /// </summary>
    public async Task CloseSessionAsync(int sessionId)
    {
        await CloseResolutionAsync(sessionId);

        var session = await dbContext.Sessions.FindAsync(sessionId)
                      ?? throw new InvalidOperationException("Schůze nenalezena.");
        session.ClosedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Uloží výsledek hlasování (podíly přítomných a jednotlivých voleb) do usnesení.
    /// Neukládá – volající volá SaveChangesAsync.
    /// </summary>
    private async Task SnapshotResultAsync(int resolutionId)
    {
        var resolution = await dbContext.Resolutions.FindAsync(resolutionId)
                         ?? throw new InvalidOperationException("Usnesení nenalezeno.");

        var votes = await dbContext.Votes
            .Where(v => v.ResolutionId == resolutionId)
            .GroupBy(v => v.Choice)
            .Select(g => new { Choice = g.Key, Share = g.Sum(v => v.Member.ShareValue) })
            .ToListAsync();

        double Share(VoteChoice c) => votes.FirstOrDefault(v => v.Choice == c)?.Share ?? 0;

        resolution.PresentShare = await dbContext.Members
            .Where(m => m.SessionId == resolution.SessionId && m.IsPresent)
            .SumAsync(m => m.ShareValue);
        resolution.InFavorShare = Share(VoteChoice.InFavor);
        resolution.AgainstShare = Share(VoteChoice.Against);
        resolution.AbstainedShare = Share(VoteChoice.Abstained);
        resolution.ClosedAt = DateTime.UtcNow;
    }

    public async Task DeleteSessionAsync(int sessionId)
    {
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(o => o.CurrentSessionId == sessionId);
        if(organization != null)
        {
            organization.CurrentSessionId = null;
        }

        var session = await dbContext.Sessions.FindAsync(sessionId);
        if (session != null)
            session.OpenedResolutionId = null;

        await dbContext.SaveChangesAsync();

        await dbContext.Votes.Where(v => v.Resolution.SessionId == sessionId).ExecuteDeleteAsync();
        await dbContext.Members.Where(m => m.SessionId == sessionId).ExecuteUpdateAsync(s => s.SetProperty(m => m.ProxyId, (int?)null));
        await dbContext.Resolutions.Where(r => r.SessionId == sessionId).ExecuteDeleteAsync();
        await dbContext.Members.Where(m => m.SessionId == sessionId).ExecuteDeleteAsync();
        await dbContext.Sessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync();

        // ExecuteDelete obchází change tracker – zahodit zastaralé entity.
        dbContext.ChangeTracker.Clear();
    }
}
