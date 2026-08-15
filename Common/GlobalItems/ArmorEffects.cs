using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaCells.Common.ModPlayers;
using TerrariaCells.Content.WeaponAnimations;

namespace TerrariaCells.Common.GlobalItems
{
    public class ArmorEffects : GlobalItem, PostModifyTooltips.IGlobal
    {
        public override void Load()
        {
            On_Player.GrantArmorBenefits += On_Player_GrantArmorBenefits;
        }
        public override void Unload()
        {
            On_Player.GrantArmorBenefits -= On_Player_GrantArmorBenefits;
        }

        public override void SetDefaults(Item item)
        {
            switch (item.type)
            {
                case ItemID.NinjaHood:
                case ItemID.NinjaShirt:
                case ItemID.NinjaPants:
                case ItemID.JungleHat:
                case ItemID.JungleShirt:
                case ItemID.JunglePants:
                case ItemID.NecroHelmet:
                case ItemID.NecroBreastplate:
                case ItemID.NecroGreaves:
                case ItemID.MoltenHelmet:
                case ItemID.MoltenBreastplate:
                case ItemID.MoltenGreaves:
                case ItemID.GoldHelmet:
                case ItemID.GoldChainmail:
                case ItemID.GoldGreaves:
                    item.defense = 0;
                    break;
            }
        }

        private void On_Player_GrantArmorBenefits(On_Player.orig_GrantArmorBenefits orig, Player player, Item item)
        {
            ArmorPlayer modPlayer = player.GetModPlayer<ArmorPlayer>();
            switch (item.type)
            {
                // Ninja Armor
                case ItemID.NinjaHood:
                    modPlayer.ninjaHood = true;
                    player.GetDamage(DamageClass.Generic) += 0.15f;
                    break;
                case ItemID.NinjaShirt:
                    modPlayer.ninjaShirt = true;
                    break;
                case ItemID.NinjaPants:
                    modPlayer.ninjaPants = true;
                    player.moveSpeed += 0.15f;
                    break;

                // Jungle Armor
                case ItemID.JungleHat:
                    modPlayer.jungleHat = true;
                    player.GetDamage(DamageClass.Magic) += 0.2f;
                    break;
                case ItemID.JungleShirt:
                    modPlayer.jungleShirt = true;
                    //picking up mana stars reduces skill cooldowns by 1/2 second
                    break;
                case ItemID.JunglePants:
                    modPlayer.junglePants = true;
                    player.moveSpeed += 0.1f;
                    player.GetDamage(DamageClass.Magic) += 0.1f;
                    break;

                // Necro Armor
                case ItemID.NecroHelmet:
                    modPlayer.necroHelmet = true;
                    player.GetDamage(DamageClass.Ranged) += 0.2f;
                    break;
                case ItemID.NecroBreastplate:
                    modPlayer.necroBreastplate = true;
                    //killing an enemy spawns baby spiders, which attack nearby enemies
                    break;
                case ItemID.NecroGreaves:
                    modPlayer.necroGreaves = true;
                    player.moveSpeed += 0.1f;
                    //the last bullet in a magazine deals 50% more damage
                    break;

                // Molten Armor
                case ItemID.MoltenHelmet:
                    modPlayer.moltenHelmet = true;
                    player.GetDamage(DamageClass.Melee) += 0.2f;
                    break;
                case ItemID.MoltenBreastplate:
                    modPlayer.moltenBreastplate = true;
                    //-20% damage taken
                    //upon taking damage, all nearby enemies are lit on fire
                    break;
                case ItemID.MoltenGreaves:
                    modPlayer.moltenGreaves = true;
                    player.moveSpeed += 0.1f;
                    //leave a trail of flames that ignites enemies (hellfire treads, but functional)
                    break;
                
                case ItemID.GoldHelmet:
                    modPlayer.goldArmorCount++;
                    player.GetDamage(DamageClass.Generic) += Get_GoldMovespeed(player);
                    break;
                case ItemID.GoldGreaves:
                    modPlayer.goldArmorCount++;
                    player.moveSpeed += Get_GoldMovespeed(player);
                    break;
                case ItemID.GoldChainmail:
                    modPlayer.goldArmorCount++;
                    modPlayer.goldChestplate=true;
                    break;
                    

                default:
                    orig.Invoke(player, item);
                    break;
            }

            if (modPlayer.necroArmorSet)
            {
                Gun.StaticReloadTimeMult = 0.5f;
                Bow.StaticChargeTimeMult = 0.5f;
            }
            else
            {
                Gun.StaticReloadTimeMult = 1f;
                Bow.StaticChargeTimeMult = 1f;
            }
        }
        
        public override bool OnPickup(Item item, Player player)
        {
            if (player.GetModPlayer<ArmorPlayer>().jungleShirt)
            {
                if (item.type is ItemID.Star or ItemID.SoulCake or ItemID.SugarPlum)
                {
                    Systems.AbilityHandler modPlayer = player.GetModPlayer<Systems.AbilityHandler>();
                    foreach (Systems.AbilitySlot ability in modPlayer.Abilities)
                    {
                        if (ability.IsOnCooldown)
                            ability.cooldownTimer -= 30;
                    }
                }
            }
            return base.OnPickup(item, player);
        }

        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            var modPlayer = player.GetModPlayer<ArmorPlayer>();
            if (modPlayer.goldArmorCount > 0 && item.IsACoin)
            {
                grabRange += modPlayer.goldArmorCount * 2 * 16;
            }
        }
    
        public static float Get_GoldDamage(Player player)
        {
            long coinsCount = Utils.CoinsCount(out _, player.inventory, 58, 57, 56, 55, 54);

            //Geometric series that converges on 50
            float moveSpeedMod = 0.0002f * 0.999996f * (MathF.Pow(0.999996f, coinsCount) - 1) / (0.999996f - 1);
            //Add quadratic (X^4/9) for slow scaling
            moveSpeedMod += MathF.Pow(0.0002f * coinsCount, 4f / 9f);
            //Ensure it's represented as a percent
            moveSpeedMod *= 0.01f;

            // Movespeed increase   | Money required (appx)
            // ============================================
            // +10%                 | 4g10s
            // +15%                 | 6g70s
            // +25%                 | 13g40s
            // +33%                 | 20g70s
            // +50%                 | 47g35s
            return moveSpeedMod;
        }

        public static float Get_GoldMovespeed(Player player)
        {
            long coinsCount = Utils.CoinsCount(out _, player.inventory, 58, 57, 56, 55, 54);

            //Geometric series that converges on 50
            float damageMod = 0.0003f * 0.999994f * (MathF.Pow(0.999994f, coinsCount) - 1) / (0.999994f - 1);
            //Ensure it's represented as a percent
            damageMod *= 0.01f;

            // Movespeed increase   | Money required (appx)
            // ============================================
            // +10%                 | 2g70s
            // +15%                 | 4g50s
            // +25%                 | 8g90s
            // +33%                 | 13g75s
            // +50%                 | 31g50s
            return damageMod;
        }

        public void PostModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            switch (item.type)
            {
                case ItemID.GoldHelmet:
                    {
                        int index = tooltips.FindIndex(ttL => ttL.Name.StartsWith("Tooltip"));
                        if (index != -1)
                        {
                            tooltips[index].Text = string.Format(tooltips[index].Text, $"{100*Get_GoldMovespeed(Main.LocalPlayer):0.0}");
                        }
                        break;
                    }
                case ItemID.GoldGreaves:
                    {
                        int index = tooltips.FindIndex(ttL => ttL.Name.StartsWith("Tooltip"));
                        if (index != -1)
                        {
                            tooltips[index].Text = string.Format(tooltips[index].Text, $"{100*Get_GoldMovespeed(Main.LocalPlayer):0.0}");
                        }
                        break;
                    }
            }
        }
    }
}