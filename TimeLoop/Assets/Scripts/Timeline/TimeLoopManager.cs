using System.Collections.Generic;
using System.Linq;
using TimeLoop.Actors;
using TimeLoop.Core;
using TimeLoop.Grid;
using TimeLoop.Interactables;
using TimeLoop.Levels;
using TimeLoop.Puzzle;
using UnityEngine;

namespace TimeLoop.Timeline
{
    /// <summary>
    /// The orchestrator and single most important class in the codebase: drives the countdown,
    /// steps the deterministic simulation once per tick for the live player and every active
    /// ghost, and manages the loop transition (freeze recording -&gt; spawn ghost -&gt; reset world -&gt;
    /// start fresh recording). Every other system (UI, Audio, VFX, Achievements) reacts only
    /// through <see cref="Events"/> — nothing else reaches into this class's internals. See
    /// docs/TECHNICAL_ARCHITECTURE.md section 4.3.
    /// </summary>
    public sealed class TimeLoopManager : MonoBehaviour
    {
        [SerializeField] private GameSettings _gameSettings;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private GhostAgent _ghostPrefab;
        [SerializeField] private PuzzleManager _puzzleManager;

        public TimelineEvents Events { get; } = new TimelineEvents();

        public LevelDefinition CurrentLevel { get; private set; }
        public GridWorld World { get; private set; }
        public int CurrentLoopIndex { get; private set; }
        public int CurrentTick { get; private set; }
        public int TotalLoopsUsedThisAttempt { get; private set; }
        public GridActor PlayerActor => _playerController.Actor;
        public IReadOnlyList<GhostAgent> ActiveGhosts => _ghosts;

        public float SecondsRemaining =>
            Mathf.Max(0f, (_loopDurationTicks - CurrentTick) / (float)_gameSettings.TicksPerSecond);

        private readonly List<GhostAgent> _ghosts = new List<GhostAgent>();
        private readonly TimelineRecorder _recorder = new TimelineRecorder();

        private float _tickAccumulator;
        private int _loopDurationTicks;
        private bool _levelComplete;
        private bool _timeCriticalFired;
        private bool _hazardTriggeredThisTick;

        public void LoadLevel(LevelDefinition level)
        {
            CurrentLevel = level;
            World = level.BuildWorld();
            _loopDurationTicks = Mathf.RoundToInt(level.LoopSeconds * _gameSettings.TicksPerSecond);

            foreach (var ghost in _ghosts)
            {
                if (ghost != null) Destroy(ghost.gameObject);
            }
            _ghosts.Clear();

            CurrentLoopIndex = 0;
            TotalLoopsUsedThisAttempt = 0;
            _levelComplete = false;

            _playerController.Initialize("player", level.SpawnPosition);
            _puzzleManager.Initialize(this, level);

            foreach (var hazard in World.AllEntities.OfType<Hazard>())
            {
                hazard.OnPlayerHit += OnHazardHit;
            }

            BeginLoop();
        }

        private void OnHazardHit(GridActor actor) => _hazardTriggeredThisTick = true;

        private void BeginLoop()
        {
            CurrentTick = 0;
            _tickAccumulator = 0f;
            _timeCriticalFired = false;
            _hazardTriggeredThisTick = false;

            World.ResetToInitial();

            _playerController.ResetForNewLoop();
            foreach (var ghost in _ghosts)
            {
                ghost.ResetForNewLoop();
            }

            _recorder.BeginNewRecording(CurrentLoopIndex);
            Events.RaiseLoopStart(CurrentLoopIndex);
        }

        private void Update()
        {
            if (_levelComplete || World == null) return;

            _tickAccumulator += Time.deltaTime;
            var tickInterval = 1f / _gameSettings.TicksPerSecond;

            var safetyTicks = 0;
            while (_tickAccumulator >= tickInterval && safetyTicks < _gameSettings.MaxTicksPerFrame)
            {
                _tickAccumulator -= tickInterval;
                RunTick();
                safetyTicks++;
                if (_levelComplete) break;
            }

            MaybeRaiseTimeCritical();
        }

        private void RunTick()
        {
            var orderedInputs = new List<ActorTickInput>(_ghosts.Count + 1);

            foreach (var ghost in _ghosts)
            {
                orderedInputs.Add(new ActorTickInput(ghost.Actor, ghost.InputProvider.GetCommand(CurrentTick)));
            }

            var playerCommand = _playerController.InputProvider.GetCommand(CurrentTick);
            orderedInputs.Add(new ActorTickInput(_playerController.Actor, playerCommand));

            _recorder.RecordTick(CurrentTick, playerCommand);

            World.Step(orderedInputs, CurrentTick);
            Events.RaiseTick(CurrentTick);

            if (_hazardTriggeredThisTick)
            {
                _hazardTriggeredThisTick = false;
                Events.RaiseHazardReset();
                BeginLoop();
                return;
            }

            if (_puzzleManager.CheckWinCondition(this))
            {
                CompleteLevel();
                return;
            }

            CurrentTick++;

            if (CurrentLevel.UsesFixedTimer && CurrentTick >= _loopDurationTicks)
            {
                EndLoop();
            }
        }

        /// <summary>Manually ends the current loop early ("Split Timeline" — GDD section 4.2), available from World 2 onward.</summary>
        public void RequestManualLoopSplit()
        {
            if (!_levelComplete && CurrentLevel != null && CurrentLevel.AllowsManualSplit)
            {
                EndLoop();
            }
        }

        private void EndLoop()
        {
            var completedTimeline = _recorder.Current;
            Events.RaiseLoopEnd(completedTimeline);

            if (_ghosts.Count < CurrentLevel.MaxTimelines)
            {
                SpawnGhostFromTimeline(completedTimeline);
            }

            CurrentLoopIndex++;
            TotalLoopsUsedThisAttempt++;
            BeginLoop();
        }

        private void SpawnGhostFromTimeline(RecordedTimeline timelineRecording)
        {
            GhostAgent ghost;
            if (_ghostPrefab != null)
            {
                ghost = Instantiate(_ghostPrefab, transform);
            }
            else
            {
                var go = new GameObject($"Ghost_{timelineRecording.LoopIndex}");
                go.transform.SetParent(transform);
                go.AddComponent<ActorVisual>();
                ghost = go.AddComponent<GhostAgent>();
            }

            ghost.Initialize($"ghost_{timelineRecording.LoopIndex}", CurrentLevel.SpawnPosition, timelineRecording);
            _ghosts.Add(ghost);
            Events.RaiseGhostSpawned(ghost);
        }

        /// <summary>The visible Reset button — restarts the current in-progress loop without discarding already-spawned ghosts.</summary>
        public void ResetCurrentLoop() => BeginLoop();

        /// <summary>Full restart: clears every ghost and starts the level over from loop 0.</summary>
        public void RestartLevel() => LoadLevel(CurrentLevel);

        private void CompleteLevel()
        {
            _levelComplete = true;
            TotalLoopsUsedThisAttempt++;
            _puzzleManager.OnLevelCompleted(this);
        }

        private void MaybeRaiseTimeCritical()
        {
            if (_timeCriticalFired || CurrentLevel == null || !CurrentLevel.UsesFixedTimer) return;

            if (SecondsRemaining <= _gameSettings.CriticalTimeThresholdSeconds)
            {
                _timeCriticalFired = true;
                Events.RaiseTimeCritical(SecondsRemaining);
            }
        }

        private void OnDestroy()
        {
            if (World == null) return;

            foreach (var hazard in World.AllEntities.OfType<Hazard>())
            {
                hazard.OnPlayerHit -= OnHazardHit;
            }
        }
    }
}
