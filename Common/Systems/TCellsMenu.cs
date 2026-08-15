using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace TerrariaCells.Common.Systems;

public class TCellsMenu : ModMenu
{
    const string LogoAssetPath = "TerrariaCells/Common/Assets/";

    private static Asset<Texture2D> newLogo;

    public override Asset<Texture2D> Logo => newLogo ??= ModContent.Request<Texture2D>(LogoAssetPath + "TitleLogo_NoTransparency");
}
