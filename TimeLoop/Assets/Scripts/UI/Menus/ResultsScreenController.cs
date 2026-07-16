using System;
using System.Text;
using TimeLoop.Levels;
using TimeLoop.Puzzle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TimeLoop.UI.Menus
{
    /// <summary>
    /// End-of-level summary panel: shows the star rating and attempt stats produced by
    /// <see cref="LevelResult"/>, then routes to whichever destination the player picks next.
    /// </summary>
    public sealed class ResultsScreenController : MonoBehaviour
    {
        private const int MaxStars = 3;
        private const string FilledStarGlyph = "⭐";
        private const string EmptyStarGlyph = "☆";

        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private TextMeshProUGUI _levelNameLabel;
        [SerializeField] private TextMeshProUGUI _starsLabel;
        [SerializeField] private TextMeshProUGUI _statsLabel;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _levelSelectButton;

        /// <summary>Populates and shows the results panel for a completed level attempt.</summary>
        public void Show(
            LevelDefinition level,
            LevelResult result,
            Action onNextLevel,
            Action onRetry,
            Action onLevelSelect)
        {
            if (_panelRoot != null)
            {
                _panelRoot.SetActive(true);
            }

            if (_levelNameLabel != null)
            {
                _levelNameLabel.text = level.Name;
            }

            if (_starsLabel != null)
            {
                _starsLabel.text = BuildStarsText(result.Stars);
            }

            if (_statsLabel != null)
            {
                _statsLabel.text = BuildStatsText(result);
            }

            if (_nextLevelButton != null)
            {
                _nextLevelButton.onClick.RemoveAllListeners();
                _nextLevelButton.onClick.AddListener(() =>
                {
                    onNextLevel?.Invoke();
                    _panelRoot.SetActive(false);
                });
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(() =>
                {
                    onRetry?.Invoke();
                    _panelRoot.SetActive(false);
                });
            }

            if (_levelSelectButton != null)
            {
                _levelSelectButton.onClick.RemoveAllListeners();
                _levelSelectButton.onClick.AddListener(() =>
                {
                    onLevelSelect?.Invoke();
                    _panelRoot.SetActive(false);
                });
            }
        }

        private static string BuildStarsText(StarRating stars)
        {
            var filled = (int)stars;
            var empty = MaxStars - filled;

            var builder = new StringBuilder(MaxStars);
            for (var i = 0; i < filled; i++)
            {
                builder.Append(FilledStarGlyph);
            }
            for (var i = 0; i < empty; i++)
            {
                builder.Append(EmptyStarGlyph);
            }

            return builder.ToString();
        }

        private static string BuildStatsText(LevelResult result)
        {
            var text = $"{result.LoopsUsed} timelines · {result.TicksElapsed} ticks";
            if (result.UsedHint)
            {
                text += " · Hint used";
            }

            return text;
        }
    }
}
