using hlasovanisvj.Data;
using hlasovanisvj.Models;
using Microsoft.EntityFrameworkCore;

namespace hlasovanisvj.Services;

public class SessionReportService(IDbContextFactory<AppDbContext> dbFactory)
{
    /// <summary>
    /// Načte report schůze. Vrací null, pokud schůze neexistuje nebo nepatří organizaci.
    /// </summary>
    public async Task<SessionReport?> GetReportAsync(int sessionId, int organizationId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var session = await db.Sessions
            .AsNoTracking()
            .Include(s => s.Organization)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.OrganizationId == organizationId);
        if (session == null)
            return null;

        var members = await db.Members
            .AsNoTracking()
            .Include(m => m.Proxy)
            .Include(m => m.Principals)
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.Id)
            .ToListAsync();

        var resolutions = await db.Resolutions
            .AsNoTracking()
            .Where(r => r.SessionId == sessionId)
            .OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            .ToListAsync();

        var votes = await db.Votes
            .AsNoTracking()
            .Where(v => v.Resolution.SessionId == sessionId)
            .Select(v => new { v.ResolutionId, v.MemberId, v.Choice })
            .ToListAsync();

        return new SessionReport
        {
            Session = session,
            OrganizationName = session.Organization.Name,
            Members = members,
            Resolutions = resolutions,
            Votes = votes.ToDictionary(v => (v.ResolutionId, v.MemberId), v => v.Choice)
        };
    }

    /// <summary>Organizace přihlášeného uživatele podle e-mailu z cookie.</summary>
    public async Task<int?> GetOrganizationIdAsync(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Users
            .Where(u => u.Email == email)
            .Select(u => (int?)u.Organization.Id)
            .FirstOrDefaultAsync();
    }
}
