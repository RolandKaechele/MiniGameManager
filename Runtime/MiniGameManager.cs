using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MiniGameManager.Runtime
{
    /// <summary>
    /// <b>MiniGameManager</b> manages mini-game definitions and lifecycle.
    /// <para>
    /// <b>Responsibilities:</b>
    /// <list type="number">
    /// <item>Load <see cref="MiniGameData"/> definitions from <c>Resources/MiniGames/</c> and an optional external folder.</item>
    /// <item>Launch, complete, and abort mini-games via a uniform lifecycle API.</item>
    /// <item>Track completion results per mini-game id.</item>
    /// <item>Expose delegate hooks so external systems can handle scene/prefab loading.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Setup:</b> Add to a persistent manager GameObject. Assign JSON definition files to
    /// <c>Assets/Resources/MiniGames/</c>. Set <see cref="LaunchCallback"/> to integrate with your
    /// scene loader.
    /// </para>
    /// </summary>
    [AddComponentMenu("MiniGameManager/Mini Game Manager")]
    [DisallowMultipleComponent]
    public class MiniGameManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("If true, definitions are also loaded from persistentDataPath/MiniGames/.")]
        [SerializeField] private bool loadFromPersistentDataPath = true;

        [Header("Loaded mini-games (read-only, set at runtime)")]
        [SerializeField] private List<string> loadedMiniGameIds = new List<string>();

        // ─── Events ──────────────────────────────────────────────────────────────

        /// <summary>Fired when a mini-game is launched. Parameter: mini-game id.</summary>
        public event Action<string> OnMiniGameStarted;

        /// <summary>Fired when a mini-game completes. Parameter: the result object.</summary>
        public event Action<MiniGameResult> OnMiniGameCompleted;

        /// <summary>Fired when a mini-game is aborted. Parameter: mini-game id.</summary>
        public event Action<string> OnMiniGameAborted;

        // ─── Delegates ───────────────────────────────────────────────────────────

        /// <summary>
        /// Custom launcher — receives the mini-game id when <see cref="Launch"/> is called.
        /// If null, <see cref="MiniGameData.sceneOrPrefab"/> is only logged as a reminder.
        /// Typically assigned to a scene manager or prefab spawner.
        /// </summary>
        public Action<string> LaunchCallback;

        // ─── State ───────────────────────────────────────────────────────────────
        private readonly Dictionary<string, MiniGameData>   _miniGames = new Dictionary<string, MiniGameData>();
        private readonly Dictionary<string, MiniGameResult> _results   = new Dictionary<string, MiniGameResult>();
        private string _activeMiniGameId;

        // ─── Properties ──────────────────────────────────────────────────────────

        /// <summary>True while a mini-game is active.</summary>
        public bool IsPlaying => _activeMiniGameId != null;

        /// <summary>The id of the currently active mini-game, or <see langword="null"/>.</summary>
        public string ActiveMiniGameId => _activeMiniGameId;

        // ─── Unity lifecycle ─────────────────────────────────────────────────────

        private void Awake()
        {
            LoadAllMiniGames();
        }

        // ─── JSON Loading ─────────────────────────────────────────────────────────

        /// <summary>
        /// Loads all mini-game JSON files from <c>Resources/MiniGames/</c> and the external folder.
        /// Call again at runtime to reload after mod changes.
        /// </summary>
        public void LoadAllMiniGames()
        {
            _miniGames.Clear();
            loadedMiniGameIds.Clear();

            var assets = Resources.LoadAll<TextAsset>("MiniGames");
            foreach (var a in assets)
                RegisterFromJson(a.text);

            if (loadFromPersistentDataPath)
            {
                string dir = Path.Combine(Application.persistentDataPath, "MiniGames");
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
                    {
                        try { RegisterFromJson(File.ReadAllText(f)); }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[MiniGameManager] Failed to load {f}: {ex.Message}");
                        }
                    }
                }
            }

            Debug.Log($"[MiniGameManager] Loaded {_miniGames.Count} mini-game definition(s).");
        }

        /// <summary>
        /// Registers a single mini-game definition from a raw JSON string.
        /// Safe to call at runtime (e.g. from mod-loading bridges) to add or replace a definition.
        /// </summary>
        public void RegisterMiniGameFromJson(string json)
        {
            RegisterFromJson(json);
        }

        private void RegisterFromJson(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<MiniGameData>(json);
                if (data == null || string.IsNullOrEmpty(data.id)) return;
                data.rawJson = json;
                _miniGames[data.id] = data;
                if (!loadedMiniGameIds.Contains(data.id))
                    loadedMiniGameIds.Add(data.id);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MiniGameManager] Failed to parse mini-game JSON: {ex.Message}");
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Launch the mini-game with the given <paramref name="id"/>.
        /// Fires <see cref="OnMiniGameStarted"/> and calls <see cref="LaunchCallback"/> if set.
        /// Does nothing if a mini-game is already active or if canReplay is false and it has been completed.
        /// </summary>
        public void Launch(string id)
        {
            if (!_miniGames.TryGetValue(id, out var data))
            {
                Debug.LogWarning($"[MiniGameManager] Mini-game '{id}' not found.");
                return;
            }

            if (_activeMiniGameId != null)
            {
                Debug.LogWarning($"[MiniGameManager] Mini-game '{_activeMiniGameId}' is already active.");
                return;
            }

            if (!data.canReplay && _results.ContainsKey(id))
            {
                Debug.Log($"[MiniGameManager] Mini-game '{id}' already completed and canReplay is false.");
                return;
            }

            _activeMiniGameId = id;
            OnMiniGameStarted?.Invoke(id);

            if (LaunchCallback != null)
                LaunchCallback(id);
            else if (!string.IsNullOrEmpty(data.sceneOrPrefab))
                Debug.Log($"[MiniGameManager] Assign LaunchCallback to handle loading '{data.sceneOrPrefab}'.");
        }

        /// <summary>
        /// Record a completion result for the given mini-game.
        /// Fires <see cref="OnMiniGameCompleted"/>.
        /// </summary>
        public void Complete(string id, int score = 0)
        {
            if (_activeMiniGameId != null && _activeMiniGameId != id)
                Debug.LogWarning($"[MiniGameManager] Complete called for '{id}' but active mini-game is '{_activeMiniGameId}'.");

            var result = new MiniGameResult
            {
                miniGameId = id,
                completed  = true,
                score      = score,
                timestamp  = Time.time
            };

            _results[id]      = result;
            _activeMiniGameId = null;
            OnMiniGameCompleted?.Invoke(result);
        }

        /// <summary>
        /// Abort the active mini-game without recording a result.
        /// Fires <see cref="OnMiniGameAborted"/>.
        /// </summary>
        public void Abort(string id)
        {
            if (_activeMiniGameId == id)
                _activeMiniGameId = null;
            OnMiniGameAborted?.Invoke(id);
        }

        // ─── Query ────────────────────────────────────────────────────────────────

        /// <summary>Returns the <see cref="MiniGameData"/> for the given id, or null.</summary>
        public MiniGameData GetData(string id) =>
            _miniGames.TryGetValue(id, out var d) ? d : null;

        /// <summary>Returns the latest <see cref="MiniGameResult"/> for the given id, or null.</summary>
        public MiniGameResult GetResult(string id) =>
            _results.TryGetValue(id, out var r) ? r : null;

        /// <summary>Returns true if the mini-game has been completed at least once.</summary>
        public bool HasCompleted(string id) =>
            _results.TryGetValue(id, out var r) && r.completed;

        /// <summary>Returns all loaded mini-game definitions.</summary>
        public IReadOnlyDictionary<string, MiniGameData> GetAllMiniGames() => _miniGames;

        /// <summary>Returns all recorded mini-game results.</summary>
        public IReadOnlyDictionary<string, MiniGameResult> GetAllResults() => _results;
    }
}
