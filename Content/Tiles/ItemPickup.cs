using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using Terraria.UI;
using TerrariaCells.Common.GlobalNPCs;
using System.Reflection;
using System.Collections.Generic;
using TerrariaCells.Common.ModPlayers;
using System.Linq;
using Terraria.Audio;
using Terraria.ID;
using Terraria.GameContent.Drawing;

namespace TerrariaCells.Content.Tiles;

public class ItemPickup : ModTile
{
    private static System.Reflection.MethodInfo Main_DrawItem_Item_int;
    public override void Load()
    {
        Main_DrawItem_Item_int = typeof(Main).GetMethod("DrawItem", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod);
    }
    public override void SetStaticDefaults()
    {
        Main.tileNoAttach[Type] = true;
        Main.tileFrameImportant[Type] = true;
        Main.tileLavaDeath[Type] = false;
        

        TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
        TileObjectData.newTile.Width=2;
        TileObjectData.newTile.Height=2;
        TileObjectData.newTile.Origin=new Point16(0,0);
        TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent.GetInstance<ItemPickup_TE>().Generic_HookPostPlaceMyPlayer;
        TileObjectData.addTile(Type);
        
        AddMapEntry(Color.BurlyWood);
        DustType = Terraria.ID.DustID.WoodFurniture;
    }

    public override void KillMultiTile(int i, int j, int frameX, int frameY)
    {
        ModContent.GetInstance<ItemPickup_TE>().Kill(i, j);
    }

    public override bool RightClick(int i, int j)
    {
        if(Common.Configs.DevConfig.Instance.BuilderMode)
        {
            Item heldItem = Main.LocalPlayer.HeldItem;
            if(heldItem.IsAir) return true;

            if (!TileEntity.TryGet(i, j, out ItemPickup_TE tileEntity)) return true;

            tileEntity.HeldItem = new Item(heldItem.type);
            
            heldItem.stack--;
            if(heldItem.stack < 1)
                heldItem.TurnToAir();
        }
        return base.RightClick(i, j);
    }

    public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
    {
        base.PostDraw(i, j, spriteBatch);
        
        if(Main.LocalPlayer.GetModPlayer<ItemPickupPlayer>().AlreadyUsedTileAt(i, j)) return;
    
        if(TileObjectData.IsTopLeft(i, j))
        {
            if (Main.LocalPlayer.GetModPlayer<ItemPickupPlayer>().AlreadyUsedTileAt(i, j)) return;

            Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomNonSolid);
        }
    }

    public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
    {
        if (TileEntity.TryGet(i, j, out ItemPickup_TE tileEntity) && !tileEntity.HeldItem.IsAir)
        {
            Vector2 worldPosition = new Vector2(i * 16 + 8, j * 16 + 8);
            worldPosition.Y += MathF.Sin((float)Main.timeForVisualEffects * 0.06f) * 5f;

            Item toDraw = tileEntity.HeldItem.Clone();
            toDraw.SetDefaults(tileEntity.GetItemTypeForPlayer(Main.LocalPlayer), true);

            toDraw.position = worldPosition;

            Main_DrawItem_Item_int.Invoke(Main.instance, [toDraw, Main.maxItems - 1]);
        }
    }
}

internal class ItemPickup_TE : ModTileEntity
{
    public Item HeldItem = new Item();
    
    public int GetItemTypeForPlayer(Player player)
    {
        int itemTypeToUse = this.HeldItem.type;
        if (player.GetModPlayer<Common.ModPlayers.MetaPlayer>().CheckUnlocks(itemTypeToUse) != UI.UnlockState.Locked)
        {
            itemTypeToUse = DropFoodHeals.TIER_ONE_FOOD[itemTypeToUse % DropFoodHeals.TIER_ONE_FOOD.Length];
        }
        return itemTypeToUse;
    }

    public override bool IsTileValidForEntity(int x, int y)
    {
        Tile tile = Framing.GetTileSafely(x, y);
        return tile.HasTile && tile.TileType == ModContent.TileType<ItemPickup>();
    }

    public override void SaveData(TagCompound tag)
    {
        tag.Add(nameof(HeldItem), ItemIO.Save(HeldItem));
    }
    public override void LoadData(TagCompound tag)
    {
        HeldItem = tag.Get<Item>(nameof(HeldItem));
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(HeldItem.type);
    }
    public override void NetReceive(BinaryReader reader)
    {
        HeldItem = new Item(reader.ReadInt32(), 1, 0);
    }
}

public class ItemPickupPlayer : ModPlayer, RunReset.IPlayer
{
    private List<Point> pickedUp = new List<Point>();
    
    public bool AlreadyUsedTileAt(Point position)
    {
        return pickedUp.Contains(position);
    }
    
    public bool AlreadyUsedTileAt(int i, int j)
    {
        return pickedUp.Contains(new Point(i, j));
    }
    
    public override void PostUpdate()
    {
        //if(Common.Configs.DevConfig.Instance.BuilderMode) return;
        
        if(Player.whoAmI != Main.myPlayer) return;
        
        Point anchor = Player.position.ToTileCoordinates();
        for(int j = -1; j < 4; j++)
        {
            for(int i = -1; i < 3; i++)
            {
                Point tilePos = new Point(anchor.X + i, anchor.Y + j);
                Tile tile = Framing.GetTileSafely(tilePos);
                    
                if (tile.TileType != ModContent.TileType<ItemPickup>()) continue;

                //Getting top left of placed tile
                if (tile.TileFrameX > 0)
                    tilePos.X--;
                if (tile.TileFrameY > 0)
                    tilePos.Y--;

                if (pickedUp.Contains(tilePos)) continue;

                if (!TileEntity.TryGet<ItemPickup_TE>(tilePos.X, tilePos.Y, out var tileEntity)) continue;
                
                int itemType = tileEntity.GetItemTypeForPlayer(Player);
                Player.QuickSpawnItem(Player.GetSource_TileInteraction(tilePos.X, tilePos.Y), itemType, 1);
                Player.GetModPlayer<Common.ModPlayers.MetaPlayer>().UpdateItemStatus(itemType, UI.UnlockState.Unlocked);
                pickedUp.Add(tilePos);
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }
        }
    }

    public void Reset(RunReset.ResetContext context)
    {
        if(context == RunReset.ResetContext.NewWorld)
        {
            pickedUp = new List<Point>();
        }
    }

    public override void SaveData(TagCompound tag)
    {
        tag[$"{nameof(pickedUp)}_x"] = pickedUp.Select(p => p.X).ToList();
        tag[$"{nameof(pickedUp)}_y"] = pickedUp.Select(p => p.Y).ToList();
    }

    public override void LoadData(TagCompound tag)
    {
        IList<int> listX = tag.GetList<int>($"{nameof(pickedUp)}_x");
        IList<int> listY = tag.GetList<int>($"{nameof(pickedUp)}_y");
        pickedUp = listX.Zip(listY, (int x, int y) => new Point(x, y)).ToList();
    }
}