using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Internal;
using StardewValley.Menus;
using StardewValley.Triggers;

namespace PeliQ.Framework.ItemQ;

/// <summary>
/// A custom asset for item queries which can be called from another item query, plus trigger action
/// </summary>
internal static class StoredQuery
{
    internal const string Asset_ItemQueries = $"{ModEntry.ModId}/ItemQueries";
    internal const string ItemQuery_STORED_QUERY = $"{ModEntry.ModId}_STORED_QUERY";
    internal const string Action_AddItemByQuery = $"{ModEntry.ModId}_AddItemByQuery";
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
        [NotNullWhen(true)] out List<PeliQSpawnItemData>? spawnDataList
    )
    {
        spawnDataList = null;
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
        {
            error = $"No query with ID '{storedQueryId}' defined in '{Asset_ItemQueries}'";
            return false;
        }
        if (
            !ArgUtility.TryGetOptional(
                args,
                start + 1,
                out string itemQuerySearchModeStr,
                out error,
                "string itemQuerySearchMode"
            )
            || !ArgUtility.TryGetOptional(
                args,
                start + 2,
                out string? inputItemId,
                out error,
                defaultValue: null,
                name: "string inputItemId"
            )
        )
            return false;
        if (Enum.TryParse(itemQuerySearchModeStr, true, out ItemQuerySearchMode searchMode))
        {
            foreach (PeliQSpawnItemData spawnData in spawnDataList)
            {
                spawnData.SearchMode = searchMode;
            }
        }
        if (inputItemId != null)
        {
            foreach (PeliQSpawnItemData spawnData in spawnDataList)
            {
                spawnData.InputId = inputItemId;
            }
        }
        return true;
    }

    internal static IList<Item> ResolveItemQueryList(List<PeliQSpawnItemData> spawnDataList, ItemQueryContext context)
    {
        IList<Item> items = [];
        foreach (PeliQSpawnItemData spawnData in spawnDataList)
        {
            if (!GameStateQuery.CheckConditions(spawnData.Condition))
                continue;
            IList<ItemQueryResult> results = spawnData.TryPeliQResolve(context: context);
            foreach (ItemQueryResult res in results)
            {
                if (res.Item is Item item)
                {
                    items.Add(item);
                }
            }
        }
        return items;
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
            if (!TryGetItemQueryData(args, 1, out string _, out List<PeliQSpawnItemData>? spawnDataList))
                return;
            foreach (Item item in ResolveItemQueryList(spawnDataList, new(null, null, null, args[0])))
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
        if (!TryGetItemQueryData(args, 1, out string error, out List<PeliQSpawnItemData>? spawnDataList))
            return false;
        if (
            !ArgUtility.TryGetOptionalBool(
                args,
                4,
                out bool asDebris,
                out error,
                defaultValue: true,
                name: "bool asDebris"
            )
        )
            return false;

        var items = ResolveItemQueryList(spawnDataList, new(location, farmer, null, args[0]));
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
        if (!TryGetItemQueryData(args, 1, out error, out List<PeliQSpawnItemData>? spawnDataList))
            return false;
        var items = ResolveItemQueryList(spawnDataList, new(null, null, null, args[0]));
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
        if (!TryGetItemQueryData(args, 0, out string error, out List<PeliQSpawnItemData>? spawnDataList))
        {
            ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, error);
            yield break;
        }
        foreach (PeliQSpawnItemData spawnData in spawnDataList)
        {
            foreach (
                ItemQueryResult res in spawnData.TryPeliQResolve(
                    new ItemQueryContext(context, $"{context.SourcePhrase} {key}"),
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

    private static Dictionary<string, List<PeliQSpawnItemData>>? _iqData = null;

    /// <summary>Stored item query data</summary>
    public static Dictionary<string, List<PeliQSpawnItemData>> IQData
    {
        get
        {
            _iqData ??= Game1.content.Load<Dictionary<string, List<PeliQSpawnItemData>>>(Asset_ItemQueries);
            return _iqData;
        }
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo(Asset_ItemQueries))
            e.LoadFrom(() => new Dictionary<string, List<PeliQSpawnItemData>>(), AssetLoadPriority.Low);
    }

    private static void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.NamesWithoutLocale.Any(an => an.IsEquivalentTo(Asset_ItemQueries)))
            _iqData = null;
    }
}
