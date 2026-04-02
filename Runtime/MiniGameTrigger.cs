using UnityEngine;

namespace MiniGameManager.Runtime
{
    public enum MiniGameTriggerMode
    {
        OnStart,
        OnTriggerEnter,
        OnInteract
    }

    /// <summary>
    /// Launches a <see cref="MiniGameManager"/> mini-game in response to common Unity scene events
    /// without requiring any code.
    /// </summary>
    [AddComponentMenu("MiniGameManager/Mini Game Trigger")]
    public class MiniGameTrigger : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("ID of the mini-game to launch.")]
        [SerializeField] private string miniGameId;

        [SerializeField] private MiniGameTriggerMode triggerMode = MiniGameTriggerMode.OnTriggerEnter;

        [Tooltip("Only trigger if this flag is NOT already set. Wired automatically by SaveMiniGameBridge when MINIGAMEMANAGER_SM is active.")]
        [SerializeField] private string requireFlagNotSet;

        [Tooltip("Collider tag that activates this trigger (OnTriggerEnter mode).")]
        [SerializeField] private string triggerTag = "Player";

        [Tooltip("Disable this GameObject after the trigger fires.")]
        [SerializeField] private bool disableAfterTrigger = true;

        // ─── Delegates ───────────────────────────────────────────────────────────

        /// <summary>
        /// Check a flag condition. Wired by <c>SaveMiniGameBridge</c> when <c>MINIGAMEMANAGER_SM</c> is active.
        /// </summary>
        public System.Func<string, bool> ConditionCheck;

        // ─── Internal ────────────────────────────────────────────────────────────
        private MiniGameManager _mgr;
        private bool _triggered;

        private void Start()
        {
            _mgr = FindFirstObjectByType<MiniGameManager>();
            if (_mgr == null)
                Debug.LogWarning("[MiniGameTrigger] No MiniGameManager found in scene.");

            if (triggerMode == MiniGameTriggerMode.OnStart)
                Fire();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerMode != MiniGameTriggerMode.OnTriggerEnter) return;
            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
            Fire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerMode != MiniGameTriggerMode.OnTriggerEnter) return;
            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
            Fire();
        }

        /// <summary>Manually fire the trigger (for OnInteract mode or external calls).</summary>
        public void Interact() => Fire();

        private void Fire()
        {
            if (_triggered) return;
            if (_mgr == null || string.IsNullOrEmpty(miniGameId)) return;

            if (!string.IsNullOrEmpty(requireFlagNotSet)
                && ConditionCheck != null
                && ConditionCheck(requireFlagNotSet))
            {
                Debug.Log($"[MiniGameTrigger] Blocked by flag '{requireFlagNotSet}'.");
                return;
            }

            _triggered = true;
            _mgr.Launch(miniGameId);

            if (disableAfterTrigger)
                gameObject.SetActive(false);
        }
    }
}
