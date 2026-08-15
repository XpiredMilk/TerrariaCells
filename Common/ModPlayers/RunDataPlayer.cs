using System;
using System.Collections.Generic;

using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ModLoader.IO;
using System.Linq;
using System.IO;
using TerrariaCells.Common.Utilities;
using Terraria.UI;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using Terraria.ID;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria.UI.Chat;
using Terraria.GameInput;

namespace TerrariaCells.Common.Systems;

public class RunDataPlayer : ModPlayer
{
    #region Run Info
    public record struct RunData : TagSerializable
    {
        public static readonly Func<TagCompound, RunData> DESERIALIZER = Load;

        public static RunData Default => new RunData()
        {
            Crits = 0,
            DamageTaken = 0,
            DamageDealt = 0,
            Debuffs = 0,
            KillCount = 0,
            TotalMoney = 0,
            LifeCrystalsFound = 0,
        };
        public uint Crits;
        public uint DamageTaken;
        public uint Debuffs;
        public uint DamageDealt;
        public uint KillCount;
        public ulong TotalMoney;
        public ushort LifeCrystalsFound; //Can be translated into total health -> x20
        
        public static RunData operator +(RunData a, RunData b)
        {
            a.Crits += b.Crits;
            a.DamageTaken += b.DamageTaken;
            a.Debuffs += b.Debuffs;
            a.DamageDealt += b.DamageDealt;
            a.KillCount += b.KillCount;
            a.TotalMoney += b.TotalMoney;
            a.LifeCrystalsFound += b.LifeCrystalsFound;
            return a;
        }

        public readonly TagCompound SerializeData()
        {
            return new TagCompound()
            {
                [nameof(Crits)] = Crits,
                [nameof(DamageTaken)] = DamageTaken,
                [nameof(Debuffs)] = Debuffs,
                [nameof(DamageDealt)] = DamageDealt,
                [nameof(KillCount)] = KillCount,
                [nameof(TotalMoney)] = TotalMoney,
                [nameof(LifeCrystalsFound)] = LifeCrystalsFound,
            };
        }
        
        public static RunData Load(TagCompound tag)
        {
            return new RunData()
            {
                Crits = tag.Get<uint>(nameof(Crits)),
                DamageTaken = tag.Get<uint>(nameof(DamageTaken)),
                Debuffs = tag.Get<uint>(nameof(Debuffs)),
                DamageDealt = tag.Get<uint>(nameof(DamageDealt)),
                KillCount = tag.Get<uint>(nameof(KillCount)),
                TotalMoney = tag.Get<ulong>(nameof(TotalMoney)),
                LifeCrystalsFound = tag.Get<ushort>(nameof(LifeCrystalsFound)),
            };
        }
    }

    private string currentRegion = "forest";
    
    private RunData currentRegionData = RunData.Default;
    public RunData CurrentRegionData { get => currentRegionData; private set => currentRegionData = value; }
    //["Total"] = sum of all others
    //["Forest"] = info obtained in forest
    internal Dictionary<string, RunData> _runSummary = new Dictionary<string, RunData>();

    public void FlushPath()
    {
        if(!_runSummary.TryAdd(currentRegion.ToLower(), CurrentRegionData))
        {
            Main.NewText($"Player could not flush current data: {currentRegion}");
        }
    }
    public void SetNewRegion(string @new)
    {
        CurrentRegionData = RunData.Default;
        currentRegion = @new;
    }
    public void FlushPath(string @new)
    {
        FlushPath();
        SetNewRegion(@new.ToLower());
        //Main.NewText($"Player flushing with {@new}");
    }

    public void Reset()
    {
        _runSummary.Clear();
        currentRegion = "Forest";
        CurrentRegionData = RunData.Default;
    }
    #endregion

    #region Data Collection
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if(hit.Crit)
            currentRegionData.Crits++;
        if(target.life < 1)
            currentRegionData.KillCount++;
        currentRegionData.DamageDealt+=(ushort)damageDone;
    }
    public override void PostHurt(Player.HurtInfo info)
    {
        currentRegionData.DamageTaken+=(ushort)info.Damage;
    }
    public override bool OnPickup(Item item)
    {
        if(item.IsACoin)
        {
            ulong amount = item.type switch
            {
                ItemID.CopperCoin => (ulong)item.stack,
                ItemID.SilverCoin => 1_00 * (ulong)item.stack,
                ItemID.GoldCoin => 1_00_00 * (ulong)item.stack,
                ItemID.PlatinumCoin => 1_00_00_00 * (ulong)item.stack,
            };
            currentRegionData.TotalMoney += amount;
        }
        return base.OnPickup(item);
    }
    public void PickupLifeCrystal()
    {
        currentRegionData.LifeCrystalsFound++;
    }
    public void AppliedDebuff(int stacks)
    {
        currentRegionData.Debuffs+=(uint)stacks;
        //Main.NewText("Applied debuff!");
    }
    #endregion

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if(triggersSet.Inventory)
            DeadCellsUISystem.ToggleActive<RunDataWindow>(false);
    }

    public override void SaveData(TagCompound tag)
    {
        List<string> runSummaryKeys = _runSummary.Keys.ToList<string>();
        List<RunData> runSummaryValues = _runSummary.Values.ToList();
        tag.Add(nameof(runSummaryKeys), runSummaryKeys);
        tag.Add(nameof(runSummaryValues), runSummaryValues);

        tag.Add(nameof(currentRegion), currentRegion);
        tag.Add(nameof(CurrentRegionData), CurrentRegionData);
    }

    public override void LoadData(TagCompound tag)
    {
        List<string> runSummaryKeys = tag.Get<List<string>>(nameof(runSummaryKeys));
        List<RunData> runSummaryValues = tag.Get<List<RunData>>(nameof(runSummaryValues));

        _runSummary = runSummaryKeys.Zip(runSummaryValues, (k, v) => new { Key = k, Value = v }).ToDictionary(x => x.Key, x => x.Value);

        currentRegion = tag.Get<string>(nameof(currentRegion));
        CurrentRegionData = tag.Get<RunData>(nameof(CurrentRegionData));
    }
}
public class RunDataSystem : ModSystem
{
    public record struct RunData : TagSerializable
    {
        public static readonly Func<TagCompound, RunData> DESERIALIZER = Load;

        public static RunData Default => new RunData()
        {
            Time = TimeSpan.Zero,
        };
        public TimeSpan Time;

        public TagCompound SerializeData()
        {
            return new TagCompound()
            {
                [nameof(Time)] = Time.Ticks,
            };
        }
        
        public static RunData Load(TagCompound tag)
        {
            return new RunData()
            {
                Time = TimeSpan.FromTicks(tag.Get<long>(nameof(Time))),
            };
        }
    }
    internal List<string> _path = new List<string>();
    internal Dictionary<string, RunData> _runSummary = new Dictionary<string, RunData>();
    public string CurrentRegion { get; private set; } = "forest";
    
    private RunData currentRegionData = RunData.Default;
    public RunData CurrentRegionData { get => currentRegionData; private set => currentRegionData = value; }
    public void FlushPath()
    {
        if(!_runSummary.TryAdd(CurrentRegion.ToLower(), CurrentRegionData with { Time = RewardTrackerSystem.LevelTime - currentRegionData.Time }))
        {
            if(Main.dedServ)
                System.Console.WriteLine($"World could not flush current data {CurrentRegion}");
            else
                Main.NewText($"World could not flush current data {CurrentRegion}");
        }
        else
        {
            _path.Add(CurrentRegion);
        }
    }
    public void SetNewRegion(string @new)
    {
        currentRegionData = RunData.Default with { Time = RewardTrackerSystem.LevelTime };
        CurrentRegion = @new;
    }
    public void FlushPath(string @new)
    {
        FlushPath();
        SetNewRegion(@new.ToLower());
        //Main.NewText($"System flushing with {@new}");
    }

    public void Reset()
    {
        _path.Clear();
        _runSummary.Clear();
        CurrentRegion = "forest";
        currentRegionData = RunData.Default;
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag.Add(nameof(_path), _path);
        
        List<string> runSummaryKeys = _runSummary.Keys.ToList<string>();
        List<RunData> runSummaryValues = _runSummary.Values.ToList();
        tag.Add(nameof(runSummaryKeys), runSummaryKeys);
        tag.Add(nameof(runSummaryValues), runSummaryValues);
        
        tag.Add(nameof(CurrentRegion), CurrentRegion);
        tag.Add(nameof(CurrentRegionData), CurrentRegionData);
    }

    public override void LoadWorldData(TagCompound tag)
    {
        _path = tag.Get<List<string>>(nameof(_path));

        List<string> runSummaryKeys = tag.Get<List<string>>(nameof(runSummaryKeys));
        List<RunData> runSummaryValues = tag.Get<List<RunData>>(nameof(runSummaryValues));
        
        _runSummary = runSummaryKeys.Zip(runSummaryValues, (k, v) => new { Key = k, Value = v }).ToDictionary(x => x.Key, x => x.Value);
        
        CurrentRegion = tag.Get<string>(nameof(CurrentRegion));
        CurrentRegionData = tag.Get<RunData>(nameof(CurrentRegionData));
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(CurrentRegion);
        writer.Write(CurrentRegionData.Time.Ticks);
    }

    public override void NetReceive(BinaryReader reader)
    {
        CurrentRegion = reader.ReadString();
        currentRegionData.Time = TimeSpan.FromTicks(reader.ReadInt64());
    }
}

//In all, this is how this should appear:
/*

    [Summary] --- [Forest] --- [CORRUPTION] --- [Desert] --- [Hive] --- [Caverns]

    @ Time                                                                   3:00
        (Time spent on this level)
    # Kills                                                                    10
    % Hits Taken                                                                3
    # Damage Taken                                                             35
    % Hits Dealt                                                              192
    # Damage Dealt                                                           1869
    $ Money Collected                               [i/s3:73][i/s70:72][i/s30:71]
    ~ Life Obtained                                                     [i/s4:29]

                                                                                    */
//Assume, above, Corruption is selected, "@ Time" is being hovered
//Specific information may vary
public class RunDataWindow : Common.UI.Components.Windows.WindowState
{
    internal override string Name => "TerraCells.RunDataWindow";

    private PathDisplay Path;
    private InformationDisplay Display;
    private CloseUIButton CloseButton;
    private ExitUIButton EndRunButton;

    bool isOpen = false;
    protected override void OnOpened()
    {
        isOpen = true;
        Main.playerInventory = false;
        Path.Reset();
        Display.UpdateDisplay("current");
        Recalculate();
    }
    protected override void OnClosed()
    {
        isOpen = false;
    }

    public override void OnInitialize()
    {
        //Scoped UI code style from tomat and math2

        Left.Set(0, 0.35f);
        Top.Set(0, 0.3f);
        Width.Set(0, 0.3f);
        Height.Set(0, 0.4f);

        Path = new PathDisplay();
        {
            Path.Left.Set(0, 0);
            Path.Top.Set(0, 0);
            Path.Width.Set(0, 1);
            Path.Height.Set(64, 0);
        }
        Append(Path);

        Display = new InformationDisplay();
        {
            Display.Left.Set(0, 0);
            Display.Top.Set(64, 0);
            Display.Width.Set(0, 1);
            Display.Height.Set(-64, 1);
        }
        Append(Display);
        
        CloseButton = new CloseUIButton();
        {
            CloseButton.Left.Set(0, 0);
            CloseButton.Top.Set(-48, 1);
            CloseButton.Width.Set(0, 0.5f);
            CloseButton.Height.Set(48, 0);
        }
        Append(CloseButton);
        
        EndRunButton = new ExitUIButton();
        {
            EndRunButton.Left.Set(0, 0.5f);
            EndRunButton.Top.Set(-48, 1);
            EndRunButton.Width.Set(0, 0.5f);
            EndRunButton.Height.Set(48, 0);
        }
        Append(EndRunButton);
        
        isOpen = true;
        Recalculate();
        isOpen = false;
    }

    public override void Recalculate()
    {
        base.Recalculate();
        
        if(!isOpen) return;
        
        Left.Set(0, 0.35f);
        Top.Set(0, 0.25f);
        Width.Set(0, 0.3f);
        Height.Set(0, 0.5f);
    }

    public override void RecalculateChildren()
    {
        base.RecalculateChildren();
        
        if(!isOpen) return;
        
        Path.Left.Set(0, 0.05f);
        Path.Top.Set(0, 0.05f);
        Path.Width.Set(0, 0.9f);
        Path.Height.Set(64, 0);
        
        Display.Left.Set(0, 0.05f);
        Display.Top.Set(64, 0.1f);
        Display.Width.Set(0, 0.9f);
        Display.Height.Set(-64, 0.8f);

        CloseButton.Left.Set(0, 0);
        CloseButton.Top.Set(-48, 1);
        CloseButton.Width.Set(0, 0.5f);
        CloseButton.Height.Set(48, 0);

        EndRunButton.Left.Set(0, 0.5f);
        EndRunButton.Top.Set(-48, 1);
        EndRunButton.Width.Set(0, 0.5f);
        EndRunButton.Height.Set(48, 0);
    }

    protected override void WindowUpdate(GameTime time)
    {
        if(isOpen)
            Recalculate();
    }

    protected override bool PreDrawSelf(SpriteBatch spriteBatch)
    {
        Rectangle bounds = GetDimensions().ToRectangle();
        bounds.Height = (int)this.CloseButton.GetDimensions().Y - bounds.Top;
        UIHelper.PANEL.Draw(spriteBatch, bounds, UIHelper.InventoryColour);
        return false;
    }

    private void SetSummaryDisplay(string name)
    {
        Display.UpdateDisplay(name);
    }

    //Display at the top of the window, should appear like:
    // A --- B --- C --- D
    //With the summary at one end or the other (I haven't decided yet, which)
    //Will be used to select which one should be displayed
    internal class PathDisplay : UIElement
    {
        private class PathItem : UIElement
        {
            public PathItem(string name)
            {
                Name = name;
                if (name.Equals("current", StringComparison.InvariantCultureIgnoreCase))
                    name = ModContent.GetInstance<RunDataSystem>().CurrentRegion;
                if(TerrariaCells.Instance.FileExists($"Common/Assets/{Name}_icon.png"))
                    Icon = ModContent.Request<Texture2D>($"TerrariaCells/Common/Assets/{Name}_icon");
                else
                {
                    Icon = ModContent.Request<Texture2D>("Terraria/Images/UI/Bestiary/Icon_Tags_Shadow");
                    switch(name.ToLower())
                    {
                        case "forest":
                            Frame = new Rectangle(0, 0, 30, 30);
                            break;
                        case "crimson":
                            Frame = new Rectangle(12 * 30, 0, 30, 30);
                            break;
                        case "corruption":
                            Frame = new Rectangle(7 * 30, 0, 30, 30);
                            break;
                        case "dungeon":
                            Frame = new Rectangle(0, 2 * 30, 30, 30);
                            break;
                        case "desert":
                            Frame = new Rectangle(4 * 30, 0, 30, 30);
                            break;
                        case "caverns":
                            Frame = new Rectangle(2 * 30, 0, 30, 30);
                            break;
                        default:
                            Frame = new Rectangle(0, 4 * 30, 30, 30);
                            break;
                    }
                }
                    
                Tooltip = TerrariaCells.Instance.GetLocalization($"ui.runData.biome.{name}.tooltip", () => $"{name}_tooltip");
            }
            internal LocalizedText Tooltip;
            internal readonly string Name;
            internal readonly Asset<Texture2D> Icon;
            public bool Selected { get; internal set; } = false;
            public Rectangle? Frame { get; init; } = null;
            public override void LeftClick(UIMouseEvent evt)
            {
                if(Parent is not PathDisplay display)
                    return;
                
                display.SetDisplay(Name);

                foreach(PathItem item in display.PathItems)
                {
                    item.Selected = false;
                }
                this.Selected = true;
                Terraria.Audio.SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                Color drawColor = UIHelper.InventoryColour;
                if(IsMouseHovering)
                    drawColor = Color.MediumSlateBlue;
                if(Selected)
                    drawColor = Main.OurFavoriteColor;

                Rectangle bounds = GetDimensions().ToRectangle();
                UIHelper.PANEL.Draw(spriteBatch, bounds, drawColor);

                if(Icon.IsLoaded)
                {
                    Rectangle target = new Rectangle(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                    //spriteBatch.Draw(Icon.Value, target, Color.White);

                    int sizeLimit = bounds.Height - 8;
                    Vector2 size;
                    if(Frame != null) size = Frame.Value.Size();
                    else size = Icon.Size();
                    float scale = 1f;
                    if ((float)size.X > sizeLimit || (float)size.Y > sizeLimit)
                        scale = ((size.X <= size.Y) ? (sizeLimit / (float)size.Y) : (sizeLimit / (float)size.X));
                    Vector2 offset = size * 0.5f;

                    spriteBatch.Draw(Icon.Value, target.Center(), Frame, Color.White, 0f, offset, scale, SpriteEffects.None, 0f);
                }
                
                if(IsMouseHovering)
                    Terraria.ModLoader.UI.UICommon.TooltipMouseText(Tooltip.Value);
            }
        }

        internal void Reset()
        {
            Elements.Clear();
            var worldRunData = ModContent.GetInstance<RunDataSystem>();
            
            //Main.NewText(worldRunData._runSummary.Count);

            Append(new PathItem("summary"));
            foreach (string biome in worldRunData._path)
            {
                PathItem item = new PathItem(biome);

                Append(item);
            }
            Append(new PathItem("current") { Selected = true });
            
            RecalculateChildren();
        }

        private void SetDisplay(string name)
        {
            if(Parent is not RunDataWindow window)
                return;

            window.SetSummaryDisplay(name);
        }
        private IEnumerable<PathItem> PathItems => Elements.Where(e => e is PathItem).Select(e => (PathItem)e);

        public override void RecalculateChildren()
        {
            int itemCount = PathItems.Count();
            Rectangle bounds = GetDimensions().ToRectangle();
            
            int itemSize = bounds.Height - 16;

            if(itemCount > 1)
            {
                int distance = (bounds.Width-(itemCount * itemSize)) / (itemCount - 1);
                distance = Math.Min(distance, 48);

                int i = 0;
                foreach(PathItem item in PathItems)
                {
                    item.Left.Set(i * (distance + itemSize), 0);
                    item.Top.Set(-itemSize/2, 0.5f);
                    item.Width.Set(itemSize, 0);
                    item.Height.Set(itemSize, 0);
                    i++;
                }
            }
            else if(itemCount == 1)
            {
                var item = PathItems.First();
                item.Left.Set(0, 0);
                item.Top.Set(-itemSize/2, 0.5f);
                item.Width.Set(itemSize, 0);
                item.Height.Set(itemSize, 0);
            }

            base.RecalculateChildren();
        }

        protected override void DrawChildren(SpriteBatch spriteBatch)
        {
            if(PathItems.Count() > 1)
            {
                Rectangle bounds = GetDimensions().ToRectangle();
                
                //Terraria.Utils.DrawLine(spriteBatch, bounds.Left(), bounds.Right(), Color.Red, Color.Red, 6);
                
                // Vector2 start = bounds.Left();
                // Vector2 end = start;
                // foreach(var item in PathItems)
                // {
                //     end = item.GetDimensions().Center();
                //     if(item.Selected)
                //         break;
                // }
                
                
                Asset<Texture2D> magicPixel = Terraria.GameContent.TextureAssets.MagicPixel;
                if(magicPixel.IsLoaded)
                {
                    float start = bounds.Left;
                    float end = start;
                    foreach (var item in Elements)
                    {
                        var dims = item.GetDimensions();
                        if (dims.Center().X > end)
                        {
                            end = dims.X;
                        }
                        if (dims.Center().X < start || start == -1)
                        {
                            start = dims.X;
                        }
                    }

                    int width = 2;
                    spriteBatch.Draw(magicPixel.Value, new Rectangle((int)start, bounds.Y + ((bounds.Height-width)/2), (int)(end - start), width), Color.White);
                }
                //Terraria.Utils.DrawLine(spriteBatch, start, end, Color.White, Color.White*0.6f, 6);
                //Terraria.Utils.DrawLine(spriteBatch, end, bounds.Right(), Color.White*0.6f, Color.White, 6);
            }

            base.DrawChildren(spriteBatch);
        }
    }
    //Main panel of the display, should appear as:
    /*
        @ Time              : 3:00
        # Kills             : 10
        % Hits Taken        : 3
        # Damage Taken      : 35
        % Hits Dealt        : 192
        # Damage Dealt      : 1869
        $ Money Collected   : [i/s3:73][i/s70:72][i/s30:71]
        ~ Life Obtained     : [i/s4:29]
                                                            */
    //Each line can have a little tooltip description (eg, "time: time spent in this level")
    //Each line will be its own element, with an icon (left side), header (title text), and field info (time, kills, etc)
    internal class InformationDisplay : UIElement
    {
        private static class FieldTypes
        {
            public const string Time = "Time";
            public const string Kills = "Kills";
            public const string DamageDealt = "DamageDealt";
            public const string CritsDealt = "Crits";
            public const string StatusApplied = "Debuffs";
            public const string DamageTaken = "DamageTaken";
            public const string NewMoney = "Money";
            public const string NewLife = "Life";
        }
        public override void OnInitialize()
        {
            Append(new FieldDisplay(FieldTypes.Time, Terraria.GameContent.TextureAssets.Item[ItemID.GoldWatch]));
            Append(new FieldDisplay(FieldTypes.Kills, Terraria.GameContent.TextureAssets.Item[ItemID.Skull]));
            Append(new FieldDisplay(FieldTypes.DamageDealt, Terraria.GameContent.TextureAssets.Item[ItemID.IronBroadsword]));
            Append(new FieldDisplay(FieldTypes.CritsDealt, Terraria.GameContent.TextureAssets.Item[ItemID.DestroyerEmblem]));
            Append(new FieldDisplay(FieldTypes.StatusApplied, Terraria.GameContent.TextureAssets.Item[ItemID.FlaskofPoison]));
            Append(new FieldDisplay(FieldTypes.DamageTaken, Terraria.GameContent.TextureAssets.Item[ItemID.CobaltShield]));
            Append(new FieldDisplay(FieldTypes.NewMoney, Terraria.GameContent.TextureAssets.Item[ItemID.LuckyCoin]));
            Append(new FieldDisplay(FieldTypes.NewLife, Terraria.GameContent.TextureAssets.Item[ItemID.LifeCrystal]));
        }
        
        private IEnumerable<FieldDisplay> Fields => Elements.Where(e => e is FieldDisplay).Select(e => (FieldDisplay)e);
        
        string currentlyViewedRegion = "current";
        public void UpdateDisplay(string name)
        {
            if(!Elements.All(x => x is FieldDisplay))
            {
                ModContent.GetInstance<TerrariaCells>().Logger.Error("Run data information display was not set up properly");
                //Main.NewText("ERROR: Run data information display was not set up properly");
                return;
            }
            
            currentlyViewedRegion = name;
        }

        public override void RecalculateChildren()
        {
            Rectangle bounds = this.GetDimensions().ToRectangle();
            int pxHeight = Elements.Count != 0 ? Math.Min(32, bounds.Height / Elements.Count) : 32;

            for(int i = 0; i < Elements.Count; i++)
            {
                Elements[i].Left.Set(0, 0);
                Elements[i].Top.Set(i * (pxHeight+4), 0);
                Elements[i].Width.Set(0, 1);
                Elements[i].Height.Set(pxHeight, 0);
            }
            base.RecalculateChildren();
        }

        public override void Update(GameTime gameTime)
        {
            RunDataPlayer modPlayer = Main.LocalPlayer.GetModPlayer<RunDataPlayer>();
            RunDataSystem modSystem = ModContent.GetInstance<RunDataSystem>();

            RunDataSystem.RunData worldData;
            RunDataPlayer.RunData playerData;
            if (currentlyViewedRegion.Equals("current", StringComparison.InvariantCultureIgnoreCase))
            {
                worldData = modSystem.CurrentRegionData;
                playerData = modPlayer.CurrentRegionData;

                worldData.Time = RewardTrackerSystem.LevelTime - worldData.Time;
            }
            else if(currentlyViewedRegion.Equals("summary", StringComparison.InvariantCultureIgnoreCase))
            {
                worldData = modSystem.CurrentRegionData;
                playerData = modPlayer.CurrentRegionData;
                
                worldData.Time = RewardTrackerSystem.LevelTime;
                
                foreach(KeyValuePair<string, RunDataPlayer.RunData> kvp in modPlayer._runSummary)
                {
                    playerData += kvp.Value;
                }
            }
            else
            {
                if (!modSystem._runSummary.TryGetValue(currentlyViewedRegion, out worldData))
                    worldData = RunDataSystem.RunData.Default;

                if (!modPlayer._runSummary.TryGetValue(currentlyViewedRegion, out playerData))
                    playerData = RunDataPlayer.RunData.Default;
            }

            foreach (FieldDisplay field in Fields)
            {
                //Boxing because it's easier to write :/
                object o = field.Name switch
                {
                    FieldTypes.Kills => playerData.KillCount,
                    FieldTypes.Time => $"{worldData.Time.Hours:00}:{worldData.Time.Minutes:00}:{worldData.Time.Seconds:00}",
                    FieldTypes.DamageDealt => playerData.DamageDealt,
                    FieldTypes.CritsDealt => playerData.Crits,
                    FieldTypes.StatusApplied => playerData.Debuffs,
                    FieldTypes.DamageTaken => playerData.DamageTaken,
                    FieldTypes.NewMoney => 
                        (playerData.TotalMoney >= 1_00_00_00 ? $"[i/s{playerData.TotalMoney/1_00_00_00}:{ItemID.PlatinumCoin}]" : string.Empty)
                        + (playerData.TotalMoney % 1_00_00_00 >= 1_00_00 ? $"[i/s{playerData.TotalMoney % 1_00_00_00 / 1_00_00}:{ItemID.GoldCoin}]" : string.Empty)
                        + (playerData.TotalMoney % 1_00_00 >= 1_00 ? $"[i/s{playerData.TotalMoney % 1_00_00 / 1_00}:{ItemID.SilverCoin}]" : string.Empty)
                        + (playerData.TotalMoney % 1_00 >= 1 ? $"[i/s{playerData.TotalMoney % 1_00}:{ItemID.CopperCoin}]" : string.Empty),
                    FieldTypes.NewLife => playerData.LifeCrystalsFound,

                    _ => string.Empty,
                };
                field.displayInfo = o is string ? (string)o : o?.ToString() ?? string.Empty;
            }
        }

        internal class FieldDisplay : UIElement
        {
            public FieldDisplay(string name, ReLogic.Content.Asset<Texture2D> icon)
            {
                Name = name;
                Icon = icon;

                Header = TerrariaCells.Instance.GetLocalization($"ui.runData.field.{name}.label", () => name);
                Tooltip = TerrariaCells.Instance.GetLocalization($"ui.runData.field.{name}.tooltip", () => $"{name}_tooltip");
            }
            public readonly string Name;
            public readonly ReLogic.Content.Asset<Texture2D> Icon;
            internal readonly LocalizedText Header;
            internal readonly LocalizedText Tooltip;
            public string displayInfo = "Sample Text";

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                Parent.RecalculateChildren();
            
                var dimensions = GetDimensions();
                Rectangle bounds = dimensions.ToRectangle();
                Color drawColor = UIHelper.InventoryColour;

                if(IsMouseHovering)
                {
                    Terraria.ModLoader.UI.UICommon.TooltipMouseText(Tooltip.Value);
                    drawColor = Color.MediumSlateBlue;
                }

                UIHelper.PANEL.Draw(spriteBatch, bounds, drawColor);

                if(Icon.IsLoaded)
                {
                    Rectangle target = new Rectangle(bounds.Left, bounds.Top, bounds.Height, bounds.Height);
                    //spriteBatch.Draw(Icon.Value, target, Color.White);
    
                    int sizeLimit = bounds.Height - 8;
                    Vector2 size = Icon.Size();
                    float scale = 1f;
                    if ((float)size.X > sizeLimit || (float)size.Y > sizeLimit)
                        scale = ((size.X <= size.Y) ? (sizeLimit / (float)size.Y) : (sizeLimit / (float)size.X));
                    Vector2 offset = size * 0.5f;
                    
                    spriteBatch.Draw(Icon.Value,target.Center(), null, Color.White, 0f, offset, scale, SpriteEffects.None, 0f);
                }

                ReLogic.Content.Asset<ReLogic.Graphics.DynamicSpriteFont> fontAsset = Terraria.GameContent.FontAssets.MouseText;

                if(fontAsset.IsLoaded)
                {
                    const float MAX_SCALE = 1f;

                    DynamicSpriteFont font = fontAsset.Value;

                    Vector2 position = new Vector2(bounds.Left + bounds.Height + 4, bounds.Top + (bounds.Height/2));
                    Vector2 size = font.MeasureString(Header.Value);
                    Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(
                        spriteBatch,
                        fontAsset.Value,
                        Header.Value,
                        position,
                        Color.White,
                        0f,
                        new Vector2(0, size.Y*0.5f),
                        new Vector2(MathF.Min(MAX_SCALE, ((bounds.Width/2 - bounds.Height - 8)/size.X)))
                    );
                    
                    position = bounds.Center();
                    size = font.MeasureString(displayInfo);
                    Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(
                        spriteBatch,
                        fontAsset.Value,
                        displayInfo,
                        position - new Vector2(0, font.MeasureString("O").Y * 0.5f),
                        Color.White,
                        0f,
                        Vector2.Zero,
                        new Vector2(MathF.Min(MAX_SCALE, ((bounds.Width/2 - bounds.Height - 8)/size.X)))
                    );

                }
            }
        }
    }

    internal class CloseUIButton : UIElement
    {
        public CloseUIButton()
        {
            CloseText = TerrariaCells.Instance.GetLocalization($"ui.runData.close.text", () => $"Close");
        }
        private readonly LocalizedText CloseText;
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Color drawColor = UIHelper.InventoryColour;
            if(IsMouseHovering)
            {
                drawColor = Color.MediumSlateBlue;
                if (Main.mouseLeft)
                {
                    drawColor = Color.SteelBlue;
                }
            }
            
            Rectangle bounds = GetDimensions().ToRectangle();
            UIHelper.PANEL.Draw(spriteBatch, bounds, drawColor);

            Asset<DynamicSpriteFont> font = Terraria.GameContent.FontAssets.MouseText;
            if (font.IsLoaded)
            {
                ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch,
                    font.Value,
                    CloseText.Value,
                    bounds.Center() - (font.Value.MeasureString(CloseText.Value) * 0.5f),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    Vector2.One
                );
            }
        }
    
        public override void LeftClick(UIMouseEvent evt)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
            DeadCellsUISystem.ToggleActive<RunDataWindow>(false);
        }
    }
    
    internal class ExitUIButton : UIElement
    {
        public ExitUIButton()
        {
            ExitText = TerrariaCells.Instance.GetLocalization($"ui.runData.exit.text", () => $"End Run");
            ConfirmActionTooltip = TerrariaCells.Instance.GetLocalization($"ui.runData.exit.confirm", () => $"Double click to confirm");
        }
        private readonly LocalizedText ConfirmActionTooltip;
        private readonly LocalizedText ExitText;
    
        ushort clickHeadsUpTimer = 0;

        public override void Update(GameTime gameTime)
        {
            if(clickHeadsUpTimer > 0)
            {
                clickHeadsUpTimer--;
            }
        }
        
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Color drawColor = Color.Red;
            if(IsMouseHovering)
            {
                drawColor = Color.Pink;
                if (Main.mouseLeft)
                {
                    drawColor = Color.DarkRed;
                }
            }
            
            Rectangle bounds = GetDimensions().ToRectangle();
            UIHelper.PANEL.Draw(spriteBatch, bounds, drawColor);
            
            Asset<DynamicSpriteFont> font = Terraria.GameContent.FontAssets.MouseText;
            if(font.IsLoaded)
            {
                ChatManager.DrawColorCodedStringWithShadow(
                    spriteBatch,
                    font.Value,
                    ExitText.Value,
                    bounds.Center() - (font.Value.MeasureString(ExitText.Value) * 0.5f),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    Vector2.One
                );
            }
            
            if(clickHeadsUpTimer > 0)
            {
                Terraria.ModLoader.UI.UICommon.TooltipMouseText(ConfirmActionTooltip.Value);
            }
        }

        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            if(clickHeadsUpTimer == 0) return;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuClose);
            DeadCellsUISystem.ToggleActive<RunDataWindow>(false);

            var UID = Main.ActiveWorldFileData.UniqueId;
            WorldGen.SaveAndQuit(delegate
            {
                if (!Configs.DevConfig.Instance.EnableCustomWorldGen) return;
                Main.LoadWorlds();
                for (int i = 0; i < Main.WorldList.Count; i++)
                {
                    if (Main.WorldList[i].UniqueId.Equals(UID))
                        typeof(Main).GetMethod("EraseWorld", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, [i]);
                }
            });
            
            Main.LocalPlayer.GetModPlayer<RunDataPlayer>().Reset();
            ModContent.GetInstance<RunDataSystem>().Reset();
        }

        public override void LeftClick(UIMouseEvent evt)
        {
            clickHeadsUpTimer = 300;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.MenuOpen);
        }
    }
}