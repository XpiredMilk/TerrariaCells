using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace TerrariaCells.Common.ModPlayers;

public class RunReset : ILoadable
{
    public enum ResetContext
    {
        NewWorld,
        Death,
        Respawn,
    }
    
    //Aim to replace all direct calls from 'DeathReset' to hook with this
    public interface IPlayer
    {
        void Reset(ResetContext context);
    }
    
    private static HookList<ModPlayer> _hook;
    internal static void Invoke(Player player, ResetContext context)
    {
        foreach(ModPlayer modPlayer in _hook.Enumerate(player))
        {
            if(modPlayer is not IPlayer iModPlayer) continue;
            
            iModPlayer.Reset(context);
        }
    }
    
    public void Load(Mod mod)
    {
        _hook = PlayerLoader.AddModHook(HookList<ModPlayer>.Create(e => ((IPlayer)e).Reset));
    }
    public void Unload()
    {
        _hook = null;
    }
}