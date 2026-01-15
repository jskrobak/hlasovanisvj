using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class AttendanceService(AppDbContext dbContext)
{
    public async Task SavePresenceAsync(Member member)
    {
        dbContext.Update(member);
        
        foreach(var principal in member.Principals)
        {
            principal.IsPresent = member.IsPresent;
            dbContext.Update(principal);
        }
        
        await dbContext.SaveChangesAsync();
        
    }
    
    public async Task<double> GetTotalPresentShareAsync()
    {
        return await Task.FromResult(dbContext.Members
            .Where(m => m.IsPresent)
            .Sum(m => m.ShareValue));
    }
}