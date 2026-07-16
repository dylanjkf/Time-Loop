using System.Collections.Generic;
using NUnit.Framework;
using TimeLoop.Actors;
using TimeLoop.Grid;
using TimeLoop.Interactables;
using TimeLoop.Levels;

namespace TimeLoop.Tests.EditMode
{
    [TestFixture]
    public class LevelParserTests
    {
        private const string HeaderFieldsSource =
@"NAME: Test Level
WORLD: 2
LOOP_SECONDS: 12.5
MAX_TIMELINES: 3
PAR_LOOPS: 4
PAR_TICKS: 50
GRID:
#G#
#.#
#S#
LEGEND:
S=spawn G=goal
";

        [Test]
        public void Parse_HeaderFields_MatchSourceValues()
        {
            var def = LevelParser.Parse("header-test", HeaderFieldsSource);

            Assert.AreEqual(2, def.World);
            Assert.AreEqual(12.5f, def.LoopSeconds);
            Assert.AreEqual(3, def.MaxTimelines);
            Assert.AreEqual(4, def.ParLoops);
            Assert.AreEqual(50, def.ParTicks);
            Assert.AreEqual("Test Level", def.Name);
        }

        [Test]
        public void Parse_SpawnPosition_MatchesSTile()
        {
            var def = LevelParser.Parse("header-test", HeaderFieldsSource);

            // GRID rows are stored top-to-bottom in the source but row 0 of the *text* maps to the
            // *highest* Y (LevelParser: y = gridRows.Count - 1 - rowIndex). Rows here are
            // ["#G#", "#.#", "#S#"] (height 3), so the "S" on the last text row (rowIndex 2) sits
            // at y = 3 - 1 - 2 = 0, at column x = 1 -> spawn should be (1, 0).
            Assert.AreEqual(new GridCoord(1, 0), def.SpawnPosition);
            Assert.AreEqual(3, def.Width);
            Assert.AreEqual(3, def.Height);
        }

        [Test]
        public void BuildWorld_DoorOpensOnceReferencedSwitchGroupActivates()
        {
            const string source =
@"NAME: Switch Door Test
WORLD: 1
GRID:
#D#
#.#
#1#
#S#
LEGEND:
S=spawn 1=switch:a D=door:and:a
";
            var def = LevelParser.Parse("switch-door-test", source);
            var world = def.BuildWorld();

            // D is on the last-parsed grid row at rowIndex 0 -> y = 4 - 1 - 0 = 3, x = 1, so
            // LevelParser's door id ("door_" + x + "_" + y) is "door_1_3". The switch's group id is
            // "a" so its entity id ("switch_" + groupId) is "switch_a".
            var door = world.GetEntity<Door>("door_1_3");
            var sw = world.GetEntity<Switch>("switch_a");

            Assert.IsNotNull(door, "BuildWorld should have registered a Door with id 'door_1_3'.");
            Assert.IsNotNull(sw, "BuildWorld should have registered a Switch with id 'switch_a'.");
            Assert.IsFalse(door.IsOpen, "Door must start closed: its single AND-gated switch starts unactivated.");

            var actor = new GridActor("player", sw.Position);
            var inputs = new List<ActorTickInput>
            {
                new ActorTickInput(actor, new InputCommand(Direction.None, true)),
            };

            world.Step(inputs, 0);

            Assert.IsTrue(sw.IsActivated, "Standing on the switch's tile and issuing Interact should toggle it on.");
            Assert.IsTrue(door.IsOpen, "Door must open once its referenced switch group activates.");
        }

        [Test]
        public void BuildWinCondition_RequiredGoalIdCount_MatchesNumberOfGTilesInSource()
        {
            const string source =
@"NAME: Two Targets
WORLD: 1
GRID:
G.G
...
.S.
LEGEND:
S=spawn G=goal
";
            var def = LevelParser.Parse("multi-goal-test", source);
            var winCondition = def.BuildWinCondition();

            // The GRID block's top row is "G.G" -> exactly two 'G' tiles (x=0 and x=2 on that row).
            Assert.AreEqual(2, winCondition.RequiredGoalIds.Count);
        }

        [Test]
        public void Parse_MalformedOrMissingLegendEntries_DoNotThrow_UnknownSymbolsBecomeFloor()
        {
            // 'X' is a grid symbol with no LEGEND entry at all. "ZZZ" is a legend entry with no '='
            // separator (malformed). "Q=" is a legend entry mapping to an empty token. None of
            // these should throw; unmapped/unknown grid symbols must simply become plain floor.
            const string source =
@"NAME: Malformed Legend Test
WORLD: 1
GRID:
#XS#
LEGEND:
S=spawn ZZZ Q=
";
            LevelDefinition def = null;
            Assert.DoesNotThrow(() => def = LevelParser.Parse("malformed-test", source));

            GridWorld world = null;
            Assert.DoesNotThrow(() => world = def.BuildWorld());

            // Row "#XS#" at the sole row (y=0): x=0 '#' wall, x=1 'X' unknown -> floor, x=2 'S' spawn, x=3 '#' wall.
            Assert.IsFalse(world.IsWallTile(new GridCoord(1, 0)), "Unknown symbol 'X' must resolve to plain floor, not a wall.");
            Assert.AreEqual(new GridCoord(2, 0), def.SpawnPosition);
        }
    }
}
