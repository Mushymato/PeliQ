using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.Objects;

namespace PeliQ.Framework;

/// <summary>The data for an item to create with support for a game state query, used in data assets like <see cref="T:StardewValley.GameData.Machines.MachineData" /> or <see cref="T:StardewValley.GameData.Shops.ShopData" />.</summary>
public class PeliQSpawnItemData : MachineItemOutput
{
    public string? InputId { get; set; } = null;
    public ItemQuerySearchMode SearchMode { get; set; } = ItemQuerySearchMode.AllOfTypeItem;

    internal string? ArgInputId { get; set; } = null;
    internal ItemQuerySearchMode? ArgSearchMode { get; set; } = null;

    public IList<ItemQueryResult> TryPeliQResolve(
        ItemQueryContext context,
        bool avoidRepeat = false,
        HashSet<string>? avoidItemIds = null,
        Func<string, string>? formatItemId = null,
        Action<string, string>? logError = null,
        Item? inputItem = null
    )
    {
        IList<ItemQueryResult> results = ItemQueryResolver.TryResolve(
            this,
            context,
            filter: ArgSearchMode ?? SearchMode,
            avoidRepeat,
            avoidItemIds,
            formatItemId,
            logError,
            inputItem
        );
        foreach (ItemQueryResult res in results)
        {
            if (res.Item is Item item)
            {
                ApplyMachineItemFields(ref item, this, context);
                res.Item = item;
            }
        }

        ArgSearchMode = null;
        ArgInputId = null;

        return results;
    }

    private static void ApplyMachineItemFields(
        ref Item spawnedItem,
        PeliQSpawnItemData spawnData,
        ItemQueryContext context
    )
    {
        Item? preserveItem = null;
        string? inputId = spawnData.ArgInputId ?? spawnData.InputId;
        if (!string.IsNullOrEmpty(inputId))
            preserveItem = ItemRegistry.Create(inputId);
        if (preserveItem == null && !string.IsNullOrEmpty(spawnData.PreserveId))
            preserveItem = ItemRegistry.Create(spawnData.PreserveId);
        if (preserveItem == null)
            return;

        if (spawnData.CopyColor)
        {
            Color? color =
                (preserveItem is ColoredObject obj)
                    ? new Color?(obj.color.Value)
                    : ItemContextTagManager.GetColorFromTags(preserveItem);
            if (color.HasValue && ColoredObject.TrySetColor(spawnedItem, color.Value, out var coloredItem))
            {
                spawnedItem = coloredItem;
            }
        }
        if (spawnData.CopyQuality && preserveItem != null)
        {
            spawnedItem.Quality = preserveItem.Quality;
            List<QuantityModifier> qualityModifiers = spawnData.QualityModifiers;
            if (qualityModifiers != null && qualityModifiers.Count > 0)
            {
                spawnedItem.Quality = (int)
                    Utility.ApplyQuantityModifiers(
                        spawnedItem.Quality,
                        spawnData.QualityModifiers,
                        spawnData.QualityModifierMode,
                        context.Location,
                        context.Player,
                        spawnedItem,
                        preserveItem
                    );
            }
        }
        if (spawnedItem is SObject spawnedObject)
        {
            if (spawnData.ObjectInternalName != null)
            {
                spawnedObject.Name = string.Format(spawnData.ObjectInternalName, preserveItem?.Name ?? "");
            }
            if (spawnData.CopyPrice && preserveItem is SObject preserveObject1)
            {
                spawnedObject.Price = preserveObject1.Price;
            }
            List<QuantityModifier> priceModifiers = spawnData.PriceModifiers;
            if (priceModifiers != null && priceModifiers.Count > 0)
            {
                spawnedObject.Price = (int)
                    Utility.ApplyQuantityModifiers(
                        spawnedObject.Price,
                        spawnData.PriceModifiers,
                        spawnData.PriceModifierMode,
                        context.Location,
                        context.Player,
                        spawnedItem,
                        preserveItem
                    );
            }
            if (!string.IsNullOrWhiteSpace(spawnData.PreserveType))
            {
                spawnedObject.preserve.Value = (SObject.PreserveType)
                    Enum.Parse(typeof(SObject.PreserveType), spawnData.PreserveType);
            }
            if (!string.IsNullOrWhiteSpace(spawnData.PreserveId))
            {
                string preserveId = spawnData.PreserveId;
                if (!(preserveId == "DROP_IN"))
                {
                    if (preserveId == "DROP_IN_PRESERVE" && preserveItem is SObject preserveObject2)
                    {
                        spawnedObject.preservedParentSheetIndex.Value = preserveObject2?.GetPreservedItemId();
                    }
                    else
                    {
                        spawnedObject.preservedParentSheetIndex.Value = spawnData.PreserveId;
                    }
                }
                else
                {
                    spawnedObject.preservedParentSheetIndex.Value = preserveItem?.ItemId;
                }
            }
        }
    }
}
