using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class MemberService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task SyncPrincipalsAsync(int memberId, IEnumerable<int> principalIds)
    {
        var dbContext = await dbFactory.CreateDbContextAsync();
        var member = await dbContext.Members
            .Include(m => m.Principals)
            .FirstOrDefaultAsync(m => m.Id == memberId);
        
        member.Principals.Clear();
        
        await dbContext.SaveChangesAsync();
        
        var selectedPrincipals = await dbContext.Members
            .Where(m => principalIds.Contains(m.Id))
            .ToListAsync();
        
        member.Principals = selectedPrincipals;
        //dbContext.Update(member);
        await dbContext.SaveChangesAsync();
    }
    
    
    public GridDataProviderResult<Member> GetAll(GridDataProviderRequest<Member> req, int sessionId, MemberFilter filterModel)
    {
        var dbContext = dbFactory.CreateDbContext();
        
        var data = dbContext.Members
            .Include(m => m.Principals)
            .Include(m => m.Proxy)
            .Where(m => m.SessionId == sessionId);
            
        if(!string.IsNullOrWhiteSpace(filterModel.Name))
            data = data.Where(p => p.Name.Contains(filterModel.Name));

        var count = data.Count();
        data = data.ApplyGridDataProviderRequest(req);
		
        return new GridDataProviderResult<Member>()
        {
            Data = data,
            TotalCount = count
        };
    }
    
    public async Task<List<Member>> GetPresentWithChoiseAsync (int sessionId)
    {
        var dbContext = await dbFactory.CreateDbContextAsync();
        return await dbContext.Members
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId && m.IsPresent)
            .Include(m => m.Votes)
            .Include(m => m.Proxy)
            .ToListAsync();
    }
    
    public async Task<List<Member>> GetMembersAsync(int sessionId)
    {
        var dbContext = await dbFactory.CreateDbContextAsync();
        return await dbContext.Members.Where(m => m.SessionId == sessionId).ToListAsync();
    }
    
    public async Task ClearMembersAsync(int sessionId)
    {
        var dbContext = await dbFactory.CreateDbContextAsync();
        var members = await dbContext.Members.Where(m => m.SessionId == sessionId)
            .ToListAsync();
        
        dbContext.Members.RemoveRange(members);
        await dbContext.SaveChangesAsync();
    }
    
    public async Task InsertMembers(IEnumerable<Member> members)
    {
        var dbContext = await dbFactory.CreateDbContextAsync();
        await dbContext.AddRangeAsync(members);
        await dbContext.SaveChangesAsync();
    }
}