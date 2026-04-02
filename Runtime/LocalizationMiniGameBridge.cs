#if MINIGAMEMANAGER_LM
using UnityEngine;
using LocalizationManager.Runtime;

namespace MiniGameManager.Runtime
{
    /// <summary>
    /// Optional bridge between MiniGameManager and LocalizationManager.
    /// Enable define <c>MINIGAMEMANAGER_LM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Provides helper methods that resolve localized titles and descriptions for mini-games.
    /// UI code should call <see cref="GetTitle"/> and <see cref="GetDescription"/> rather than
    /// reading <c>MiniGameData.title</c> directly, so the active language is always applied.
    /// </para>
    /// </summary>
    [AddComponentMenu("MiniGameManager/Localization Mini Game Bridge")]
    [DisallowMultipleComponent]
    public class LocalizationMiniGameBridge : MonoBehaviour
    {
        private MiniGameManager _mgr;
        private LocalizationManager.Runtime.LocalizationManager _localization;

        private void Awake()
        {
            _mgr          = GetComponent<MiniGameManager>() ?? FindFirstObjectByType<MiniGameManager>();
            _localization = GetComponent<LocalizationManager.Runtime.LocalizationManager>()
                            ?? FindFirstObjectByType<LocalizationManager.Runtime.LocalizationManager>();

            if (_mgr          == null) Debug.LogWarning("[LocalizationMiniGameBridge] MiniGameManager not found.");
            if (_localization == null) Debug.LogWarning("[LocalizationMiniGameBridge] LocalizationManager not found.");
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the localized title for the mini-game with the given id.
        /// Falls back to <c>MiniGameData.title</c> when no localization key is set.
        /// </summary>
        public string GetTitle(string miniGameId)
        {
            var data = _mgr?.GetData(miniGameId);
            return data == null ? string.Empty : Resolve(data.titleLocalizationKey, data.title);
        }

        /// <summary>
        /// Returns the localized title for the given <see cref="MiniGameData"/> object.
        /// </summary>
        public string GetTitle(MiniGameData data)
        {
            if (data == null) return string.Empty;
            return Resolve(data.titleLocalizationKey, data.title);
        }

        /// <summary>
        /// Returns the localized description for the mini-game with the given id.
        /// Falls back to <c>MiniGameData.description</c> when no localization key is set.
        /// </summary>
        public string GetDescription(string miniGameId)
        {
            var data = _mgr?.GetData(miniGameId);
            return data == null ? string.Empty : Resolve(data.descriptionLocalizationKey, data.description);
        }

        /// <summary>
        /// Returns the localized description for the given <see cref="MiniGameData"/> object.
        /// </summary>
        public string GetDescription(MiniGameData data)
        {
            if (data == null) return string.Empty;
            return Resolve(data.descriptionLocalizationKey, data.description);
        }

        // ─── Internal ─────────────────────────────────────────────────────────────
        private string Resolve(string locKey, string fallback)
        {
            if (!string.IsNullOrEmpty(locKey) && _localization != null)
                return _localization.GetText(locKey) ?? fallback;
            return fallback;
        }
    }
}
#else
namespace MiniGameManager.Runtime
{
    /// <summary>No-op stub. Enable MINIGAMEMANAGER_LM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("MiniGameManager/Localization Mini Game Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class LocalizationMiniGameBridge : UnityEngine.MonoBehaviour { }
}
#endif
