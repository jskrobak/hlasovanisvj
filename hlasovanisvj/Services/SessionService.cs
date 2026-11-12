using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class SessionService(AppDbContext dbContext)
{
    public async Task<GridDataProviderResult<Session>> GetGridData(GridDataProviderRequest<Session> req, int orgenizationId, SessionFilter filterModel)
    {
        var data = dbContext.Sessions.AsQueryable()
            .Where(r => r.OrganizationId == orgenizationId);
        
        if(!string.IsNullOrWhiteSpace(filterModel.Name))
            data = data.Where(p => p.Name.Contains(filterModel.Name));

        var count = data.Count();
        data = data.ApplyGridDataProviderRequest(req);
		
        return new GridDataProviderResult<Session>
        {
            Data = data,
            TotalCount = count
        };
    }
    
    public async Task<Session> GetByIdAsync(int sessionId)
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

    /// <summary>
    /// Otevře usnesení ve schůzi (resolutionId = null hlasování uzavře).
    /// </summary>
    public async Task SetOpenedResolutionAsync(int sessionId, int? resolutionId)
    {
        var session = await dbContext.Sessions.FindAsync(sessionId)
                      ?? throw new InvalidOperationException("Schůze nenalezena.");

        if (resolutionId != null &&
            !await dbContext.Resolutions.AnyAsync(r => r.Id == resolutionId && r.SessionId == sessionId))
            throw new InvalidOperationException("Usnesení nepatří do této schůze.");

        session.OpenedResolutionId = resolutionId;
        await dbContext.SaveChangesAsync();
    }
    
    public async Task SaveSessionAsync(Session session)
    {
        if (session.Id == 0)
        {
            await dbContext.AddAsync(session);
        }
        else
        {
             dbContext.Update(session);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteSessionAsync(int sessionId)
    {
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(o => o.CurrentSessionId == sessionId);
        if(organization != null)
        {
            organization.CurrentSessionId = null;
            dbContext.Update(organization);
        }
        
        foreach(var resolution in dbContext.Resolutions.Where(r => r.SessionId == sessionId))
        {
            dbContext.RemoveRange(dbContext.Votes.Where(v => v.ResolutionId == resolution.Id));
        }
        
        dbContext.RemoveRange(dbContext.Resolutions.Where(v => v.SessionId == sessionId));
        dbContext.RemoveRange(dbContext.Members.Where(r => r.SessionId == sessionId));
        dbContext.RemoveRange(dbContext.Sessions.Where(s => s.Id == sessionId));
        
        await dbContext.SaveChangesAsync();
    }
}