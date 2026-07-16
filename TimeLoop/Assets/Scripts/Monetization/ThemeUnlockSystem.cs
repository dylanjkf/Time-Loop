using System;
using System.Collections.Generic;

namespace TimeLoop.Monetization
{
    /// <summary>Visual theme skins players can unlock (e.g. via premium, achievements, or events).</summary>
    public enum ThemeId
    {
        Default,
        Cyberpunk,
        AncientClockwork,
        SpaceTime,
        DreamWorld,
        MinimalBlackWhite,
    }

    /// <summary>
    /// Tracks which visual themes are unlocked and which one is currently active.
    /// <see cref="ThemeId.Default"/> is always unlocked and is never stored — it's implicit —
    /// so the round trip of GetUnlockedThemeNames() -> SaveData.UnlockedThemes -> constructor
    /// only ever carries the "extra" unlocked themes.
    /// </summary>
    public sealed class ThemeUnlockSystem
    {
        private readonly HashSet<ThemeId> _unlockedThemes = new HashSet<ThemeId>();

        /// <param name="unlockedThemeNames">
        /// Theme names already unlocked, as persisted in SaveData.UnlockedThemes — each entry is
        /// expected to match a <see cref="ThemeId"/>'s ToString(). Unrecognized names are
        /// ignored rather than throwing, and an explicit "Default" entry is harmless (Default is
        /// always unlocked regardless).
        /// </param>
        public ThemeUnlockSystem(List<string> unlockedThemeNames)
        {
            if (unlockedThemeNames == null)
            {
                return;
            }

            foreach (var name in unlockedThemeNames)
            {
                if (Enum.TryParse<ThemeId>(name, out var theme) && theme != ThemeId.Default)
                {
                    _unlockedThemes.Add(theme);
                }
            }
        }

        public ThemeId ActiveTheme { get; private set; } = ThemeId.Default;

        public bool IsUnlocked(ThemeId theme) => theme == ThemeId.Default || _unlockedThemes.Contains(theme);

        public void Unlock(ThemeId theme)
        {
            if (theme != ThemeId.Default)
            {
                _unlockedThemes.Add(theme);
            }
        }

        /// <summary>No-op if <paramref name="theme"/> isn't unlocked yet.</summary>
        public void SetActiveTheme(ThemeId theme)
        {
            if (IsUnlocked(theme))
            {
                ActiveTheme = theme;
            }
        }

        /// <summary>Snapshot of unlocked theme names (excluding the always-unlocked Default), for the caller to persist into SaveData.UnlockedThemes.</summary>
        public List<string> GetUnlockedThemeNames()
        {
            var names = new List<string>(_unlockedThemes.Count);
            foreach (var theme in _unlockedThemes)
            {
                names.Add(theme.ToString());
            }
            return names;
        }
    }
}
