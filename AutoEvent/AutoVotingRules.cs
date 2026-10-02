using System.Collections.Generic;
using System.Linq;

namespace AutoEvent;

internal static class AutoVotingRules
{
    internal static bool ShouldVote(int roundsStarted, int interval) => interval > 0 && (roundsStarted + 1) % interval == 0;

    internal static bool RecordVote(IDictionary<string, bool> votes, string playerId, bool yes)
    {
        if (votes.TryGetValue(playerId, out bool previous) && previous == yes) return false;
        votes[playerId] = yes;
        return true;
    }

    internal static bool Passed(IDictionary<string, bool> votes) => votes.Values.Count(v => v) > votes.Values.Count(v => !v);
}
