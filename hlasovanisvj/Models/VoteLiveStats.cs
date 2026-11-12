namespace hlasovanisvj.Models;

public sealed record VoteLiveStats(
    int ResolutionId,
    
    int TotalVotes,
    float TotalShareVoted,
    float TotalShareVotedPercent,
    
    int InFavorCount,
    float InFavorShare,
    float InFavorSharePercent,
    
    int AgainstCount,
    float AgainstShare,
    float AgainstSharePercent,
    
    int AbstainedCount,
    float AbstainedShare,
    float AbstainedSharePercent
);