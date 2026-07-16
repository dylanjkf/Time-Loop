using System;
using System.Collections.Generic;
using System.Linq;
using TimeLoop.Grid;
using TimeLoop.Interactables;

namespace TimeLoop.Levels
{
    /// <summary>
    /// Parses the shared .level text format (docs/TECHNICAL_ARCHITECTURE.md section 6) into a
    /// GridWorld. Both hand-authored campaign levels (Resources/Levels/WorldN/*.level) and
    /// ProceduralGenerator's output go through this exact same parser, guaranteeing generated and
    /// handcrafted levels behave identically.
    ///
    /// Legend token vocabulary (right-hand side of "X=..." in the LEGEND: block):
    ///   spawn                              — player/ghost start tile
    ///   switch:&lt;groupId&gt;             — permanent toggle, gated by Interact
    ///   plate:&lt;groupId&gt;              — auto-toggles from occupancy
    ///   door:&lt;and|or&gt;:&lt;g1+g2&gt;  — solid until its switch/plate group condition is met
    ///   box                                — pushable crate
    ///   hazard                             — resets the current loop on player contact
    ///   goal                               — required tile for level completion (levels may have several)
    ///   platform:&lt;groupId&gt;:&lt;order&gt;:&lt;ticksPerStep&gt; — one waypoint of a patrol group
    ///   teleport:&lt;padId&gt;:&lt;linkedPadId&gt;           — paired instant relocation
    /// Any grid character not '#' and not present in LEGEND is treated as plain floor.
    /// </summary>
    public static class LevelParser
    {
        private enum ParseMode
        {
            Header,
            Grid,
            Legend
        }

        private sealed class ParseContext
        {
            public GridCoord Spawn;
            public readonly List<IGridEntity> SimpleEntities = new List<IGridEntity>();
            public readonly Dictionary<string, IActivatable> ActivatablesByGroup = new Dictionary<string, IActivatable>();
            public readonly List<DoorSpec> DoorSpecs = new List<DoorSpec>();
            public readonly Dictionary<string, List<PlatformWaypointSpec>> PlatformGroups = new Dictionary<string, List<PlatformWaypointSpec>>();
            public readonly Dictionary<string, TeleportPad> TeleportPadsById = new Dictionary<string, TeleportPad>();
        }

        private readonly struct DoorSpec
        {
            public readonly string Id;
            public readonly GridCoord Position;
            public readonly Door.LogicMode Mode;
            public readonly List<string> GroupIds;

            public DoorSpec(string id, GridCoord position, Door.LogicMode mode, List<string> groupIds)
            {
                Id = id;
                Position = position;
                Mode = mode;
                GroupIds = groupIds;
            }
        }

        private readonly struct PlatformWaypointSpec
        {
            public readonly int Order;
            public readonly GridCoord Position;
            public readonly int TicksPerStep;

            public PlatformWaypointSpec(int order, GridCoord position, int ticksPerStep)
            {
                Order = order;
                Position = position;
                TicksPerStep = ticksPerStep;
            }
        }

        private sealed class ParsedLevel
        {
            public Dictionary<string, string> Header;
            public GridCoord Spawn;
            public List<string> GoalIds;
            public int Width;
            public int Height;
        }

        public static LevelDefinition Parse(string levelId, string source)
        {
            var parsed = ParseHeaderAndLayout(source);
            return new LevelDefinition(levelId, source, parsed.Header, parsed.Spawn, parsed.GoalIds, parsed.Width, parsed.Height);
        }

        /// <summary>Rebuilds a fresh, independent GridWorld from the level's stored source text. Called every level (re)load.</summary>
        internal static GridWorld BuildWorld(string source)
        {
            var lines = SplitLines(source);
            var gridRows = ExtractGridRows(lines);
            var legend = ExtractLegend(lines);

            var width = gridRows.Count == 0 ? 0 : gridRows.Max(r => r.Length);
            var height = gridRows.Count;
            var tiles = new TileType[Math.Max(width, 1), Math.Max(height, 1)];

            for (var x = 0; x < tiles.GetLength(0); x++)
            for (var y = 0; y < tiles.GetLength(1); y++)
                tiles[x, y] = TileType.Wall;

            var ctx = new ParseContext();

            for (var rowIndex = 0; rowIndex < gridRows.Count; rowIndex++)
            {
                var row = gridRows[rowIndex];
                var y = gridRows.Count - 1 - rowIndex;

                for (var x = 0; x < row.Length; x++)
                {
                    var ch = row[x];
                    var coord = new GridCoord(x, y);

                    if (ch == '#')
                    {
                        tiles[x, y] = TileType.Wall;
                        continue;
                    }

                    tiles[x, y] = TileType.Empty;

                    if (ch == '.' || ch == ' ') continue;
                    if (!legend.TryGetValue(ch, out var token)) continue;

                    ApplyToken(token, coord, ctx);
                }
            }

            var world = new GridWorld(tiles.GetLength(0), tiles.GetLength(1), tiles);

            foreach (var entity in ctx.SimpleEntities)
            {
                world.RegisterEntity(entity);
            }

            foreach (var spec in ctx.DoorSpecs)
            {
                var sources = spec.GroupIds
                    .Where(id => ctx.ActivatablesByGroup.ContainsKey(id))
                    .Select(id => ctx.ActivatablesByGroup[id])
                    .ToList();

                world.RegisterEntity(new Door(spec.Id, spec.Position, sources, spec.Mode));
            }

            foreach (var kvp in ctx.PlatformGroups)
            {
                var ordered = kvp.Value.OrderBy(w => w.Order).ToList();
                var waypoints = ordered.Select(w => w.Position).ToList();
                var ticksPerStep = ordered.Count > 0 ? ordered[0].TicksPerStep : 6;
                world.RegisterEntity(new MovingPlatform("platform_" + kvp.Key, waypoints, ticksPerStep));
            }

            foreach (var pad in ctx.TeleportPadsById.Values)
            {
                if (ctx.TeleportPadsById.TryGetValue(pad.LinkedPadId, out var partner))
                {
                    pad.LinkTo(partner);
                }

                world.RegisterEntity(pad);
            }

            return world;
        }

        private static ParsedLevel ParseHeaderAndLayout(string source)
        {
            var lines = SplitLines(source);
            var header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var mode = ParseMode.Header;
            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.Trim();

                if (mode == ParseMode.Header)
                {
                    if (trimmed.Length == 0 || trimmed.StartsWith("#")) continue;
                    if (string.Equals(trimmed, "GRID:", StringComparison.OrdinalIgnoreCase))
                    {
                        mode = ParseMode.Grid;
                        continue;
                    }

                    var colonIndex = trimmed.IndexOf(':');
                    if (colonIndex < 0) continue;
                    header[trimmed.Substring(0, colonIndex).Trim()] = trimmed.Substring(colonIndex + 1).Trim();
                }
                else if (mode == ParseMode.Grid)
                {
                    if (string.Equals(trimmed, "LEGEND:", StringComparison.OrdinalIgnoreCase))
                    {
                        mode = ParseMode.Legend;
                    }
                }
            }

            var gridRows = ExtractGridRows(lines);
            var legend = ExtractLegend(lines);
            var width = gridRows.Count == 0 ? 0 : gridRows.Max(r => r.Length);
            var height = gridRows.Count;

            var spawn = GridCoord.Zero;
            var goalIds = new List<string>();

            for (var rowIndex = 0; rowIndex < gridRows.Count; rowIndex++)
            {
                var row = gridRows[rowIndex];
                var y = gridRows.Count - 1 - rowIndex;

                for (var x = 0; x < row.Length; x++)
                {
                    var ch = row[x];
                    if (!legend.TryGetValue(ch, out var token)) continue;

                    var parts = token.Split(':');
                    if (parts[0] == "spawn")
                    {
                        spawn = new GridCoord(x, y);
                    }
                    else if (parts[0] == "goal")
                    {
                        goalIds.Add($"goal_{x}_{y}");
                    }
                }
            }

            return new ParsedLevel { Header = header, Spawn = spawn, GoalIds = goalIds, Width = width, Height = height };
        }

        private static void ApplyToken(string token, GridCoord coord, ParseContext ctx)
        {
            var parts = token.Split(':');
            var type = parts[0];

            switch (type)
            {
                case "spawn":
                    ctx.Spawn = coord;
                    break;

                case "switch":
                {
                    var groupId = parts[1];
                    var sw = new Switch("switch_" + groupId, coord);
                    ctx.ActivatablesByGroup[groupId] = sw;
                    ctx.SimpleEntities.Add(sw);
                    break;
                }

                case "plate":
                {
                    var groupId = parts[1];
                    var plate = new PressurePlate("plate_" + groupId, coord);
                    ctx.ActivatablesByGroup[groupId] = plate;
                    ctx.SimpleEntities.Add(plate);
                    break;
                }

                case "door":
                {
                    var mode = parts[1] == "or" ? Door.LogicMode.Or : Door.LogicMode.And;
                    var groupIds = parts[2].Split('+').ToList();
                    var id = "door_" + coord.X + "_" + coord.Y;
                    ctx.DoorSpecs.Add(new DoorSpec(id, coord, mode, groupIds));
                    break;
                }

                case "box":
                    ctx.SimpleEntities.Add(new MovableBox($"box_{coord.X}_{coord.Y}", coord));
                    break;

                case "hazard":
                    ctx.SimpleEntities.Add(new Hazard($"hazard_{coord.X}_{coord.Y}", coord));
                    break;

                case "goal":
                {
                    var id = $"goal_{coord.X}_{coord.Y}";
                    ctx.SimpleEntities.Add(new Goal(id, coord));
                    break;
                }

                case "platform":
                {
                    var groupId = parts[1];
                    var order = int.Parse(parts[2]);
                    var ticksPerStep = parts.Length > 3 ? int.Parse(parts[3]) : 6;

                    if (!ctx.PlatformGroups.TryGetValue(groupId, out var list))
                    {
                        list = new List<PlatformWaypointSpec>();
                        ctx.PlatformGroups[groupId] = list;
                    }

                    list.Add(new PlatformWaypointSpec(order, coord, ticksPerStep));
                    break;
                }

                case "teleport":
                {
                    var padId = parts[1];
                    var linkedPadId = parts[2];
                    var pad = new TeleportPad("teleport_" + padId, coord, linkedPadId);
                    ctx.TeleportPadsById[padId] = pad;
                    break;
                }
            }
        }

        private static string[] SplitLines(string source) => source.Replace("\r\n", "\n").Split('\n');

        private static List<string> ExtractGridRows(string[] lines)
        {
            var rows = new List<string>();
            var mode = ParseMode.Header;

            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.Trim();

                if (mode == ParseMode.Header)
                {
                    if (string.Equals(trimmed, "GRID:", StringComparison.OrdinalIgnoreCase))
                    {
                        mode = ParseMode.Grid;
                    }
                }
                else if (mode == ParseMode.Grid)
                {
                    if (string.Equals(trimmed, "LEGEND:", StringComparison.OrdinalIgnoreCase))
                    {
                        mode = ParseMode.Legend;
                        continue;
                    }

                    rows.Add(rawLine.TrimEnd('\r'));
                }
            }

            return rows;
        }

        private static Dictionary<char, string> ExtractLegend(string[] lines)
        {
            var legend = new Dictionary<char, string>();
            var mode = ParseMode.Header;

            foreach (var rawLine in lines)
            {
                var trimmed = rawLine.Trim();

                if (mode == ParseMode.Header)
                {
                    if (string.Equals(trimmed, "GRID:", StringComparison.OrdinalIgnoreCase)) mode = ParseMode.Grid;
                }
                else if (mode == ParseMode.Grid)
                {
                    if (string.Equals(trimmed, "LEGEND:", StringComparison.OrdinalIgnoreCase)) mode = ParseMode.Legend;
                }
                else
                {
                    if (trimmed.Length == 0) continue;

                    foreach (var entry in trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var eqIndex = entry.IndexOf('=');
                        if (eqIndex <= 0) continue;
                        legend[entry[0]] = entry.Substring(eqIndex + 1);
                    }
                }
            }

            return legend;
        }
    }
}
