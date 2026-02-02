using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaCells.Content.Buffs;


namespace TerrariaCells.Content.Projectiles
{
    public class InfernoPotionProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60*1;
        }


        public override void AI()
        {
            if (Projectile..HasBuff(ModContent.BuffType<CustomInfernoBuff>()))
            {
                Projectile.timeLeft = 60*1;
            }
        }
    }
}