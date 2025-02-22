using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Internal;

namespace PeliQ.Framework.ItemQ;

/// <summary>Bogus salable for purpose of running purchase actions without obtaining any actual item</summary>
public static class ActionSalable
{
    public static string ItemQuery_ACTION_SALABLE => $"{ModEntry.ModId}_ACTION_SALABLE";

    internal static void Register()
    {
        ItemQueryResolver.Register(ItemQuery_ACTION_SALABLE, ACTION_SALABLE);
    }

    public static IEnumerable<ItemQueryResult> ACTION_SALABLE(
        string key,
        string arguments,
        ItemQueryContext context,
        bool avoidRepeat,
        HashSet<string> avoidItemIds,
        Action<string, string> logError
    )
    {
        string[] array = ItemQueryResolver.Helpers.SplitArguments(arguments);
        if (!ArgUtility.TryGet(array, 0, out string objectId, out string error, allowBlank: false, "string objectId"))
            return ItemQueryResolver.Helpers.ErrorResult(key, arguments, logError, error);
        return [new ItemQueryResult(new ActionSalableObject(objectId))];
    }
}

public class ActionSalableObject(string itemId) : SObject(itemId, 1)
{
    public static string CustomField_ExitOnPurchase => $"{ModEntry.ModId}/ExitOnPurchase";
    public static string CustomField_PurchaseSound => $"{ModEntry.ModId}/PurchaseSound";

    public override bool actionWhenPurchased(string shopId)
    {
        if (!Game1.objectData.TryGetValue(ItemId, out ObjectData? data) || data.CustomFields == null)
        {
            // default behavior
            Game1.playSound("purchaseClick", null);
            return true;
        }
        // only Data/Objects get to use this path
        var customFields = data.CustomFields;
        if (
            customFields.TryGetValue(CustomField_ExitOnPurchase, out string? exitOnPurchaseStr)
            && bool.TryParse(exitOnPurchaseStr, out bool exitOnPurchase)
            && exitOnPurchase
        )
            Game1.exitActiveMenu();
        if (customFields.TryGetValue(CustomField_PurchaseSound, out string? purchaseSound))
        {
            if (purchaseSound != null)
                Game1.playSound(purchaseSound, null);
        }
        else
            Game1.playSound("purchaseClick", null);

        return true;
    }
}
