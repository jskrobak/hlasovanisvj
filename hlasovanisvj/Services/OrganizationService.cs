using hlasovanisvj.Data;

namespace hlasovanisvj.Services;

public class OrganizationService(AppDbContext dbContext)
{
    public async Task SetCurrentSessionAsync(int organizationId, int sessionId)
    {
        var organization = await dbContext.Organizations.FindAsync(organizationId);
        if (organization != null)
        {
            organization.CurrentSessionId = sessionId;
            await dbContext.SaveChangesAsync();
        }
    }
}