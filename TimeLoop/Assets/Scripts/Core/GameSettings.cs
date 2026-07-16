using UnityEngine;

namespace TimeLoop.Core
{
    /// <summary>
    /// Single source of truth for global tuning values, authored as a ScriptableObject asset so
    /// designers can tweak feel without touching code. Create via
    /// Assets > Create > Time Loop > Game Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Time Loop/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        [Header("Simulation")]
        [Tooltip("Simulation ticks per second. This is the deterministic heartbeat every actor and ghost shares.")]
        [SerializeField] private int _ticksPerSecond = 12;

        [Tooltip("Safety cap on ticks processed in a single frame, to avoid a spiral of death after a long stall/hitch.")]
        [SerializeField] private int _maxTicksPerFrame = 8;

        [Header("Feel")]
        [SerializeField] private float _criticalTimeThresholdSeconds = 3f;

        [Header("Visual")]
        [SerializeField] private float _tilesPerSecondVisual = 6f;
        [SerializeField] private float _worldUnitsPerTile = 1f;

        public int TicksPerSecond => _ticksPerSecond;
        public int MaxTicksPerFrame => _maxTicksPerFrame;
        public float CriticalTimeThresholdSeconds => _criticalTimeThresholdSeconds;
        public float TilesPerSecondVisual => _tilesPerSecondVisual;
        public float WorldUnitsPerTile => _worldUnitsPerTile;
    }
}
