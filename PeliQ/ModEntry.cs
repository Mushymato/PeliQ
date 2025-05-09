global using SObject = StardewValley.Object;
using System.Diagnostics;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Internal;

namespace PeliQ;

public class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
    private static IMonitor mon = null!;
    internal static IManifest manifest = null!;
    internal static IModHelper help = null!;
    internal static Harmony harm = null!;

    internal static string ModId => manifest?.UniqueID ?? "ERROR";

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        manifest = ModManifest;
        help = helper;
        harm = new(ModId);

        Framework.GameStateQ.ItemRegex.Register();
        Framework.GameStateQ.LogicGates.Register();
        Framework.ItemQ.ActionSalable.Register();
        Framework.ItemQ.NestedQuery.Register();
        Framework.ItemQ.StoredQuery.Register();

        // This is supposed to be in some other framework but alas
        Framework.TriggerQ.Divorce.Register();
    }

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.LogOnce(msg, level);
    }
}
