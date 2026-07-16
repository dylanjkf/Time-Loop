using NUnit.Framework;
using TimeLoop.Actors;
using TimeLoop.Grid;
using TimeLoop.Interactables;

namespace TimeLoop.Tests.EditMode
{
    [TestFixture]
    public class GridWorldTests
    {
        /// <summary>3x3 grid, all Empty except a single Wall tile at (1,1).</summary>
        private static GridWorld MakeWorld()
        {
            var tiles = new TileType[3, 3];
            for (var x = 0; x < 3; x++)
            for (var y = 0; y < 3; y++)
                tiles[x, y] = TileType.Empty;

            tiles[1, 1] = TileType.Wall;

            return new GridWorld(3, 3, tiles);
        }

        [Test]
        public void IsWallTile_OutOfBounds_ReturnsTrue()
        {
            var world = MakeWorld();

            Assert.IsTrue(world.IsWallTile(new GridCoord(-1, 0)));
            Assert.IsTrue(world.IsWallTile(new GridCoord(3, 0)));
            Assert.IsTrue(world.IsWallTile(new GridCoord(0, -1)));
            Assert.IsTrue(world.IsWallTile(new GridCoord(0, 3)));
        }

        [Test]
        public void IsWallTile_OnWallTile_ReturnsTrue()
        {
            var world = MakeWorld();

            Assert.IsTrue(world.IsWallTile(new GridCoord(1, 1)));
        }

        [Test]
        public void IsWallTile_OnEmptyTile_ReturnsFalse()
        {
            var world = MakeWorld();

            Assert.IsFalse(world.IsWallTile(new GridCoord(0, 0)));
            Assert.IsFalse(world.IsWallTile(new GridCoord(2, 2)));
        }

        [Test]
        public void RegisterEntity_GetEntity_RoundTrips()
        {
            var world = MakeWorld();
            var box = new MovableBox("box_1", new GridCoord(0, 0));

            world.RegisterEntity(box);
            var retrieved = world.GetEntity<MovableBox>("box_1");

            Assert.AreSame(box, retrieved);
        }

        [Test]
        public void GetEntity_UnknownId_ReturnsNull()
        {
            var world = MakeWorld();
            var box = new MovableBox("box_1", new GridCoord(0, 0));
            world.RegisterEntity(box);

            Assert.IsNull(world.GetEntity<MovableBox>("does_not_exist"));
        }

        [Test]
        public void UnregisterEntity_RemovesItFromLookup()
        {
            var world = MakeWorld();
            var box = new MovableBox("box_1", new GridCoord(0, 0));
            world.RegisterEntity(box);

            world.UnregisterEntity("box_1");

            Assert.IsNull(world.GetEntity<MovableBox>("box_1"));
            Assert.IsFalse(world.AllEntities.Contains(box));
        }

        [Test]
        public void Door_WithSwitchSource_OpensAndClosesAsSwitchToggles()
        {
            var world = MakeWorld();
            var sw = new Switch("switch_a", new GridCoord(0, 0));
            // Door(id, position, sources, logic) subscribes to each source's OnActivationChanged
            // in its constructor and calls Recompute() immediately, so IsOpen reflects the
            // switch's starting state (unactivated) right away.
            var door = new Door("door_1", new GridCoord(2, 2), new[] { sw }, Door.LogicMode.And);

            world.RegisterEntity(sw);
            world.RegisterEntity(door);

            Assert.IsFalse(door.IsOpen, "Door should start closed because its single AND-gated switch starts unactivated.");
            Assert.IsTrue(door.IsSolid, "IsSolid is defined as !IsOpen, so a closed door must be solid.");

            var actor = new GridActor("player", new GridCoord(0, 0));

            sw.OnInteract(actor);
            Assert.IsTrue(sw.IsActivated);
            Assert.IsTrue(door.IsOpen, "Door must open the moment its only source switch activates (AND over one source).");
            Assert.IsFalse(door.IsSolid);

            sw.OnInteract(actor);
            Assert.IsFalse(sw.IsActivated);
            Assert.IsFalse(door.IsOpen, "Door must close again once its source switch deactivates.");
            Assert.IsTrue(door.IsSolid);
        }

        [Test]
        public void Door_WithNoSources_IsAlwaysOpen()
        {
            var world = MakeWorld();
            var door = new Door("door_open", new GridCoord(2, 2), new System.Collections.Generic.List<IActivatable>(), Door.LogicMode.And);
            world.RegisterEntity(door);

            Assert.IsTrue(door.IsOpen, "A door with zero gating sources is defined to always be open.");
            Assert.IsFalse(door.IsSolid);
        }
    }
}
