using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class MemberService(AppDbContext dbContext)
{
    public GridDataProviderResult<Member> GetGridData(GridDataProviderRequest<Member> req, int organizationId, MemberFilter filterModel)
    {
        var data = dbContext.Members
            .Where(m => m.OrganizationId == organizationId);
            
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
    
    public async Task<List<Member>> GetMembersAsync(int orgId)
    {
        return await dbContext.Members.Where(m => m.OrganizationId == orgId).ToListAsync();
    }
    
    public async Task ClearMembersAsync(int orgId)
    {
        var members = await dbContext.Members.Where(m => m.OrganizationId == orgId)
            .ToListAsync();
        
        dbContext.Members.RemoveRange(members);
        await dbContext.SaveChangesAsync();
    }
    
    public async Task InsertMembers(IEnumerable<Member> members)
    {
        await dbContext.AddRangeAsync(members);
        await dbContext.SaveChangesAsync();
    }
}