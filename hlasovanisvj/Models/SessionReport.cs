using hlasovanisvj.Domain;

namespace hlasovanisvj.Models;

/// <summary>
/// Data pro detail / report schůze: prezenční listina a usnesení s výsledky.
/// </summary>
public sealed class SessionReport
{
    public required Session Session { get; init; }
    public required string OrganizationName { get; init; }

    /// <summary>Všichni účastníci schůze (členové i hosté) seřazení podle čísla.</summary>
    public required IReadOnlyList<Member> Members { get; init; }

    public required IReadOnlyList<Resolution> Resolutions { get; init; }

    /// <summary>Hlasy podle (usnesení, účastník).</summary>
    public required IReadOnlyDictionary<(int ResolutionId, int MemberId), VoteChoice> Votes { get; init; }

    public IEnumerable<Member> Owners => Members.Where(m => !m.IsGuest);
    public IEnumerable<Member> Guests => Members.Where(m => m.IsGuest);

    public double TotalShare => Owners.Sum(m => m.ShareValue);
    public double PresentShare => Owners.Where(m => m.IsPresent).Sum(m => m.ShareValue);
    public int PresentOwnersCount => Owners.Count(m => m.IsPresent);

    /// <summary>Usnášeníschopnost: přítomni vlastníci s nadpoloviční většinou podílů (§ 1206 NOZ).</summary>
    public bool IsQuorate => TotalShare > 0 && PresentShare / TotalShare > 0.5;

    public VoteChoice? GetVote(Resolution resolution, Member member) =>
        Votes.TryGetValue((resolution.Id, member.Id), out var choice) ? choice : null;

    /// <summary>Text účasti pro prezenční listinu.</summary>
    public static string AttendanceText(Member member) => member switch
    {
        { IsPresent: false } => "nepřítomen",
        { Proxy: not null } => $"v zastoupení: {member.Proxy.Name}",
        { IsGuest: true } => "přítomen (host)",
        _ => "přítomen"
    };

    /// <summary>
    /// Stav usnesení. "Přijato" = nadpoloviční většina hlasů přítomných (výchozí pravidlo § 1206 NOZ;
    /// stanovy nebo zákon mohou pro některá rozhodnutí vyžadovat jinou většinu).
    /// </summary>
    public static string ResultText(Resolution resolution, bool isOpened) => resolution switch
    {
        _ when isOpened => "hlasování probíhá",
        { HasResult: false } => "nehlasováno",
        _ when resolution.PercentOfPresent(resolution.InFavorShare) > 50 => "přijato",
        _ => "nepřijato"
    };

    public static string ChoiceText(VoteChoice? choice) => choice switch
    {
        VoteChoice.InFavor => "pro",
        VoteChoice.Against => "proti",
        VoteChoice.Abstained => "zdržel se",
        _ => "nehlasoval"
    };
}
