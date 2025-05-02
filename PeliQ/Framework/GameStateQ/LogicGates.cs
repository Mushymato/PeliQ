using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;

namespace PeliQ.Framework.GameStateQ;

public static class LogicGates
{
    public static string GameStateQuery_ANY_N => $"{ModEntry.ModId}_ANY_N";
    public static string GameStateQuery_AND => $"{ModEntry.ModId}_AND";
    public static string GameStateQuery_OR => $"{ModEntry.ModId}_OR";
    public static string GameStateQuery_XOR => $"{ModEntry.ModId}_XOR";

    internal static void Register()
    {
        GameStateQuery.Register(GameStateQuery_ANY_N, ANY_N);
        GameStateQuery.Register(GameStateQuery_XOR, XOR);
        GameStateQuery.Register(GameStateQuery_AND, AND);
        GameStateQuery.Register(GameStateQuery_OR, OR);
    }

    /// <summary>Check at least X following GSQs return same value as the first GSQ.</summary>
    private static bool ANY_N(string[] query, GameStateQueryContext context)
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

    /// <summary>Takes 2 GSQ and evaluate, then call given callback on results.</summary>
    private static bool EvaluateTwoGSQWithCallback(
        string[] query,
        GameStateQueryContext context,
        int firstIdx,
        Func<bool, bool, bool> callback
    )
    {
        if (
            !ArgUtility.TryGet(query, firstIdx, out string firstGSQ, out string error, false, "string firstGSQ")
            || !ArgUtility.TryGet(query, firstIdx + 1, out string secondGSQ, out error, false, "string secondGSQ")
        )
        {
            ModEntry.Log(error, LogLevel.Error);
            return false;
        }
        return callback(
            GameStateQuery.CheckConditions(firstGSQ, context),
            GameStateQuery.CheckConditions(secondGSQ, context)
        );
    }

    /// <summary>Takes 2 GSQ, check AND.</summary>
    private static bool AND(string[] query, GameStateQueryContext context)
    {
        return EvaluateTwoGSQWithCallback(query, context, 1, (firstRes, secondRes) => firstRes && secondRes);
    }

    /// <summary>Takes 2 GSQ, check OR.</summary>
    private static bool OR(string[] query, GameStateQueryContext context)
    {
        return EvaluateTwoGSQWithCallback(query, context, 1, (firstRes, secondRes) => firstRes || secondRes);
    }

    /// <summary>Takes 2 GSQ, check XOR.</summary>
    private static bool XOR(string[] query, GameStateQueryContext context)
    {
        return EvaluateTwoGSQWithCallback(query, context, 1, (firstRes, secondRes) => firstRes != secondRes);
    }
}
