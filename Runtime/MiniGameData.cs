using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGameManager.Runtime
{
    // -------------------------------------------------------------------------
    // MiniGameCategory
    // -------------------------------------------------------------------------

    /// <summary>Broad gameplay category for a mini-game.</summary>
    public enum MiniGameCategory
    {
        Puzzle      = 0,
        Action      = 1,
        Racing      = 2,
        Shooting    = 3,
        Platformer  = 4,
        Custom      = 5
    }

    // -------------------------------------------------------------------------
    // MiniGameData
    // -------------------------------------------------------------------------

    /// <summary>
    /// Describes a single mini-game that can be launched at runtime.
    /// Authored in JSON and stored in <c>Resources/MiniGames/</c>.
    /// </summary>
    [Serializable]
    public class MiniGameData
    {
        /// <summary>Unique identifier used to launch this mini-game.</summary>
        public string id;

        /// <summary>Human-readable title.</summary>
        public string title;

        /// <summary>Short description shown in menus.</summary>
        public string description;

        /// <summary>Localization key for the title.</summary>
        public string titleLocalizationKey;

        /// <summary>Localization key for the description.</summary>
        public string descriptionLocalizationKey;

        /// <summary>
        /// Scene name or Resources-relative path to the mini-game prefab.
        /// Passed to <see cref="MiniGameManager.LaunchCallback"/> when launching.
        /// </summary>
        public string sceneOrPrefab;

        /// <summary>Resources-relative path to a preview icon sprite.</summary>
        public string previewIconResource;

        public MiniGameCategory category;

        /// <summary>
        /// Save-flag name that must be set to unlock this mini-game.
        /// Leave empty for mini-games that are always available.
        /// </summary>
        public string unlockCondition;

        /// <summary>If true the mini-game can be played again after completion.</summary>
        public bool canReplay;

        /// <summary>If true, win/loss is determined by score vs <see cref="minPassScore"/>.</summary>
        public bool trackScore;

        /// <summary>Minimum score required to count as a successful completion.</summary>
        public int minPassScore;

        /// <summary>Raw JSON stored during deserialisation (non-serialised).</summary>
        [NonSerialized] public string rawJson;
    }

    // -------------------------------------------------------------------------
    // MiniGameResult
    // -------------------------------------------------------------------------

    /// <summary>
    /// Records the outcome of a mini-game session.
    /// Returned via <see cref="MiniGameManager.OnMiniGameCompleted"/> and stored per mini-game id.
    /// </summary>
    [Serializable]
    public class MiniGameResult
    {
        /// <summary>Id of the mini-game this result belongs to.</summary>
        public string miniGameId;

        /// <summary>True if the mini-game was completed (not aborted).</summary>
        public bool completed;

        /// <summary>Final score. Only meaningful when <see cref="MiniGameData.trackScore"/> is true.</summary>
        public int score;

        /// <summary><c>Time.time</c> value recorded when the result was committed.</summary>
        public float timestamp;
    }
}
