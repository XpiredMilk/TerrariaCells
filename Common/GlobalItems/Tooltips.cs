using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Microsoft.Build.Evaluation;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using TerrariaCells.Common.Configs;
using TerrariaCells.Common.GlobalProjectiles;
using TerrariaCells.Common.Items;
using TerrariaCells.Common.ModPlayers;
using TerrariaCells.Common.Systems;
using TerrariaCells.Common.UI;
using TerrariaCells.Common.Utilities;
using TerrariaCells.Content.WeaponAnimations;

namespace TerrariaCells.Common.GlobalItems;
public partial class VanillaReworksGlobalItem
{
    static LocalizedText Cat_Weapons;
    static LocalizedText Cat_Abilities;
    static LocalizedText Cat_Accessories;
    static LocalizedText Cat_Armor;
    static LocalizedText Cat_Potions;
    private void LoadLocalization()
    {
        Cat_Weapons = Language.GetOrRegister(Mod.GetLocalizationKey("Tooltips.Category.Weapons"), () => "Weapon");
        Cat_Abilities = Language.GetOrRegister(Mod.GetLocalizationKey("Tooltips.Category.Abilities"), () => "Ability");
        Cat_Accessories = Language.GetOrRegister(Mod.GetLocalizationKey("Tooltips.Category.Accessories"), () => "Accessory");
        Cat_Armor = Language.GetOrRegister(Mod.GetLocalizationKey("Tooltips.Category.Armor"), () => "Armour");
        Cat_Potions = Language.GetOrRegister(Mod.GetLocalizationKey("Tooltips.Category.Potions"), () => "Potion");

        TooltipReorganization.LoadTooltip("Category", "ItemName");
    }

    private void SetNameOverrides(Item item)
    {
        if (ItemID.Search.TryGetName(item.type, out string internalName))
        {
            string key = Mod.GetLocalizationKey($"Tooltips.Items.{internalName}.ItemName");
            if (Language.Exists(key))
            {
                item.SetNameOverride(Language.GetTextValue(key));
            }
        }
    }
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (ItemsJson.Instance.Category.TryGetValue(item.type, out var cat))
        {
            TooltipLine categoryTooltip;
            switch (cat)
            {
                case ItemsJson.ItemCategory.Weapons:
                    categoryTooltip = new TooltipLine(Mod, "Category", Cat_Weapons.Value) { OverrideColor = Color.DarkRed };
                    tooltips.FilterTooltips(new string[] { "Damage", "Speed", "UseMana", "Tooltip" });
                    break;

                case ItemsJson.ItemCategory.Abilities:
                    categoryTooltip = new TooltipLine(Mod, "Category", Cat_Abilities.Value) { OverrideColor = Color.ForestGreen };
                    tooltips.FilterTooltips(new string[] { "Damage", "Tooltip" });
                    break;

                case ItemsJson.ItemCategory.Accessories:
                    categoryTooltip = new TooltipLine(Mod, "Category", Cat_Accessories.Value) { OverrideColor = Color.DarkGoldenrod };
                    tooltips.FilterTooltips(new string[] { "Equipable", "Tooltip" });
                    break;

                case ItemsJson.ItemCategory.Armor:
                    categoryTooltip = new TooltipLine(Mod, "Category", Cat_Armor.Value) { OverrideColor = Color.DarkSlateBlue };
                    tooltips.FilterTooltips(new string[] { "Equipable", "Tooltip" });
                    break;


                case ItemsJson.ItemCategory.Potions:
                    categoryTooltip = new TooltipLine(Mod, "Category", Cat_Potions.Value) { OverrideColor = (Color.DeepSkyBlue * 0.6f) with { A = 255 } };
                    tooltips.FilterTooltips(new string[] { "HealLife", "Consumable", "Tooltip" });
                    break;

                default:
                    return;
            }
            tooltips.InsertTooltip(categoryTooltip, "ItemName");
        }

        if (ItemID.Search.TryGetName(item.type, out string internalName))
        {
            foreach (string tooltipName in TooltipReorganization._tooltips)
            {
                string key = Mod.GetLocalizationKey($"Tooltips.Items.{internalName}.{tooltipName}");
                if (Language.Exists(key))
                {
                    tooltips.ReplaceTooltip(new TooltipLine(Mod, tooltipName, Language.GetTextValue(key)), tooltipName);
                }
            }
        }
        
        PostModifyTooltips.Invoke(item, tooltips);
    }
}
//I'd have liked this not to be necessary, using monomod hooks or something
//And, technically, I can replicate tMod's logic for tooltip construction
//And then just leave it be.

//But that's really obnoxious, and I can just do this instead.
//SO! We'll be using these when we need to modify tooltips, because
//We need to defer any changes (eg, formatting) until AFTER the above
//System has updated items' tooltips
public class PostModifyTooltips : ILoadable
{
    public interface IItem
    {
        public void PostModifyTooltips(List<TooltipLine> tooltips);
    }
    public interface IGlobal
    {
        public void PostModifyTooltips(Item item, List<TooltipLine> tooltips);
    }

    internal static void Invoke(Item item, List<TooltipLine> tooltips)
    {
        if (item.ModItem is IItem i)
            i.PostModifyTooltips(tooltips);

        foreach (GlobalItem g in _hook.Enumerate(item))
        {
            if (g is not IGlobal ig) continue;

            ig.PostModifyTooltips(item, tooltips);
        }
    }
    private static GlobalHookList<GlobalItem> _hook;
    public void Load(Mod mod)
    {
        _hook = ItemLoader.AddModHook(GlobalHookList<GlobalItem>.Create(e => ((IGlobal)e).PostModifyTooltips));
    }
    public void Unload()
    {
        _hook = null;
    }
}