using System.Collections.Generic;
using NUnit.Framework;
using TimeLoop.Actors;
using TimeLoop.Grid;
using TimeLoop.Interactables;

namespace TimeLoop.Tests.EditMode
{
    /// <summary>
    /// Exercises TimeLoop.Grid.MoveResolver.Resolve directly (no GridWorld.Step, no MonoBehaviours)
    /// against hand-built GridWorld + GridActor instances, per docs/TECHNICAL_ARCHITECTURE.md
    /// section 5's simultaneous-resolution rules.
    /// </summary>
    [TestFixture]
    public class MoveResolverTests
    {
        /// <summary>All-Empty rectangular world, with optional Wall tiles at the given coordinates.</summary>
        private static GridWorld MakeOpenWorld(int width, int height, params GridCoord[] walls)
        {
            var tiles = new TileType[width, height];
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                tiles[x, y] = TileType.Empty;

            foreach (var wall in walls)
            {
                tiles[wall.X, wall.Y] = TileType.Wall;
            }

            return new GridWorld(width, height, tiles);
        }

        [Test]
        public void TwoActors_MovingIntoSameTile_BothBounceBackToStart()
        {
            var world = MakeOpenWorld(3, 3);
            var actorA = new GridActor("A", new GridCoord(0, 1));
            var actorB = new GridActor("B", new GridCoord(2, 1));

            // A moves Right: (0,1) -> (1,1). B moves Left: (2,1) -> (1,1). Same destination tile
            // contested by two actors => MoveResolver's "contested tile" rule bounces both back.
            var inputs = new List<ActorTickInput>
            {
                new ActorTickInput(actorA, new InputCommand(Direction.Right, false)),
                new ActorTickInput(actorB, new InputCommand(Direction.Left, false)),
            };

            var resolution = MoveResolver.Resolve(world, inputs);

            Assert.AreEqual(new GridCoord(0, 1), resolution.ActorDestinations[actorA]);
            Assert.AreEqual(new GridCoord(2, 1), resolution.ActorDestinations[actorB]);
        }

        [Test]
        public void Actor_MovingIntoWall_StaysPut()
        {
            var wall = new GridCoord(1, 1);
            var world = MakeOpenWorld(3, 3, wall);
            var actor = new GridActor("A", new GridCoord(0, 1));

            var inputs = new List<ActorTickInput>
            {
                new ActorTickInput(actor, new InputCommand(Direction.Right, false)),
            };

            var resolution = MoveResolver.Resolve(world, inputs);

            Assert.AreEqual(new GridCoord(0, 1), resolution.ActorDestinations[actor],
                "An actor whose only requested destination is a Wall tile must not move.");
        }

        [Test]
        public void Actor_PushingBoxIntoFreeSpace_Succeeds_AndBoxPositionUpdatesViaGetBoxAt()
        {
            var world = MakeOpenWorld(4, 3);
            var actor = new GridActor("A", new GridCoord(0, 1));
            var box = new MovableBox("box_1", new GridCoord(1, 1));
            world.RegisterEntity(box);

            // Actor moves Right into the box's tile (1,1); the box has free space one further at
            // (2,1), so per MoveResolver pass 1 the push should be accepted: actor ends up at
            // (1,1), and BoxPushes records the box moving to (2,1).
            var inputs = new List<ActorTickInput>
            {
                new ActorTickInput(actor, new InputCommand(Direction.Right, false)),
            };

            var resolution = MoveResolver.Resolve(world, inputs);

            Assert.AreEqual(new GridCoord(1, 1), resolution.ActorDestinations[actor]);
            Assert.AreEqual(1, resolution.BoxPushes.Count);
            Assert.AreSame(box, resolution.BoxPushes[0].Box);
            Assert.AreEqual(new GridCoord(2, 1), resolution.BoxPushes[0].NewPosition);

            // MoveResolver.Resolve only computes the resolution; GridWorld.Step is what actually
            // applies BoxPushes via MoveEntityTo. Do that same step here so we can assert the
            // box's position is observable through world.GetBoxAt afterward, as the task requires.
            world.MoveEntityTo(box, resolution.BoxPushes[0].NewPosition);

            Assert.AreSame(box, world.GetBoxAt(new GridCoord(2, 1)));
            Assert.IsNull(world.GetBoxAt(new GridCoord(1, 1)));
        }

        [Test]
        public void BoxPush_IntoWall_IsRejected_ActorAndBoxBothStayPut()
        {
            // Wall sits directly behind the box, so the box has nowhere to go.
            var wallBehindBox = new GridCoord(2, 1);
            var world = MakeOpenWorld(4, 3, wallBehindBox);
            var actor = new GridActor("A", new GridCoord(0, 1));
            var box = new MovableBox("box_1", new GridCoord(1, 1));
            world.RegisterEntity(box);

            var inputs = new List<ActorTickInput>
            {
                new ActorTickInput(actor, new InputCommand(Direction.Right, false)),
            };

            var resolution = MoveResolver.Resolve(world, inputs);

            Assert.AreEqual(new GridCoord(0, 1), resolution.ActorDestinations[actor]);
            Assert.AreEqual(0, resolution.BoxPushes.Count);
            Assert.AreSame(box, world.GetBoxAt(new GridCoord(1, 1)), "The box must not move if its push destination is blocked.");
        }

        [Test]
        public void BoxPush_IntoAnotherBox_IsRejected_ActorAndBothBoxesStayPut()
        {
            var world = MakeOpenWorld(4, 3);
            var actor = new GridActor("A", new GridCoord(0, 1));
            var box1 = new MovableBox("box_1", new GridCoord(1, 1));
            var box2 = new MovableBox("box_2", new GridCoord(2, 1));
            world.RegisterEntity(box1);
            world.RegisterEntity(box2);

            var inputs = new List<ActorTickInput>
            {
                new ActorTickInput(actor, new InputCommand(Direction.Right, false)),
            };

            var resolution = MoveResolver.Resolve(world, inputs);

            Assert.AreEqual(new GridCoord(0, 1), resolution.ActorDestinations[actor]);
            Assert.AreEqual(0, resolution.BoxPushes.Count);
            Assert.AreSame(box1, world.GetBoxAt(new GridCoord(1, 1)));
            Assert.AreSame(box2, world.GetBoxAt(new GridCoord(2, 1)));
        }

        [Test]
        public void Actor_CannotMoveIntoTile_OccupiedByStationaryNonVacatingActor()
        {
            var world = MakeOpenWorld(3, 3);
            var mover = new GridActor("mover", new GridCoord(0, 1));
            var stationary = new GridActor("stationary", new GridCoord(1, 1));

            // The stationary actor issues Idle (does not vacate its tile), so per MoveResolver
            // pass 2's actor-vs-actor rule, the mover must bounce back to its start.
            var inputs = new List<ActorTickInput>
            {
                new ActorTickInput(mover, new InputCommand(Direction.Right, false)),
                new ActorTickInput(stationary, InputCommand.Idle),
            };

            var resolution = MoveResolver.Resolve(world, inputs);

            Assert.AreEqual(new GridCoord(0, 1), resolution.ActorDestinations[mover]);
            Assert.AreEqual(new GridCoord(1, 1), resolution.ActorDestinations[stationary]);
        }

        [Test]
        public void Actor_CanMoveIntoTile_VacatedByAnotherActorThisSameTick()
        {
            var world = MakeOpenWorld(3, 3);
            var mover = new GridActor("mover", new GridCoord(0, 1));
            var vacator = new GridActor("vacator", new GridCoord(1, 1));

            // The vacator itself moves away (Right, to (2,1)) on the same tick, so the mover
            // should be allowed to take (1,1) rather than bounce.
            var inputs = new List<ActorTickInput>
            {
                new ActorTickInput(mover, new InputCommand(Direction.Right, false)),
                new ActorTickInput(vacator, new InputCommand(Direction.Right, false)),
            };

            var resolution = MoveResolver.Resolve(world, inputs);

            Assert.AreEqual(new GridCoord(1, 1), resolution.ActorDestinations[mover]);
            Assert.AreEqual(new GridCoord(2, 1), resolution.ActorDestinations[vacator]);
        }
    }
}
