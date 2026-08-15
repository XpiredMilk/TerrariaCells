using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaCells.Content.Projectiles;
using TerrariaCells.Common.Systems;


namespace TerrariaCells.Content.Buffs
{
    public class CustomInfernoBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.HasBuff(ModContent.BuffType<CustomInfernoBuff>()) && player.ownedProjectileCounts[ModContent.ProjectileType<InfernoPotionProjectile>()] < 1 && Main.myPlayer == player.whoAmI)
            {
                Projectile.NewProjectileDirect(player.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<InfernoPotionProjectile>(), ContentSamples.ItemsByType[ItemID.InfernoPotion].damage, 0f, Main.myPlayer);
            }
        }
    }
}