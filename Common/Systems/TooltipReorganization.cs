using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Frozen;
namespace TerrariaCells.Common.Systems;

public static class TooltipReorganization
{
    internal static readonly List<string> _tooltips = new List<string>
    {
        "ItemName", "Favorite", "FavoriteDesc", //Exist on all items
        "NoTransfer", //UNUSED
        "Social", "SocialDesc", //Social
        "Damage", "CritChance", "Speed", "NoSpeedScaling", "SpecialSpeedScaling", "Knockback", //Weapons
        "FishingPower", "NeedsBait", "BaitPower", //Fishing
        "Equipable", //Equips
        "WandConsumes",
        "Quest", //Fishing (2)
        "Vanity", "Defense", //Equips (2)
        "PickPower", "AxePower", "HammerPower", "TileBoost", //Tools
        "HealLife", "HealMana", "UseMana", //Resource
        "Placeable",
        "Ammo", "Consumable", "Material", //Use
        "Tooltip",
        "EtherianManaWarning", //Resource (2)
        "WellFedExpert", "BuffTime", //Buffs
        "OneDropLogo",
        "PrefixDamage", "PrefixSpeed", "PrefixCritChance", "PrefixUseMana", "PrefixSize", "PrefixShootSpeed", "PrefixKnockback", "PrefixAccDefense", "PrefixAccMaxMana", "PrefixAccCritChance", "PrefixAccDamage", "PrefixAccMoveSpeed", "PrefixAccMeleeSpeed", //Modifiers
        "SetBonus", //Equips (3)
        "Expert", "Master", "JourneyResearch", //Difficulty
        "ModifiedByMods",
        "BestiaryNotes",
        "SpecialPrice", "Price", //Price
    };

    ///<inheritdoc cref="Terraria.ModLoader.TooltipLine.TooltipLine(Mod, string, string)"/>
    public static IEnumerable<string> Tooltips => _tooltips;

    /// <summary>
    /// Use to load tooltips where order is important
    /// </summary>
    /// <param name="tooltipName"></param>
    /// <param name="afterAnchor"></param>
    /// <returns></returns>
    /// <remarks></remarks>
    public static bool LoadTooltip(string tooltipName, string afterAnchor)
    {
        if (_tooltips.Contains(tooltipName)) return false;
        int index = _tooltips.FindIndex(ttL => ttL.Equals(afterAnchor));
        if (index != -1)
        {
            _tooltips.Insert(++index, tooltipName);
            return true;
        }
        _tooltips.Add(tooltipName);
        return true;
    }

    public static int IndexTooltip(this List<TooltipLine> tooltips, string afterAnchor)
    {
        //Special case for Tooltip# because there can be as many of those as you'd like
        bool isTooltip_num = afterAnchor.StartsWith("Tooltip");

        //Check if anchor tooltip already exists. No reason to iterate '_tooltips' if it does
        int index = isTooltip_num ?
        tooltips.FindLastIndex(ttL => ttL.Name.StartsWith("Tooltip")) :
        tooltips.FindIndex(ttL => ttL.Name.Equals(afterAnchor));

        if (index != -1)
            return ++index;

        index = _tooltips.IndexOf(afterAnchor)-1;

        //Stardust - This could be a loop, but it'd just be repeating the logic again anyway
        if (index > -1)
            return tooltips.IndexTooltip(_tooltips[index]);

        ModContent.GetInstance<TerrariaCells>().Logger.Warn($"Anchor tooltip ('{nameof(afterAnchor)}'=\"{afterAnchor}\") was not found. Could not find valid tooltip insert location.");
        return tooltips.Count;
    }

    public static void InsertTooltip(this List<TooltipLine> tooltips, TooltipLine tooltip)
    {
        int index = _tooltips.IndexOf(tooltip.Name);
        if (index == -1)
        {
            tooltips.Add(tooltip);
            return;
        }
        if (index > 0)
            index--;
        tooltips.InsertTooltip(tooltip, _tooltips[index]);
    }

    public static void InsertTooltip(this List<TooltipLine> tooltips, TooltipLine tooltip, string afterAnchor)
    {
        tooltips.Insert(tooltips.IndexTooltip(afterAnchor), tooltip);
    }

    public static void ReplaceTooltip(this List<TooltipLine> tooltips, TooltipLine tooltip, string tooltipName)
    {
        if (_Global_Unaffected_Tooltips.Contains(tooltipName)) return;

        //Special case for Tooltip#
        bool isTooltip_num = tooltipName.StartsWith("Tooltip");

        int replaceIndex = tooltips.FindIndex(ttL => ttL.Mod.Equals("Terraria") && ttL.Visible && (ttL.Name.Equals(tooltipName) || (isTooltip_num && ttL.Name.StartsWith("Tooltip"))));
        if (replaceIndex != -1)
        {
            tooltips[replaceIndex] = tooltip;

            if (isTooltip_num)
            {
                foreach (TooltipLine ttL in tooltips)
                    if (ttL.Mod.Equals("Terraria") && ttL.Name.StartsWith("Tooltip"))
                        ttL.Hide();
            }
        }
        else
        {
            tooltips.InsertTooltip(tooltip, tooltipName);
        }
    }

    /// <summary> Set of tooltips by name that <b>should not</b> be hidden or filtered out </summary>
    internal static readonly FrozenSet<string> _Global_Unaffected_Tooltips = new HashSet<string>()
    {
        "ItemName", "SetBonus", "ModifiedByMods", "SpecialPrice", "Price"
    }.ToFrozenSet();
    /// <summary>
    /// Filters by VANILLA tooltips (modded tooltips assumed to be deliberate)
    /// </summary>
    /// <param name="tooltips"></param>
    /// <param name="tooltipNames"></param>
    /// <param name="asBlacklist"></param>
    public static void FilterTooltips(this List<TooltipLine> tooltips, string[] tooltipNames, bool asBlacklist = false)
    {
        foreach (TooltipLine tooltip in tooltips.Where(ttL => ttL.Mod.Equals("Terraria")))
        {
            bool isTooltip_num = tooltip.Name.StartsWith("Tooltip");
            if (_Global_Unaffected_Tooltips.Contains(tooltip.Name) || (isTooltip_num && _Global_Unaffected_Tooltips.Contains("Tooltip"))) continue;
            bool isInList = tooltipNames.Contains(tooltip.Name) || (isTooltip_num && tooltipNames.Any(s => s.StartsWith("Tooltip")));
            if (!isInList ^ asBlacklist)
                tooltip.Hide();
        }
    }
}