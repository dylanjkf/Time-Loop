using System.Collections.Generic;
using TimeLoop.Grid;

namespace TimeLoop.Interactables
{
    /// <summary>
    /// Patrols a fixed list of waypoints in a ping-pong pattern, advancing one waypoint every
    /// <see cref="_ticksPerStep"/> ticks. Position is a pure function of the world tick number —
    /// never real time or accumulated velocity — so every ghost and every loop sees the platform
    /// in exactly the same place on the same tick, always (docs/TECHNICAL_ARCHITECTURE.md section
    /// 2). GridWorld.Step carries any actor that ends its own move standing on the platform's
    /// pre-tick tile.
    /// </summary>
    public sealed class MovingPlatform : GridEntityBase, IWorldTickable
    {
        private readonly IReadOnlyList<GridCoord> _waypoints;
        private readonly int _ticksPerStep;

        public MovingPlatform(string id, IReadOnlyList<GridCoord> waypoints, int ticksPerStep)
            : base(id, waypoints[0])
        {
            _waypoints = waypoints;
            _ticksPerStep = System.Math.Max(1, ticksPerStep);
        }

        public void OnWorldTick(int tickNumber, GridWorld world)
        {
            SetPosition(WaypointAt(tickNumber));
        }

        private GridCoord WaypointAt(int tickNumber)
        {
            if (_waypoints.Count == 1) return _waypoints[0];

            var period = (_waypoints.Count - 1) * 2;
            var stepIndex = (tickNumber / _ticksPerStep) % period;
            var pingPongIndex = stepIndex < _waypoints.Count ? stepIndex : period - stepIndex;
            return _waypoints[pingPongIndex];
        }
    }
}
