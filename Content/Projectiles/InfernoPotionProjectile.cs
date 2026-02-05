using Microsoft.Build.Evaluation;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaCells.Content.Buffs;
using System.Net.Http.Headers;


namespace TerrariaCells.Content.Projectiles
{
    public class InfernoPotionProjectile : ModProjectile
    {
        float Ring1Opacity = 0f; //Opacity of the three rings
        float Ring2Opacity = 0f;
        float Ring3Opacity = 0.675f; //Third ring starts at max amplitude becuase of cyclic pattern

        float Ring1Scale = 1f; //Scale of the three rings
        float Ring2Scale = 1f;
        float Ring3Scale = 1f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 60*1;
            Projectile.penetrate = -1;
            Projectile.width = Projectile.height = 185;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60*1;
        }

        public override void AI()
        {
            Projectile.ai[0] += 1f; //Timer

            if (Main.player[Projectile.owner].HasBuff(ModContent.BuffType<CustomInfernoBuff>()))
            {
                Projectile.timeLeft = 60*1;
                Projectile.Center = Main.player[Projectile.owner].Center;
            }
            else
                Projectile.Kill();

            //Animation loop
            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
            }

            Lighting.AddLight(Projectile.Center, 0.5f, 0.25f, 0f);
            Projectile.rotation += 0.05f;
            FadeInAndOut();
            SetScale();
        }

        //Circle collision hitbox
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Distance(Projectile.Center) < Projectile.width && Projectile.active)
            {
                return true;
            }
            else return false;
        }

        //Apply on fire debuff
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            target.AddBuff(BuffID.OnFire, 60*5);
        }

        //Visual effects
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            //Get the frame height and the starting y pos for the frame in the sprite sheet
            int frameHeight = texture.Height / Main.projFrames[Type];
            int startY = frameHeight * Projectile.frame;

            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);

            Vector2 origin = sourceRectangle.Size() / 2f;
            Color drawColor = Color.White;

            //Draw first ring
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, drawColor * Ring1Opacity, Projectile.rotation, origin, Ring1Scale, spriteEffects, 0);
            //Draw second ring
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, drawColor* Ring2Opacity, Projectile.rotation, origin, Ring2Scale, spriteEffects, 0);
            //Draw third ring
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, drawColor* Ring3Opacity, Projectile.rotation, origin, Ring3Scale, spriteEffects, 0);

            return false; //Return false so the original projectile isn't drawn
        }

        //Makes the opacity of the rings increase and decrease so they fade in and out
        public void FadeInAndOut()
        {
            if (Projectile.ai[0] % 90 < 15)
                Ring1Opacity += 0.045f;
            else if (Projectile.ai[0] % 90 < 30)
                Ring3Opacity -= 0.045f;
            else if (Projectile.ai[0] % 90 < 45)
                Ring2Opacity += 0.045f;
            else if (Projectile.ai[0] % 90 < 60)
                Ring1Opacity -= 0.045f;
            else if (Projectile.ai[0] % 90 < 75)
                Ring3Opacity += 0.045f;
            else
                Ring2Opacity -= 0.045f;
        }

        //Make the rings expand in size and then reset
        public void SetScale()
        {
            Ring1Scale += 0.004f;
            Ring2Scale += 0.004f;
            Ring3Scale += 0.004f;

            if (Projectile.ai[0] % 90 == 1)
                Ring1Scale = 1f;
            if (Projectile.ai[0] % 90 == 31)
                Ring2Scale = 1f;
            if (Projectile.ai[0] % 90 == 61)
                Ring3Scale = 1f;
        }
    }
}