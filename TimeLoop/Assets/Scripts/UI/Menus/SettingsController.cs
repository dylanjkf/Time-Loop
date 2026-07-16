using System;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.Menus
{
    /// <summary>
    /// Binds the settings panel's controls to externally-owned state. This controller holds no
    /// settings state itself — the caller supplies the current values and the callbacks to invoke
    /// when the player changes them (typically backed by a persisted settings/preferences store).
    /// </summary>
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _noCountdownPressureToggle;
        [SerializeField] private Slider _tweenSpeedSlider;

        /// <summary>
        /// Initializes each control's displayed value without firing its change callback, then
        /// subscribes the supplied callbacks so subsequent player interaction is reported back.
        /// </summary>
        public void Bind(
            bool musicEnabled,
            bool soundEnabled,
            bool noCountdownPressure,
            float tweenSpeedNormalized,
            Action<bool> onMusicChanged,
            Action<bool> onSoundChanged,
            Action<bool> onNoCountdownPressureChanged,
            Action<float> onTweenSpeedChanged)
        {
            if (_musicToggle != null)
            {
                _musicToggle.SetIsOnWithoutNotify(musicEnabled);
                _musicToggle.onValueChanged.RemoveAllListeners();
                _musicToggle.onValueChanged.AddListener(value => onMusicChanged?.Invoke(value));
            }

            if (_soundToggle != null)
            {
                _soundToggle.SetIsOnWithoutNotify(soundEnabled);
                _soundToggle.onValueChanged.RemoveAllListeners();
                _soundToggle.onValueChanged.AddListener(value => onSoundChanged?.Invoke(value));
            }

            if (_noCountdownPressureToggle != null)
            {
                _noCountdownPressureToggle.SetIsOnWithoutNotify(noCountdownPressure);
                _noCountdownPressureToggle.onValueChanged.RemoveAllListeners();
                _noCountdownPressureToggle.onValueChanged.AddListener(value => onNoCountdownPressureChanged?.Invoke(value));
            }

            if (_tweenSpeedSlider != null)
            {
                _tweenSpeedSlider.SetValueWithoutNotify(tweenSpeedNormalized);
                _tweenSpeedSlider.onValueChanged.RemoveAllListeners();
                _tweenSpeedSlider.onValueChanged.AddListener(value => onTweenSpeedChanged?.Invoke(value));
            }
        }
    }
}
