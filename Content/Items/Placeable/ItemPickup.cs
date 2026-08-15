using System;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaCells.Content.Items.Placeable;

public class ItemPickup : ModItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<Content.Tiles.ItemPickup>());
        
        Item.width = 32;
        Item.height = 32;
    }
}
