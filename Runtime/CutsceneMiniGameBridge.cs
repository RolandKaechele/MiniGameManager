#if MINIGAMEMANAGER_CSM
using UnityEngine;
using CutsceneManager.Runtime;

namespace MiniGameManager.Runtime
{
    /// <summary>
    /// Optional bridge between MiniGameManager and CutsceneManager.
    /// Enable define <c>MINIGAMEMANAGER_CSM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Listens to <see cref="CutsceneManager.Runtime.CutsceneManager.OnCustomEvent"/> and
    /// interprets payloads matching the configured verbs as mini-game commands:
    /// </para>
    /// <list type="bullet">
    /// <item><c>"minigame.launch:sorting_puzzle"</c>          — launches the mini-game</item>
    /// <item><c>"minigame.complete:sorting_puzzle"</c>        — completes with score 0</item>
    /// <item><c>"minigame.complete:sorting_puzzle:250"</c>    — completes with score 250</item>
    /// <item><c>"minigame.abort:sorting_puzzle"</c>           — aborts the mini-game</item>
    /// </list>
    /// <para>The command verbs are configurable in the Inspector.</para>
    /// </summary>
    [AddComponentMenu("MiniGameManager/Cutscene Mini Game Bridge")]
    [DisallowMultipleComponent]
    public class CutsceneMiniGameBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Verb for launching a mini-game from a cutscene custom event.")]
        [SerializeField] private string launchVerb   = "minigame.launch";

        [Tooltip("Verb for completing a mini-game. Optionally append a score: \"minigame.complete:id:250\".")]
        [SerializeField] private string completeVerb = "minigame.complete";

        [Tooltip("Verb for aborting a mini-game.")]
        [SerializeField] private string abortVerb    = "minigame.abort";

        // ─── References ──────────────────────────────────────────────────────────
        private MiniGameManager _mgr;
        private CutsceneManager.Runtime.CutsceneManager _cutscene;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _mgr      = GetComponent<MiniGameManager>() ?? FindFirstObjectByType<MiniGameManager>();
            _cutscene = GetComponent<CutsceneManager.Runtime.CutsceneManager>()
                        ?? FindFirstObjectByType<CutsceneManager.Runtime.CutsceneManager>();

            if (_mgr      == null) Debug.LogWarning("[CutsceneMiniGameBridge] MiniGameManager not found.");
            if (_cutscene == null) Debug.LogWarning("[CutsceneMiniGameBridge] CutsceneManager not found.");
        }

        private void OnEnable()
        {
            if (_cutscene != null) _cutscene.OnCustomEvent += OnCustomEvent;
        }

        private void OnDisable()
        {
            if (_cutscene != null) _cutscene.OnCustomEvent -= OnCustomEvent;
        }

        // ─── Handler ─────────────────────────────────────────────────────────────
        private void OnCustomEvent(string sequenceId, string eventData)
        {
            if (string.IsNullOrEmpty(eventData) || _mgr == null) return;

            if (TryParseVerb(eventData, launchVerb, out string launchId))
            {
                _mgr.Launch(launchId);
                return;
            }

            if (TryParseVerb(eventData, completeVerb, out string completeRaw))
            {
                // Optional score after a second ':' separator — e.g. "sorting_puzzle:250"
                int score = 0;
                string completeId = completeRaw;
                int sep = completeRaw.IndexOf(':');
                if (sep >= 0)
                {
                    int.TryParse(completeRaw.Substring(sep + 1), out score);
                    completeId = completeRaw.Substring(0, sep);
                }
                _mgr.Complete(completeId, score);
                return;
            }

            if (TryParseVerb(eventData, abortVerb, out string abortId))
                _mgr.Abort(abortId);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────
        private static bool TryParseVerb(string eventData, string verb, out string payload)
        {
            payload = null;
            if (string.IsNullOrEmpty(verb) || !eventData.StartsWith(verb)) return false;
            int afterVerb = verb.Length;
            if (eventData.Length <= afterVerb || eventData[afterVerb] != ':') return false;
            payload = eventData.Substring(afterVerb + 1);
            return !string.IsNullOrEmpty(payload);
        }
    }
}
#else
// MINIGAMEMANAGER_CSM not defined — bridge is inactive.
namespace MiniGameManager.Runtime
{
    /// <summary>No-op stub. Enable MINIGAMEMANAGER_CSM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("MiniGameManager/Cutscene Mini Game Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class CutsceneMiniGameBridge : UnityEngine.MonoBehaviour { }
}
#endif
