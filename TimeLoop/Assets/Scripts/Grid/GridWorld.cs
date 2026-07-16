using System;
using System.Collections.Generic;
using System.Linq;
using TimeLoop.Actors;
using TimeLoop.Interactables;

namespace TimeLoop.Grid
{
    public enum TileType
    {
        Empty,
        Wall
    }

    /// <summary>
    /// The mutable simulation state for a single level attempt: static tile geometry plus every
    /// interactable entity currently on the grid. Actors (player/ghosts) are NOT stored here —
    /// see IGridEntity's doc comment. Every level attempt within one loop shares exactly one
    /// GridWorld instance; TimeLoopManager calls <see cref="ResetToInitial"/> between loops.
    /// </summary>
    public sealed class GridWorld
    {
        public int Width { get; }
        public int Height { get; }

        private readonly TileType[,] _tiles;
        private readonly Dictionary<string, IGridEntity> _entitiesById = new Dictionary<string, IGridEntity>();
        private readonly List<IGridEntity> _entities = new List<IGridEntity>();

        public GridWorld(int width, int height, TileType[,] tiles)
        {
            Width = width;
            Height = height;
            _tiles = tiles;
        }

        public bool IsInBounds(GridCoord c) => c.X >= 0 && c.X < Width && c.Y >= 0 && c.Y < Height;

        /// <summary>True for out-of-bounds or a static Wall tile. Does not consider entities.</summary>
        public bool IsWallTile(GridCoord c) => !IsInBounds(c) || _tiles[c.X, c.Y] == TileType.Wall;

        public void RegisterEntity(IGridEntity entity)
        {
            _entitiesById[entity.Id] = entity;
            _entities.Add(entity);
        }

        public void UnregisterEntity(string id)
        {
            if (_entitiesById.TryGetValue(id, out var entity))
            {
                _entitiesById.Remove(id);
                _entities.Remove(entity);
            }
        }

        public IReadOnlyList<IGridEntity> AllEntities => _entities;

        public T GetEntity<T>(string id) where T : class, IGridEntity
            => _entitiesById.TryGetValue(id, out var e) ? e as T : null;

        public IEnumerable<IGridEntity> EntitiesAt(GridCoord c) => _entities.Where(e => e.Position == c);

        public MovableBox GetBoxAt(GridCoord c) => _entities.OfType<MovableBox>().FirstOrDefault(b => b.Position == c);

        /// <summary>
        /// True if a non-box, solid entity (e.g. a closed Door) occupies this tile. MovableBox is
        /// deliberately excluded even though it's IsSolid — a box tile is only blocked if it can't
        /// be pushed, which MoveResolver checks separately via GetBoxAt. Do not remove this
        /// exclusion: without it, IsBlockedIgnoringBoxes returns true for every box tile and
        /// MoveResolver's push-feasibility branch is never reached, silently disabling box pushing.
        /// </summary>
        public bool IsSolidEntityAt(GridCoord c) => _entities.Any(e => e.Position == c && e.IsSolid && !(e is MovableBox));

        /// <summary>
        /// Movement legality check used before attempting a box push — walls and closed doors block,
        /// boxes are handled separately by the caller (MoveResolver), since a box tile is only
        /// blocked if it *can't* be pushed.
        /// </summary>
        public bool IsBlockedIgnoringBoxes(GridCoord c) => IsWallTile(c) || IsSolidEntityAt(c);

        public void MoveEntityTo(IGridEntity entity, GridCoord newPosition) => entity.SetPosition(newPosition);

        /// <summary>Advances every IWorldTickable entity (moving platforms) by one tick.</summary>
        public void TickWorldEntities(int tickNumber)
        {
            foreach (var tickable in _entities.OfType<IWorldTickable>().ToList())
            {
                tickable.OnWorldTick(tickNumber, this);
            }
        }

        /// <summary>Resets every entity to its level-authored initial state. Called at the start of every loop.</summary>
        public void ResetToInitial()
        {
            foreach (var entity in _entities)
            {
                entity.ResetToInitial();
            }
        }

        /// <summary>
        /// Advances the whole world by exactly one tick: resolves simultaneous actor movement
        /// (MoveResolver), applies box pushes, advances world-tickables (moving platforms) and
        /// carries any actor riding one, then dispatches OnExit/OnEnter/OnInteract in the same
        /// fixed order used for movement (ghosts oldest-to-newest, then the live player).
        /// This is the single entry point TimeLoopManager calls every tick — see
        /// docs/TECHNICAL_ARCHITECTURE.md sections 4.3 and 5.
        /// </summary>
        public TickResult Step(IReadOnlyList<ActorTickInput> orderedInputs, int tickNumber)
        {
            var previousPositions = new Dictionary<GridActor, GridCoord>();
            foreach (var input in orderedInputs)
            {
                previousPositions[input.Actor] = input.Actor.Position;
            }

            var resolution = MoveResolver.Resolve(this, orderedInputs);

            var platforms = _entities.OfType<Interactables.MovingPlatform>().ToList();
            var platformPreTickPositions = platforms.ToDictionary(p => (IGridEntity)p, p => p.Position);

            foreach (var input in orderedInputs)
            {
                input.Actor.SetPosition(resolution.ActorDestinations[input.Actor]);
            }

            foreach (var push in resolution.BoxPushes)
            {
                MoveEntityTo(push.Box, push.NewPosition);
            }

            TickWorldEntities(tickNumber);

            foreach (var platform in platforms)
            {
                var oldPos = platformPreTickPositions[platform];
                var newPos = platform.Position;
                if (newPos == oldPos) continue;

                foreach (var input in orderedInputs)
                {
                    if (input.Actor.Position == oldPos)
                    {
                        input.Actor.SetPosition(newPos);
                    }
                }
            }

            var events = new List<InteractionEvent>();
            foreach (var input in orderedInputs)
            {
                var actor = input.Actor;
                var oldPos = previousPositions[actor];
                var newPos = actor.Position;

                if (oldPos != newPos)
                {
                    foreach (var e in EntitiesAt(oldPos).OfType<Interactables.IInteractable>().ToList())
                    {
                        e.OnExit(actor);
                    }

                    foreach (var e in EntitiesAt(newPos).OfType<Interactables.IInteractable>().ToList())
                    {
                        e.OnEnter(actor);
                    }
                }

                if (input.Command.Interact)
                {
                    foreach (var e in EntitiesAt(newPos).OfType<Interactables.IInteractable>().ToList())
                    {
                        e.OnInteract(actor);
                        events.Add(new InteractionEvent(actor, e));
                    }
                }
            }

            return new TickResult(resolution, events);
        }
    }

    public readonly struct InteractionEvent
    {
        public readonly GridActor Actor;
        public readonly Interactables.IInteractable Entity;

        public InteractionEvent(GridActor actor, Interactables.IInteractable entity)
        {
            Actor = actor;
            Entity = entity;
        }
    }

    public sealed class TickResult
    {
        public readonly MoveResolution Resolution;
        public readonly IReadOnlyList<InteractionEvent> Interactions;

        public TickResult(MoveResolution resolution, IReadOnlyList<InteractionEvent> interactions)
        {
            Resolution = resolution;
            Interactions = interactions;
        }
    }
}
