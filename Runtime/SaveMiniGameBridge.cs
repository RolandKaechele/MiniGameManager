#if MINIGAMEMANAGER_SM
using System;
using System.Collections.Generic;
using UnityEngine;
using SaveManager.Runtime;

namespace MiniGameManager.Runtime
{
    /// <summary>
    /// Optional bridge between MiniGameManager and SaveManager.
    /// Enable define <c>MINIGAMEMANAGER_SM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// <list type="bullet">
    /// <item>Persists all mini-game completion results as a JSON blob under the configured
    /// SaveManager custom-data key.</item>
    /// <item>Wires <see cref="MiniGameTrigger.ConditionCheck"/> on all triggers in the scene
    /// to <see cref="SaveManager.Runtime.SaveManager.IsSet"/> so flag-gated triggers work.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("MiniGameManager/Save Mini Game Bridge")]
    [DisallowMultipleComponent]
    public class SaveMiniGameBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Custom data key used in the SaveManager save slot.")]
        [SerializeField] private string saveKey = "minigames";

        [Tooltip("Automatically persist results to the save slot when a mini-game completes.")]
        [SerializeField] private bool autoSaveOnComplete = false;

        // ─── References ──────────────────────────────────────────────────────────
        private MiniGameManager _mgr;
        private SaveManager.Runtime.SaveManager _save;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _mgr  = GetComponent<MiniGameManager>() ?? FindFirstObjectByType<MiniGameManager>();
            _save = GetComponent<SaveManager.Runtime.SaveManager>()
                    ?? FindFirstObjectByType<SaveManager.Runtime.SaveManager>();

            if (_mgr  == null) { Debug.LogWarning("[SaveMiniGameBridge] MiniGameManager not found."); return; }
            if (_save == null) { Debug.LogWarning("[SaveMiniGameBridge] SaveManager not found."); return; }

            WireTriggers();
        }

        private void OnEnable()
        {
            if (_mgr != null) _mgr.OnMiniGameCompleted += OnMiniGameCompleted;
        }

        private void OnDisable()
        {
            if (_mgr != null) _mgr.OnMiniGameCompleted -= OnMiniGameCompleted;
        }

        // ─── Handlers ────────────────────────────────────────────────────────────
        private void OnMiniGameCompleted(MiniGameResult result)
        {
            if (autoSaveOnComplete) SaveResults();
        }

        // ─── Persistence ─────────────────────────────────────────────────────────

        /// <summary>Save all current mini-game results into the active save slot.</summary>
        public void SaveResults()
        {
            if (_save == null || _mgr == null) return;
            var wrapper = new MiniGameResultsSnapshot();
            foreach (var kv in _mgr.GetAllResults())
                wrapper.results.Add(kv.Value);
            _save.SetCustom(saveKey, JsonUtility.ToJson(wrapper));
        }

        /// <summary>Restore mini-game results from the active save slot.</summary>
        public void LoadResults()
        {
            if (_save == null || _mgr == null) return;
            string json = _save.GetCustom(saveKey);
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                var wrapper = JsonUtility.FromJson<MiniGameResultsSnapshot>(json);
                if (wrapper?.results == null) return;
                foreach (var r in wrapper.results)
                    if (r.completed) _mgr.Complete(r.miniGameId, r.score);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveMiniGameBridge] Failed to load results: {ex.Message}");
            }
        }

        // ─── Trigger wiring ───────────────────────────────────────────────────────
        private void WireTriggers()
        {
            foreach (var trigger in FindObjectsByType<MiniGameTrigger>(FindObjectsSortMode.None))
                trigger.ConditionCheck = flag => _save.IsSet(flag);
        }
    }

    [Serializable]
    internal class MiniGameResultsSnapshot
    {
        public List<MiniGameResult> results = new List<MiniGameResult>();
    }
}
#else
// MINIGAMEMANAGER_SM not defined — bridge is inactive.
namespace MiniGameManager.Runtime
{
    /// <summary>No-op stub. Enable MINIGAMEMANAGER_SM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("MiniGameManager/Save Mini Game Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class SaveMiniGameBridge : UnityEngine.MonoBehaviour { }
}
#endif
