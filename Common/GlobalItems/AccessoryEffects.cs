using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.Localization;
using System.Collections.Generic;
using TerrariaCells.Common.ModPlayers;

namespace TerrariaCells.Common.GlobalItems
{
	public class AccessoryEffects : GlobalItem
	{
		public override void Load()
		{
			On_Player.ApplyEquipFunctional += On_Player_ApplyEquipFunctional;
		}
		public override void Unload()
		{
			On_Player.ApplyEquipFunctional -= On_Player_ApplyEquipFunctional;
		}

		public override void SetDefaults(Item item)
		{
			int[] NewAccessoryTypes = new int[] {
				ItemID.BallOfFuseWire, ItemID.ChlorophyteDye,
			};

			if (NewAccessoryTypes.Contains(item.type))
			{
				item.DefaultToAccessory(item.width, item.height);
				item.maxStack = 1;
			}

			switch (item.type)
			{
				case ItemID.ChlorophyteDye:
					item.SetNameOverride("Chlorophyte Coating");
					item.dye = 0;
					item.glowMask = -1;
					break;
				case ItemID.BallOfFuseWire:
					item.shoot = ProjectileID.None;
					item.buffType = 0;
					item.useAnimation = 0;
					item.useTime = 0;
					item.useStyle = ItemUseStyleID.None;
					item.UseSound = null;
					break;
				case ItemID.BerserkerGlove:
					item.defense = 0;
					break;
				case ItemID.ObsidianShield:
					item.defense = 0;
					break;
                case ItemID.FlaskofVenom:
                    item.consumable = false;
                    item.maxStack = 1;
                    item.accessory = true;
                    item.useStyle = 0;
                    item.buffType = 0;
                    item.buffTime = 0;
                    break;
			}
			if (item.type == ItemID.ChlorophyteDye)
			{
				item.SetNameOverride("Chlorophyte Coating");
				item.dye = 0;
				item.glowMask = -1;
			}
			if (item.type == ItemID.BallOfFuseWire)
			{
				item.shoot = ProjectileID.None;
				item.buffType = 0;
				item.useAnimation = 0;
				item.useTime = 0;
				item.useStyle = ItemUseStyleID.None;
				item.UseSound = null;
			}
		}

		private void On_Player_ApplyEquipFunctional(On_Player.orig_ApplyEquipFunctional orig, Player player, Item item, bool hideVisual)
		{
			ModPlayers.AccessoryPlayer modPlayer = player.GetModPlayer<ModPlayers.AccessoryPlayer>();
			switch (item.type)
			{
				case ItemID.AvengerEmblem:
				player.GetDamage(DamageClass.Generic) += 0.15f;
					break;
				case ItemID.FastClock:
					modPlayer.fastClock = true;
					break;
				case ItemID.BandofRegeneration:
					modPlayer.bandOfRegen = true;
					break;
				case ItemID.FrozenTurtleShell:
					modPlayer.frozenShieldItem = item;
					break;
				case ItemID.ObsidianShield:
					player.noKnockback = true;
                    player.endurance += 0.2f;
					break;
				case ItemID.ThePlan:
					modPlayer.thePlan = true;
					break;
				case ItemID.CelestialStone:
					modPlayer.celestialStone = true;
					break;
				case ItemID.PygmyNecklace:
					modPlayer.pygmyNecklace = true;
					break;
				case ItemID.FeralClaws:
					player.GetAttackSpeed(DamageClass.Melee) += 0.4f;
					break;
				case ItemID.Nazar:
					modPlayer.nazar = true;
					break;
				case ItemID.BerserkerGlove:
                    if (player.GetModPlayer<Regenerator>().DamageLeft > 0)
                        player.GetDamage(DamageClass.Melee) += 0.3f;
					break;

				case ItemID.ReconScope:
					modPlayer.reconScope = true;
					break;
				case ItemID.BallOfFuseWire:
					modPlayer.fuseKitten = true;
					break;
				case ItemID.ChlorophyteDye:
					modPlayer.chlorophyteCoating = true;
					break;
				//case ItemID.AmmoBox: break;
				case ItemID.StalkersQuiver:
					modPlayer.stalkerQuiver = true;
					break;

				case ItemID.ArcaneFlower:
					player.GetDamage(DamageClass.Magic) += 0.50f;
					player.manaCost += 0.5f;
					break;
				case ItemID.ManaRegenerationBand:
					player.manaRegenDelayBonus += 4f;
					player.manaRegenBonus += 50;
					break;
				case ItemID.NaturesGift:
					//I know suggestion is 25%, I'm going 33% because you're sacrificing SO MUCH for this boost to mana cost of all things
					player.manaCost -= 0.33f;
					break;
				case ItemID.MagicCuffs:
					modPlayer.magicCuffs = true;
					break;
                case ItemID.FlaskofVenom:
                    player.GetModPlayer<BuffPlayer>().ReplaceBuffWith[BuffID.Poisoned] = BuffID.Venom;
                    break;
                case ItemID.HerculesBeetle:
                    modPlayer.heracles = true;
                    break;
                case ItemID.PhilosophersStone:
                    modPlayer.philoStone = true;
                    break;
				default:
					orig.Invoke(player, item, hideVisual);
					break;
			}
		}

		public override void GrabRange(Item item, Player player, ref int grabRange)
		{
			if ((item.type == ItemID.Star || item.type == ItemID.SoulCake || item.type == ItemID.SugarPlum) && player.manaMagnet)
				grabRange = 15 * 16;
		}
	}
}
