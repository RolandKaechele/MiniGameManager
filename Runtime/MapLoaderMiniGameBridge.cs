#if MINIGAMEMANAGER_MLF
using System;
using System.IO;
using UnityEngine;
using MapLoaderFramework.Runtime;

namespace MiniGameManager.Runtime
{
    /// <summary>
    /// Optional bridge between MiniGameManager and MapLoaderFramework.
    /// Enable define <c>MINIGAMEMANAGER_MLF</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Provides two integration points:
    /// <list type="bullet">
    /// <item><b>Abort on map load</b> — aborts the currently active mini-game whenever a new map loads,
    /// preventing a mini-game from persisting across scene transitions.</item>
    /// <item><b>Mod support</b> — when <see cref="ModManager"/> is present, subscribes to
    /// <see cref="ModManager.OnModsChanged"/> and reloads mini-game definitions from the
    /// <c>minigames/</c> subfolder of each enabled mod.</item>
    /// </list>
    /// </para>
    /// </summary>
    [AddComponentMenu("MiniGameManager/Map Loader Mini Game Bridge")]
    [DisallowMultipleComponent]
    public class MapLoaderMiniGameBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Abort the active mini-game whenever a new map loads.")]
        [SerializeField] private bool abortOnMapLoad = true;

        [Tooltip("Reload mini-game definitions from mod directories when mods change.")]
        [SerializeField] private bool reloadOnModsChanged = true;

        // ─── References ──────────────────────────────────────────────────────────
        private MiniGameManager _mgr;
        private MapLoaderFramework.Runtime.MapLoaderFramework _framework;
        private ModManager _modManager;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _mgr        = GetComponent<MiniGameManager>() ?? FindFirstObjectByType<MiniGameManager>();
            _framework  = GetComponent<MapLoaderFramework.Runtime.MapLoaderFramework>()
                          ?? FindFirstObjectByType<MapLoaderFramework.Runtime.MapLoaderFramework>();
            _modManager = GetComponent<ModManager>() ?? FindFirstObjectByType<ModManager>();

            if (_mgr       == null) Debug.LogWarning("[MapLoaderMiniGameBridge] MiniGameManager not found.");
            if (_framework == null) Debug.LogWarning("[MapLoaderMiniGameBridge] MapLoaderFramework not found.");
        }

        private void OnEnable()
        {
            if (_framework  != null) _framework.OnMapLoaded    += OnMapLoaded;
            if (_modManager != null) _modManager.OnModsChanged += OnModsChanged;
        }

        private void OnDisable()
        {
            if (_framework  != null) _framework.OnMapLoaded    -= OnMapLoaded;
            if (_modManager != null) _modManager.OnModsChanged -= OnModsChanged;
        }

        // ─── Handlers ─────────────────────────────────────────────────────────────
        private void OnMapLoaded(MapData mapData)
        {
            if (_mgr == null || !_mgr.IsPlaying) return;
            if (abortOnMapLoad)
                _mgr.Abort(_mgr.ActiveMiniGameId);
        }

        private void OnModsChanged()
        {
            if (!reloadOnModsChanged || _mgr == null || _modManager == null) return;

            // Reload definitions from each enabled mod's minigames/ subfolder
            foreach (var (filePath, modId) in _modManager.GetEnabledModMiniGameFiles())
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    _mgr.RegisterMiniGameFromJson(json);
                    Debug.Log($"[MapLoaderMiniGameBridge] Loaded mini-game from mod '{modId}': {filePath}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MapLoaderMiniGameBridge] Failed to load '{filePath}': {ex.Message}");
                }
            }
        }
    }
}
#else
// MINIGAMEMANAGER_MLF not defined — bridge is inactive.
namespace MiniGameManager.Runtime
{
    /// <summary>No-op stub. Enable MINIGAMEMANAGER_MLF in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("MiniGameManager/Map Loader Mini Game Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MapLoaderMiniGameBridge : UnityEngine.MonoBehaviour { }
}
#endif

