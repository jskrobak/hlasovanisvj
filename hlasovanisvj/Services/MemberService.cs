using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class MemberService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task SyncPrincipalsAsync(int memberId, IEnumerable<int> principalIds)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        var member = await dbContext.Members
            .Include(m => m.Principals)
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Účastník nenalezen.");

        await dbContext.EnsureSessionOpenAsync(member.SessionId);

        var ids = principalIds.Where(id => id != memberId).ToList();

        // Plnou moc mohou udělit jen členové SVJ ze stejné schůze.
        var selectedPrincipals = await dbContext.Members
            .Where(m => ids.Contains(m.Id) && m.SessionId == member.SessionId && !m.IsGuest)
            .ToListAsync();
        
        member.Principals.Clear();
        foreach (var principal in selectedPrincipals)
            member.Principals.Add(principal);

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Přidá hosta (nečlena SVJ s nulovým podílem), který může zastupovat členy na základě plné moci.
    /// </summary>
    public async Task<Member> AddGuestAsync(int sessionId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Zadejte jméno hosta.");

        await using var dbContext = await dbFactory.CreateDbContextAsync();
        await dbContext.EnsureSessionOpenAsync(sessionId);

        var guest = new Member
        {
            SessionId = sessionId,
            Name = name.Trim(),
            IsGuest = true,
            ShareValue = 0,
            ShareFraction = "0",
        };

        dbContext.Members.Add(guest);
        await dbContext.SaveChangesAsync();
        return guest;
    }

    public async Task UpdateGuestNameAsync(int memberId, string name)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        var guest = await dbContext.Members.FirstOrDefaultAsync(m => m.Id == memberId && m.IsGuest)
                    ?? throw new InvalidOperationException("Host nenalezen.");
        await dbContext.EnsureSessionOpenAsync(guest.SessionId);

        guest.Name = name.Trim();
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Odebere hosta. Zmocnitelé, které zastupoval, přijdou o zmocněnce; jeho vlastní hlasy se smažou.
    /// </summary>
    public async Task DeleteGuestAsync(int memberId)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        var guest = await dbContext.Members
                        .Include(m => m.Principals)
                        .FirstOrDefaultAsync(m => m.Id == memberId && m.IsGuest)
                    ?? throw new InvalidOperationException("Host nenalezen.");
        await dbContext.EnsureSessionOpenAsync(guest.SessionId);

        foreach (var principal in guest.Principals)
            principal.ProxyId = null;

        dbContext.Members.Remove(guest);
        await dbContext.SaveChangesAsync();
    }
    
    public async Task<GridDataProviderResult<Member>> GetAllAsync(GridDataProviderRequest<Member> req, int sessionId, MemberFilter filterModel)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        
        var data = dbContext.Members
            .AsNoTracking()
            .Include(m => m.Principals)
            .Include(m => m.Proxy)
            .Where(m => m.SessionId == sessionId);
            
        if(!string.IsNullOrWhiteSpace(filterModel.Name))
            data = data.Where(p => EF.Functions.ILike(p.Name, $"%{filterModel.Name}%"));

        var count = await data.CountAsync();
        data = data.ApplyGridDataProviderRequest(req);
		
        return new GridDataProviderResult<Member>()
        {
            Data = await data.ToListAsync(),
            TotalCount = count
        };
    }

    /// <summary>Členové schůze vhodní jako zmocnitelé (bez hostů a bez daného účastníka).</summary>
    public async Task<List<Member>> GetPrincipalCandidatesAsync(int sessionId, int exceptMemberId)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        return await dbContext.Members
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId && m.Id != exceptMemberId && !m.IsGuest)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }
    
    public async Task<List<Member>> GetPresentWithChoiseAsync (int sessionId)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        return await dbContext.Members
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId && m.IsPresent)
            .Include(m => m.Votes)
            .Include(m => m.Proxy)
            .ToListAsync();
    }
    
    public async Task<List<Member>> GetMembersAsync(int sessionId)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        return await dbContext.Members
            .AsNoTracking()
            .Include(m => m.Principals)
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Id)
            .ToListAsync();
    }
    
    public async Task ClearMembersAsync(int sessionId)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        await dbContext.EnsureSessionOpenAsync(sessionId);

        await dbContext.Members.Where(m => m.SessionId == sessionId)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.ProxyId, (int?)null));
        await dbContext.Members.Where(m => m.SessionId == sessionId)
            .ExecuteDeleteAsync();
    }
    
    public async Task InsertMembers(IEnumerable<Member> members)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        await dbContext.AddRangeAsync(members);
        await dbContext.SaveChangesAsync();
    }
}
