#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MiniGameManager.Runtime;

namespace MiniGameManager.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="MiniGameManager.Runtime.MiniGameManager"/>.
    /// Shows loaded definitions, completion results, active mini-game state, and manual launch controls.
    /// </summary>
    [CustomEditor(typeof(Runtime.MiniGameManager))]
    public class MiniGameManagerEditor : UnityEditor.Editor
    {
        private string  _launchId      = string.Empty;
        private bool    _showDefs      = true;
        private bool    _showResults   = true;
        private Vector2 _defsScroll;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

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
}
#endif
