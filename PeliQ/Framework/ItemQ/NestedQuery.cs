using HarmonyLib;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using StardewValley.Internal;

namespace PeliQ.Framework.ItemQ;

/// <summary>Allow item queries to be nested via ModData</summary>
internal static class NestedQuery
{
    private static string ItemQuery_NestedSourcePhrase => $"{ModEntry.ModId}_NestedResolve";
    private static string ItemQuery_NestedIdToken => $"{ModEntry.ModId}_NestedId";

    internal static void Register()
    {
        try
        {
            ModEntry.harm.Patch(
                original: AccessTools.Method(
                    typeof(ItemQueryResolver),
                    nameof(ItemQueryResolver.TryResolve),
                    [
                        typeof(ISpawnItemData),
                        typeof(ItemQueryContext),
                        typeof(ItemQuerySearchMode),
                        typeof(bool),
                        typeof(HashSet<string>),
                        typeof(Func<string, string>),
                        typeof(Action<string, string>),
                        typeof(Item),
                    ]
                ),
                postfix: new HarmonyMethod(typeof(NestedQuery), nameof(ItemQueryResolver_TryResolve_Postfix))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch NestedItemQuery:\n{err}", LogLevel.Error);
        }
    }

    private static void ItemQueryResolver_TryResolve_Postfix(
        ISpawnItemData data,
        ItemQueryContext context,
        ref IList<ItemQueryResult> __result
    )
    {
        if (data.ModData != null)
        {
            PeliQSpawnItemData? nestedSpawn = null;
            try
            {
                nestedSpawn = JsonConvert.DeserializeObject<PeliQSpawnItemData>(
                    JsonConvert.SerializeObject(data.ModData)
                );
            }
            catch (Exception ex)
            {
                ModEntry.Log($"Failed to convert nested query:\n {ex}");
                return;
            }
            if (nestedSpawn == null)
                return;
            List<PeliQSpawnItemData> spawnItemDatas = [nestedSpawn];
            List<ItemQueryResult> nestedResults = [];
            foreach (ItemQueryResult res in __result)
            {
                if (!GameStateQuery.CheckConditions(nestedSpawn.Condition))
                    continue;
                if (res.Item != null)
                {
                    nestedSpawn.InputId = res.Item.QualifiedItemId;
                    nestedResults.AddRange(
                        ItemQueryResolver.TryResolve(
                            nestedSpawn,
                            new ItemQueryContext(context, ItemQuery_NestedSourcePhrase),
                            formatItemId: (token) => token.Replace(ItemQuery_NestedIdToken, res.Item.QualifiedItemId)
                        )
                    );
                }
            }
            __result = nestedResults;
        }
    }
}
