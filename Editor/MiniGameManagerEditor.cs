#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using MiniGameManager.Runtime;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace MiniGameManager.Editor
{
    // ODIN Inspector: when ODIN_INSPECTOR is defined the manager extends
    // SerializedMonoBehaviour. The custom editor must derive from OdinEditor
    // so ODIN's full property tree is drawn correctly.
#if ODIN_INSPECTOR
    [CustomEditor(typeof(Runtime.MiniGameManager))]
    public class MiniGameManagerEditor : OdinEditor
#else
    [CustomEditor(typeof(Runtime.MiniGameManager))]
    public class MiniGameManagerEditor : UnityEditor.Editor
#endif
    {
        private string  _launchId      = string.Empty;
        private bool    _showDefs      = true;
        private bool    _showResults   = true;
        private Vector2 _defsScroll;

        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI() renders the full ODIN property tree when
            // inheriting from OdinEditor, or the standard Unity inspector otherwise.
            base.OnInspectorGUI();

            var mgr = (Runtime.MiniGameManager)target;

            EditorGUILayout.Space(8);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect mini-games.", MessageType.Info);
                return;
            }

            // ── Active state ─────────────────────────────────────────────────────
            if (mgr.IsPlaying)
                EditorGUILayout.HelpBox($"Active: {mgr.ActiveMiniGameId}", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("No mini-game active.", MessageType.None);

            EditorGUILayout.Space(4);

            // ── Loaded definitions ────────────────────────────────────────────────
            var all = mgr.GetAllMiniGames();
            _showDefs = EditorGUILayout.Foldout(_showDefs,
                $"Loaded Definitions ({all.Count})", true, EditorStyles.foldoutHeader);
            if (_showDefs)
            {
                if (all.Count == 0)
                {
                    EditorGUILayout.HelpBox("No definitions loaded. Place JSON files in Resources/MiniGames/.", MessageType.None);
                }
                else
                {
                    _defsScroll = EditorGUILayout.BeginScrollView(_defsScroll, GUILayout.MaxHeight(140));
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("ID",       EditorStyles.miniLabel, GUILayout.MinWidth(140));
                    EditorGUILayout.LabelField("Category", EditorStyles.miniLabel, GUILayout.Width(90));
                    EditorGUILayout.LabelField("Replay",   EditorStyles.miniLabel, GUILayout.Width(50));
                    EditorGUILayout.LabelField("Done",     EditorStyles.miniLabel, GUILayout.Width(40));
                    EditorGUILayout.EndHorizontal();

                    foreach (var kv in all)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kv.Key,                               GUILayout.MinWidth(140));
                        EditorGUILayout.LabelField(kv.Value.category.ToString(),         GUILayout.Width(90));
                        EditorGUILayout.LabelField(kv.Value.canReplay ? "yes" : "no",    GUILayout.Width(50));
                        EditorGUILayout.LabelField(mgr.HasCompleted(kv.Key) ? "✓" : "",  GUILayout.Width(40));
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.Space(4);

            // ── Results ───────────────────────────────────────────────────────────
            var results = mgr.GetAllResults();
            _showResults = EditorGUILayout.Foldout(_showResults,
                $"Results ({results.Count})", true, EditorStyles.foldoutHeader);
            if (_showResults && results.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("ID",    EditorStyles.miniLabel, GUILayout.MinWidth(140));
                EditorGUILayout.LabelField("Score", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                foreach (var kv in results)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(kv.Key,                    GUILayout.MinWidth(140));
                    EditorGUILayout.LabelField(kv.Value.score.ToString(), GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // ── Manual controls ───────────────────────────────────────────────────
            EditorGUILayout.LabelField("Manual Controls", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            _launchId = EditorGUILayout.TextField("Mini-Game ID", _launchId);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(_launchId) && !mgr.IsPlaying;
            if (GUILayout.Button("Launch")) mgr.Launch(_launchId);

            GUI.enabled = mgr.IsPlaying;
            if (GUILayout.Button("Complete (0)")) mgr.Complete(mgr.ActiveMiniGameId, 0);
            if (GUILayout.Button("Abort"))        mgr.Abort(mgr.ActiveMiniGameId);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            Repaint();
        }
    }

    // ── Prefab generation ──────────────────────────────────────────────────────

    /// <summary>
    /// Generates MiniGame trigger prefabs from Resources/MiniGames/*.json.
    /// Lives in MiniGameManager.Editor so MiniGameData and MiniGameTrigger are
    /// directly accessible without a separate assembly dependency.
    /// </summary>
    internal static class MiniGamePrefabHelper
    {
        internal const string MiniGamesPrefix = "Assets/Resources/MiniGames/";
        private  const string OutDir          = "Assets/Resources/Prefabs/Items";

        [MenuItem("Generate Prefabs/MiniGames", priority = 103)]
        public static void GenerateMiniGamePrefabs()
        {
            var dir = Path.Combine(Application.dataPath, "Resources", "MiniGames");
            if (!Directory.Exists(dir)) { Debug.LogError("[MiniGamePrefabHelper] Resources/MiniGames not found."); return; }

            EnsureDirectory(OutDir);

            int n = 0;
            foreach (var file in Directory.GetFiles(dir, "*.json"))
            {
                var data = JsonUtility.FromJson<MiniGameData>(File.ReadAllText(file));
                if (data == null || string.IsNullOrEmpty(data.id)) continue;

                var go = BuildMiniGameGo(data);
                var fname = !string.IsNullOrEmpty(data.sceneOrPrefab) ? data.sceneOrPrefab : data.id;
                SavePrefab(go, $"{OutDir}/{fname}.prefab");
                UnityEngine.Object.DestroyImmediate(go);
                n++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MiniGamePrefabHelper] Generated {n} MiniGame prefabs \u2192 {OutDir}");
        }

        private static GameObject BuildMiniGameGo(MiniGameData data)
        {
            var go = new GameObject(data.sceneOrPrefab ?? data.id);

            // MiniGameTrigger fields are [SerializeField] private — use SerializedObject.
            var trigger = go.AddComponent<MiniGameTrigger>();
            var so = new SerializedObject(trigger);
            so.FindProperty("miniGameId").stringValue        = data.id;
            so.FindProperty("triggerMode").enumValueIndex    = 0; // OnStart
            so.FindProperty("triggerTag").stringValue        = "Player";
            so.FindProperty("disableAfterTrigger").boolValue = true;
            so.ApplyModifiedProperties();

            go.AddComponent<AudioSource>().playOnAwake = false;

            return go;
        }

        private static void SavePrefab(GameObject go, string assetPath)
        {
            bool ex = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null;
            PrefabUtility.SaveAsPrefabAsset(go, assetPath, out bool ok);
            if (!ok) Debug.LogWarning($"[PrefabGen] \u2717 {assetPath}");
            else     Debug.Log(ex ? $"[PrefabGen] \u21ba {assetPath}" : $"[PrefabGen] \u2713 {assetPath}");
        }

        private static void EnsureDirectory(string assetPath)
        {
            Directory.CreateDirectory(Path.Combine(
                Path.GetDirectoryName(Application.dataPath)!,
                assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        [UnityEditor.InitializeOnLoadMethod]
        static void RegisterWithPrefabGenerator()
        {
            PrefabGenerator.Register("MiniGames", GenerateMiniGamePrefabs);
        }
    }

    internal class MiniGamePrefabPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var p in imported)
            {
                if (p.StartsWith(MiniGamePrefabHelper.MiniGamesPrefix) && p.EndsWith(".json"))
                {
                    MiniGamePrefabHelper.GenerateMiniGamePrefabs();
                    return;
                }
            }
        }
    }
}
#endif
