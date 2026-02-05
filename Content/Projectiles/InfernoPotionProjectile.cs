using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaCells.Content.Buffs;


namespace TerrariaCells.Content.Projectiles
{
    public class InfernoPotionProjectile : ModProjectile
    {
        float Ring1Opacity = 0f; //Opacity of the three rings
        float Ring2Opacity = 0f;
        float Ring3Opacity = 0f;

        float Ring1Scale = 1f; //Scale of the three rings
        float Ring2Scale = 1f;
        float Ring3Scale = 1f;

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60*2;
            Projectile.penetrate = -1;
            Projectile.width = Projectile.height = 185;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }


        public override void AI()
        {
            Projectile.ai[0] += 1; //Timer

            if (Main.player[Projectile.owner].HasBuff(ModContent.BuffType<CustomInfernoBuff>()))
                Projectile.timeLeft = 60*1;
            else
                Projectile.Kill();

            Lighting.AddLight(Projectile.Center, 0.5f, 0.25f, 0f);
            FadeInAndOut();
            SetScale();

        //Circle collision hitbox
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Distance(Projectile.Center) < Projectile.width && Projectile.active)
            {
                return true;
            }
            else return false;
        }

        //Visual effects
        public override bool PreDraw(ref Color lightColor)
        {
            return false; //Return false so the original projectile isn't drawn
        }

        //Makes the opacity of the rings increase and decrease so they fade in and out
        public void FadeInAndOut()
        {
            if (Projectile.ai[0] % 90 < 15)
                Ring1Opacity += 0.05f;
            else if (Projectile.ai[0] % 90 < 30)
                Ring3Opacity -= 0.05f;
            else if (Projectile.ai[0] % 90 < 45)
                Ring2Opacity += 0.05f;
            else if (Projectile.ai[0] % 90 < 60)
                Ring1Opacity -= 0.05f;
            else if (Projectile.ai[0] % 90 < 75)
                Ring3Opacity += 0.05f;
            else
                Ring2Opacity -= 0.05f;
        }
    }
}