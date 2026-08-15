using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaCells.Common.GlobalNPCs.NPCTypes.Shared
{
    public partial class Fighters : GlobalNPC
    {
        //ai[0]: 

        public bool DrawIceGolem(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Asset<Texture2D> t = TextureAssets.Npc[npc.type];
            Main.instance.LoadProjectile(ProjectileID.PhantasmalDeathray);
            Asset<Texture2D> deathRay = TextureAssets.Projectile[ProjectileID.PhantasmalDeathray];

            spriteBatch.Draw(
                t.Value,
                npc.Center - screenPos + new Vector2(0, npc.height / 2 + 5),
                new Rectangle(npc.frame.X, npc.frame.Y, npc.frame.Width, npc.frame.Height),
                drawColor,
                npc.rotation,
                new Vector2(t.Width() / 2, t.Height() / Main.npcFrameCount[npc.type]),
                new Vector2(npc.scale * 1.1f, npc.scale),
                npc.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0);

            Dust.NewDust(
                npc.Center,
                1,
                1,
                DustID.Phantasmal,
                0,
                -100);

            Vector2 sourcePosition = npc.Center;
            Vector2 destination = Main.MouseWorld;

            float scale = 1f;

            Vector2 toDestination = destination - sourcePosition;
            float angle1 = (float)Math.Atan2(toDestination.Y, toDestination.X) - (float)Math.PI * 0.5f;
            float angle2 = angle1 + (float)Math.PI;
            float length = toDestination.Length() * 0.5f;
            length = Math.Max(length, 24 * scale);

            float localLength = length / Math.Max(scale, 0.001f);

            Vector2 positionOffset1 = new Vector2(21 * scale, (160 - 9) * scale);
            positionOffset1 = positionOffset1.RotatedBy(angle1);
            Vector2 positionOffset2 = positionOffset1.RotatedBy(Math.PI);

            Vector2 position1 = (sourcePosition - screenPos) + positionOffset1;
            Vector2 position2 = (destination - screenPos + Main.ScreenSize.ToVector2() / 2) / 2 + positionOffset2;

            spriteBatch.Draw(
                deathRay.Value,
                position1,
                new Rectangle(0, 0, 36, (int)(localLength)),
                drawColor,
                angle1,
                new Vector2(t.Width() / 2, t.Height() / 12),
                new Vector2(scale, scale),
                npc.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0);

            spriteBatch.Draw(
                deathRay.Value,
                position2,
                new Rectangle(0, 0, 36, (int)(localLength)),
                drawColor,
                angle2,
                new Vector2(t.Width() / 2, t.Height() / 12),
                new Vector2(scale, scale),
                npc.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                0);

            //if (npc.ai[3] == 0)
            //{
            //}
            //else if (npc.ai[3] == 1)
            //{
            //    spriteBatch.Draw(deathRay.Value, npc.Center - screenPos + new Vector2(-2, npc.height + 14), new Rectangle(0, (int)CustomFrameY * 54, 44, 52), drawColor, npc.rotation, new Vector2(t.Width() / 2, t.Height() / 12), new Vector2(npc.scale * 1.1f, npc.scale), npc.direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0);
            //}

            return false;
        }

        public void IceGolemFrame(NPC npc)
        {
        }
        public void IceGolemAI(NPC npc, Player? target)
        {
        }
    }
}
