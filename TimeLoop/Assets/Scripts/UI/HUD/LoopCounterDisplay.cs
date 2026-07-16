using TimeLoop.Timeline;
using TMPro;
using UnityEngine;

namespace TimeLoop.UI.HUD
{
    /// <summary>
    /// Shows which timeline (loop attempt) is currently in progress, e.g. "Timeline 3" for the
    /// third attempt — TimelineEvents.OnLoopStart carries a 0-based loop index, shown here 1-based.
    /// </summary>
    public sealed class LoopCounterDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private TimeLoopManager _manager;

        /// <summary>
        /// Binds this display to the loop manager whose timeline count it should reflect. Safe to
        /// call again with a new manager — any previous subscription is torn down first.
        /// </summary>
        public void Bind(TimeLoopManager manager)
        {
            if (_manager != null)
            {
                _manager.Events.OnLoopStart -= HandleLoopStart;
            }

            _manager = manager;

            if (_manager == null)
            {
                return;
            }

            _manager.Events.OnLoopStart += HandleLoopStart;

            if (_manager.CurrentLevel != null)
            {
                HandleLoopStart(_manager.CurrentLoopIndex);
            }
        }

        private void HandleLoopStart(int loopIndex)
        {
            if (_label == null)
            {
                return;
            }

            _label.text = $"Timeline {loopIndex + 1}";
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.Events.OnLoopStart -= HandleLoopStart;
            }
        }
    }
}
