using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Net;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Localization;
using Terraria.Chat;
using TerrariaCells;
using TerrariaCells.Common.GlobalNPCs;
using TerrariaCells.Common.Utilities;
using TerrariaCells.Common.Systems;
using TerrariaCells.Common.ModPlayers;
using System.Collections.Generic;
using Terraria.WorldBuilding;

namespace TerrariaCells.Content.Packets
{
    internal class RunDataPacketHandler() : PacketHandler(TCPacketType.RunDataPacket)
    {
        public override void HandlePacket(Mod mod, BinaryReader reader, int fromWho)
        {
            if(Main.netMode != NetmodeID.MultiplayerClient) return;

            RunDataSystem system = ModContent.GetInstance<RunDataSystem>();
            system._path = new List<string>();
            system._runSummary = new Dictionary<string, RunDataSystem.RunData>();

            ushort len = reader.ReadUInt16();
            for(int i = 0; i < len; i++)
            {
                string path = reader.ReadString();
                long ticks = reader.ReadInt64();
                system._path.Add(path);
                system._runSummary.Add(path, RunDataSystem.RunData.Default with { Time = TimeSpan.FromTicks(ticks) });
            }
            
            DeadCellsUISystem.ToggleActive<RunDataWindow>(true);
        }
    }
}