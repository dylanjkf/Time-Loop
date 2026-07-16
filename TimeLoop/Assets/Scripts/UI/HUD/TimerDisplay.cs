using TimeLoop.Timeline;
using TMPro;
using UnityEngine;

namespace TimeLoop.UI.HUD
{
    /// <summary>
    /// Shows the current loop's countdown as whole seconds (e.g. "12s"). Only visible for levels
    /// that use a fixed timer (see LevelDefinition.UsesFixedTimer) — cleared and hidden for
    /// untimed levels, which have no countdown to show.
    /// </summary>
    public sealed class TimerDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;

        private TimeLoopManager _manager;

        /// <summary>Binds this display to the loop manager whose countdown it should reflect.</summary>
        public void Bind(TimeLoopManager manager)
        {
            _manager = manager;
        }

        private void Update()
        {
            if (_label == null || _manager == null)
            {
                return;
            }

            var usesFixedTimer = _manager.CurrentLevel != null && _manager.CurrentLevel.UsesFixedTimer;
            if (!usesFixedTimer)
            {
                if (_label.enabled)
                {
                    _label.text = string.Empty;
                    _label.enabled = false;
                }
                return;
            }

            if (!_label.enabled)
            {
                _label.enabled = true;
            }

            var wholeSeconds = Mathf.FloorToInt(_manager.SecondsRemaining);
            _label.text = $"{wholeSeconds}s";
        }
    }
}
