using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.GameData;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Triggers;

namespace PeliQ.Framework.ItemQ;

/// <summary>
/// A custom asset for item queries which can be called from another item query, plus trigger action
/// </summary>
internal static class StoredQuery
{
    internal static readonly string Asset_ItemQueries = $"{ModEntry.ModId}/ItemQueries";
    internal static readonly string ItemQuery_STORED_QUERY = $"{ModEntry.ModId}_STORED_QUERY";
    internal static readonly string Action_AddItemByQuery = $"{ModEntry.ModId}_AddItemByQuery";
    internal const string MAIL_PELIQ = "%peliQ";

    internal static void Register()
    {
        ModEntry.help.Events.Content.AssetRequested += OnAssetRequested;
        ModEntry.help.Events.Content.AssetsInvalidated += OnAssetInvalidated;
        ItemQueryResolver.Register(ItemQuery_STORED_QUERY, STORED_QUERY);
        TriggerActionManager.RegisterAction(Action_AddItemByQuery, TriggerActionAddItemByQuery);
        GameLocation.RegisterTileAction(Action_AddItemByQuery, TileActionAdditemByQuery);
        GameLocation.RegisterTouchAction(Action_AddItemByQuery, TouchActionAdditemByQuery);
        try
        {
            ModEntry.harm.Patch(
                original: AccessTools.Method(typeof(LetterViewerMenu), nameof(LetterViewerMenu.HandleItemCommand)),
                postfix: new HarmonyMethod(typeof(StoredQuery), nameof(LetterViewerMenu_HandleItemCommand_Postfix))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch StoredQuery:\n{err}", LogLevel.Error);
        }
    }

    private static bool TryGetItemQueryData(
        string[] args,
        int start,
        out string error,
        [NotNullWhen(true)] out List<GenericSpawnItemDataWithCondition>? spawnDataList,
        [NotNullWhen(true)] out ItemQuerySearchMode itemQuerySearchMode
    )
    {
        spawnDataList = null;
        itemQuerySearchMode = ItemQuerySearchMode.All;
        if (
            !ArgUtility.TryGet(
                args,
                start,
                out string storedQueryId,
                out error,
                allowBlank: false,
                "string storedQueryId"
            )
        )
            return false;
        if (!IQData.TryGetValue(storedQueryId, out spawnDataList))
            return false;
        if (
            !ArgUtility.TryGetOptional(
                args,
                start + 1,
                out string itemQuerySearchModeStr,
                out error,
                "string itemQuerySearchMode"
            )
        )
            return false;
        if (!Enum.TryParse(itemQuerySearchModeStr, true, out itemQuerySearchMode))
            itemQuerySearchMode = ItemQuerySearchMode.AllOfTypeItem;
        return true;
    }

    private static IEnumerable<Item> ResolveItemQueryList(
        List<GenericSpawnItemDataWithCondition> spawnDataList,
        ItemQuerySearchMode itemQuerySearchMode,
        ItemQueryContext context
    )
    {
        foreach (GenericSpawnItemDataWithCondition spawnData in spawnDataList)
        {
            if (!GameStateQuery.CheckConditions(spawnData.Condition))
                continue;
            var results = ItemQueryResolver.TryResolve(spawnData, context, filter: itemQuerySearchMode);
            foreach (var res in results)
            {
                if (res.Item is Item item)
                {
                    yield return item;
                }
            }
        }
    }

    private static void LetterViewerMenu_HandleItemCommand_Postfix(LetterViewerMenu __instance, ref string __result)
    {
        string mail = __result;
        ModEntry.Log(mail);
        int startIndex = 0;
        int start;
        int end;
        ReadOnlySpan<char> mailSpan;
        while (true)
        {
            if ((start = mail.IndexOf(MAIL_PELIQ, startIndex, StringComparison.InvariantCulture)) < 0)
                break;
            if ((end = mail.IndexOf("%%", start, StringComparison.InvariantCulture)) < 0)
                break;
            startIndex = start;
            string text = mail[start..(end + 2)];
            mailSpan = mail.AsSpan();
            mail = string.Concat(mailSpan[..start], mailSpan[(start + text.Length)..]);
            string[] args = ArgUtility.SplitBySpace(text);
            if (
                !TryGetItemQueryData(
                    args,
                    1,
                    out string _,
                    out List<GenericSpawnItemDataWithCondition>? spawnDataList,
                    out ItemQuerySearchMode itemQuerySearchMode
                )
            )
                return;
            foreach (
                Item item in ResolveItemQueryList(spawnDataList, itemQuerySearchMode, new(null, null, null, args[0]))
            )
            {
                __instance.itemsToGrab.Add(
                    new ClickableComponent(
                        new Rectangle(
                            __instance.xPositionOnScreen + __instance.width / 2 - 48,
                            __instance.yPositionOnScreen + __instance.height - 32 - 96,
                            96,
                            96
                        ),
                        item
                    )
                    {
                        myID = 104,
                        leftNeighborID = 101,
                        rightNeighborID = 102,
                    }
                );
                __instance.backButton.rightNeighborID = 104;
                __instance.forwardButton.leftNeighborID = 104;
            }
        }
        __result = mail;
    }

    private static void TouchActionAdditemByQuery(GameLocation location, string[] args, Farmer farmer, Vector2 vector)
    {
        TileActionAdditemByQuery(location, args, farmer, vector.ToPoint());
    }

    private static bool TileActionAdditemByQuery(GameLocation location, string[] args, Farmer farmer, Point point)
    {
        if (
            !TryGetItemQueryData(
                args,
                1,
                out string error,
                out List<GenericSpawnItemDataWithCondition>? spawnDataList,
                out ItemQuerySearchMode itemQuerySearchMode
            )
        )
            return false;
        if (
            !ArgUtility.TryGetOptionalBool(
                args,
                3,
                out bool asDebris,
                out error,
                defaultValue: true,
                name: "bool asDebris"
            )
        )
            return false;

        var items = ResolveItemQueryList(spawnDataList, itemQuerySearchMode, new(location, farmer, null, args[0]));
        if (!items.Any())
            return false;
        if (asDebris)
        {
            foreach (Item item in items)
            {
                Game1.createItemDebris(
                    item,
                    new Vector2(
                        point.X * Game1.tileSize + Game1.tileSize / 2,
                        point.Y * Game1.tileSize + Game1.tileSize / 2
                    ),
                    -1
                );
            }
        }
        else
        {
            farmer.addItemsByMenuIfNecessary(items.ToList());
        }
        return true;
    }

    private static bool TriggerActionAddItemByQuery(string[] args, TriggerActionContext context, out string error)
    {
        if (
            !TryGetItemQueryData(
                args,
                1,
                out error,
                out List<GenericSpawnItemDataWithCondition>? spawnDataList,
                out ItemQuerySearchMode itemQuerySearchMode
            )
        )
            return false;
        var items = ResolveItemQueryList(spawnDataList, itemQuerySearchMode, new(null, null, null, args[0]));
        if (items.Any())
        {
            Game1.player.addItemsByMenuIfNecessary(items.ToList());
            return true;
        }
        return false;
    }

    public static IEnumerable<ItemQueryResult> STORED_QUERY(
        string key,
        string arguments,
        ItemQueryContext context,
        bool avoidRepeat,
        HashSet<string> avoidItemIds,
        Action<string, string> logError
    )
    {
        ModEntry.Log($"{key} {arguments}");
        string[] args = ItemQueryResolver.Helpers.SplitArguments(arguments);
        if (
            !TryGetItemQueryData(
                args,
                0,
                out string error,
                out List<GenericSpawnItemDataWithCondition>? spawnDataList,
                out ItemQuerySearchMode itemQuerySearchMode
            )
        )
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, error);
            yield break;
        }
        foreach (var spawnData in spawnDataList)
        {
            foreach (
                var res in ItemQueryResolver.TryResolve(
                    spawnData,
                    new ItemQueryContext(context, $"{context.SourcePhrase} {key}"),
                    filter: itemQuerySearchMode,
                    avoidRepeat: avoidRepeat,
                    avoidItemIds: avoidItemIds,
                    logError: logError
                )
            )
            {
                yield return res;
            }
        }
    }

    private static Dictionary<string, List<GenericSpawnItemDataWithCondition>>? _iqData = null;

    /// <summary>Stored item query data</summary>
    public static Dictionary<string, List<GenericSpawnItemDataWithCondition>> IQData
    {
        get
        {
            _iqData ??= Game1.content.Load<Dictionary<string, List<GenericSpawnItemDataWithCondition>>>(
                Asset_ItemQueries
            );
            return _iqData;
        }
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(Asset_ItemQueries))
            e.LoadFrom(() => new Dictionary<string, List<GenericSpawnItemDataWithCondition>>(), AssetLoadPriority.Low);
    }

    private static void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(an => an.IsEquivalentTo(Asset_ItemQueries)))
            _iqData = null;
    }
}
