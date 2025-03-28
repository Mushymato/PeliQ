using StardewValley.GameData.Machines;

namespace PeliQ.Framework;

/// <summary>The data for an item to create with support for a game state query, used in data assets like <see cref="T:StardewValley.GameData.Machines.MachineData" /> or <see cref="T:StardewValley.GameData.Shops.ShopData" />.</summary>
public class PeliQSpawnItemData : MachineItemOutput
{
    public string? InputId { get; set; } = null;
}
