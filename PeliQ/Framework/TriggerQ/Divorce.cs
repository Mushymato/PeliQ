using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Triggers;

namespace PeliQ.Framework.TriggerQ;

/// <summary>
/// This is supposed to be in some other framework but alas
/// </summary>
public static class Divorce
{
    public static string Actions_Divorce => $"{ModEntry.ModId}_Divorce";

    private static readonly string[] FL_ModIds = ["aedenthorn.FreeLove", "ApryllForever.PolyamorySweetLove"];

    /// <summary>ApryllForever.PolyamorySweetLove compat</summary>
    private static IMod? FL_mod = null;
    private static FieldInfo? FL_spouseToDivorce = null;
    private static FieldInfo? FL_divorceheartslost = null;
    private static MethodInfo? FL_GetSpouses = null;

    internal static void Register()
    {
        foreach (string modId in FL_ModIds)
        {
            try
            {
                var modInfo = ModEntry.help.ModRegistry.Get(modId);
                if (modInfo?.GetType().GetProperty("Mod")?.GetValue(modInfo) is IMod mod)
                {
                    FL_mod = mod;
                    Type FL_modType = mod.GetType();
                    FL_spouseToDivorce = AccessTools.Field(FL_modType, "spouseToDivorce");
                    FL_divorceheartslost = AccessTools.Field(FL_modType, "divorceheartslost");
                    FL_GetSpouses = AccessTools.Method(FL_modType, "GetSpouses");
                    ModEntry.Log($"{Actions_Divorce}: {modId} Version");
                    TriggerActionManager.RegisterAction(Actions_Divorce, TriggerDivorce_FL);
                    return;
                }
            }
            catch (Exception err)
            {
                ModEntry.Log($"Failed to reflect into {modId}:\n{err}");
            }
        }
        ModEntry.Log($"{Actions_Divorce}: Vanilla Version");
        TriggerActionManager.RegisterAction(Actions_Divorce, TriggerDivorce);
    }

    private static bool TriggerDivorce(string[] args, TriggerActionContext context, out string error)
    {
        if (
            Game1.player.isMarriedOrRoommates()
            && ArgUtility.TryGetOptional(
                args,
                1,
                out string? spouse,
                out error,
                defaultValue: null,
                name: "string spouse"
            )
        )
        {
            if (spouse == null || Game1.player.spouse == spouse)
            {
                Game1.player.divorceTonight.Value = true;
                ModEntry.Log($"Divorcing tonight");
                return true;
            }
            error = $"Cannot divorce because you are not married to {spouse}.";
            return false;
        }
        error = "Cannot divorce because you are not married.";
        return false;
    }

    private static bool TriggerDivorce_FL(string[] args, TriggerActionContext context, out string error)
    {
        if (!ArgUtility.TryGet(args, 1, out string spouse, out error, allowBlank: false, name: "string spouse"))
        {
            return false;
        }
        if (FL_GetSpouses?.Invoke(FL_mod, [Game1.player, true]) is Dictionary<string, NPC> pslSpouses)
        {
            if (!pslSpouses.ContainsKey(spouse))
            {
                error =
                    $"Cannot divorce because you are not married to {spouse} (spouses: {string.Join(',', pslSpouses.Keys)}).";
                return false;
            }
            Game1.player.divorceTonight.Value = true;
            FL_spouseToDivorce?.SetValue(FL_mod, spouse);
            FL_divorceheartslost?.SetValue(FL_mod, -1);
            ModEntry.Log($"Divorcing {spouse} tonight (FL)");
            return true;
        }
        error = $"Failed to get spouses from FL.";
        return false;
    }
}
