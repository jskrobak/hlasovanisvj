using hlasovanisvj.Data;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class AttendanceService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task SavePresenceAsync(int memberId, bool isPresent)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        
        var member = await dbContext.Members
            .Include(m => m.Principals)
            .FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Účastník nenalezen.");

        await dbContext.EnsureSessionOpenAsync(member.SessionId);
        
        member.IsPresent = isPresent;
        
        foreach(var principal in member.Principals)
        {
            principal.IsPresent = member.IsPresent;
        }
        
        await dbContext.SaveChangesAsync();
    }
    
    public async Task<double> GetTotalPresentShareAsync(int sessionId)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        return await dbContext.Members
            .Where(m => m.SessionId == sessionId && m.IsPresent)
            .SumAsync(m => m.ShareValue);
    }
}
