using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;

namespace PeliQ.Framework.GameStateQ;

public static class ItemRegex
{
    public static string GameStateQuery_ITEM_ID_REGEX => $"{ModEntry.ModId}_ITEM_ID_REGEX";

    internal static void Register()
    {
        GameStateQuery.Register(GameStateQuery_ITEM_ID_REGEX, REGEX);
    }

    /// <summary>Do case insensitive regex match on item id/qualified item id</summary>
    public static bool REGEX(string[] query, GameStateQueryContext context)
    {
        if (
            !GameStateQuery.Helpers.TryGetItemArg(
                query,
                1,
                context.TargetItem,
                context.InputItem,
                out var item,
                out var error
            )
        )
        {
            ModEntry.Log(error, LogLevel.Error);
            return false;
        }
        if (item != null)
        {
            return GameStateQuery.Helpers.AnyArgMatches(
                query,
                2,
                patternStr =>
                {
                    Regex pattern = new(patternStr, RegexOptions.IgnoreCase);
                    return pattern.IsMatch(item.ItemId) || pattern.IsMatch(item.QualifiedItemId);
                }
            );
        }
        return false;
    }
}
