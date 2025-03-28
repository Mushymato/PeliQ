using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;

namespace PeliQ.Framework.GameStateQ;

/// <summary>Check at least X following GSQs return same value as the first GSQ.</summary>
public static class CountTrue
{
    public static string GameStateQuery_COUNT => $"{ModEntry.ModId}_COUNT";

    internal static void Register()
    {
        GameStateQuery.Register(GameStateQuery_COUNT, COUNT);
    }

    private static bool COUNT(string[] query, GameStateQueryContext context)
    {
        if (
            !ArgUtility.TryGet(query, 1, out string firstGSQ, out string error, false, "string firstGSQ")
            || !ArgUtility.TryGetInt(query, 2, out int count, out error, "int count")
        )
        {
            ModEntry.Log(error, LogLevel.Error);
            return false;
        }
        bool result = GameStateQuery.CheckConditions(firstGSQ, context);
        int cnt = 0;
        foreach (string gsq in query.Skip(3))
        {
            if (GameStateQuery.CheckConditions(gsq, context) == result)
            {
                cnt++;
                if (cnt == count)
                    return true;
            }
        }
        return false;
    }
}
