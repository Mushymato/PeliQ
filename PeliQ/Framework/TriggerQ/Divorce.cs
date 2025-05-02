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

    /// <summary>ApryllForever.PolyamorySweetLove compat</summary>
    private static IMod? PSL_mod = null;
    private static FieldInfo? PSL_spouseToDivorce = null;
    private static FieldInfo? PSL_divorceheartslost = null;
    private static MethodInfo? PSL_GetSpouses = null;

    internal static void Register()
    {
        try
        {
            var modInfo = ModEntry.help.ModRegistry.Get("ApryllForever.PolyamorySweetLove");
            if (modInfo?.GetType().GetProperty("Mod")?.GetValue(modInfo) is IMod mod)
            {
                PSL_mod = mod;
                Type PSL_modType = mod.GetType();
                PSL_spouseToDivorce = AccessTools.Field(PSL_modType, "spouseToDivorce");
                PSL_divorceheartslost = AccessTools.Field(PSL_modType, "divorceheartslost");
                PSL_GetSpouses = AccessTools.Method(PSL_modType, "GetSpouses");
                ModEntry.Log($"{Actions_Divorce}: PSL Version");
                TriggerActionManager.RegisterAction(Actions_Divorce, TriggerDivorce_PSL);
                return;
            }
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to reflect into ApryllForever.PolyamorySweetLove:\n{err}");
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

    private static bool TriggerDivorce_PSL(string[] args, TriggerActionContext context, out string error)
    {
        if (!ArgUtility.TryGet(args, 1, out string spouse, out error, allowBlank: false, name: "string spouse"))
        {
            return false;
        }
        if (PSL_GetSpouses?.Invoke(PSL_mod, [Game1.player, true]) is Dictionary<string, NPC> pslSpouses)
        {
            if (!pslSpouses.ContainsKey(spouse))
            {
                error =
                    $"Cannot divorce because you are not married to {spouse} (spouses: {string.Join(',', pslSpouses.Keys)}).";
                return false;
            }
            Game1.player.divorceTonight.Value = true;
            PSL_spouseToDivorce?.SetValue(PSL_mod, spouse);
            PSL_divorceheartslost?.SetValue(PSL_mod, -1);
            ModEntry.Log($"Divorcing {spouse} tonight (PSL)");
            return true;
        }
        error = $"Failed to get spouses from PSL.";
        return false;
    }
}
