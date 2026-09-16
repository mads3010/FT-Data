namespace FolketingetVotes.Core.Enums;

/// <summary>Afstemningstype: what kind of decision a vote settles.</summary>
public enum VoteType
{
    FinalPassage = 1,
    CommitteeRecommendation = 2,
    MotionForResolution = 3,
    Amendment = 4,
}
