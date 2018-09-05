using UnityEditor;
using UnityEngine;

namespace LostPolygon.SwiftShadows.Internal.Editor {
    /// <summary>
    /// A custom inspector for ShadowManager. Displays the shadow manager statistics.
    /// </summary>
    [CustomEditor(typeof(ShadowManager))]
    public class ShadowManagerEditor : UnityEditor.Editor {
        private const float kRepaintRate = 0.25f;
        private ShadowManager _object;
        private bool[] _foldoutState;
        private float _lastRepaintTime;

        private void OnEnable() {
            _object = target as ShadowManager;
            EditorApplication.update += Update;
        }

        private void OnDisable() {
            EditorApplication.update -= Update;
        }

        private void Update() {
            if (Time.realtimeSinceStartup - _lastRepaintTime > kRepaintRate) {
                Repaint();
                _lastRepaintTime = Time.realtimeSinceStartup;
            }
        }

        public override void OnInspectorGUI() {
            if (_object == null)
                return;

            GUILayout.Space(7f);
            GUILayout.Label("Do not delete this object to keep Swift Shadows working!");

            GUILayout.Label("Statistics:", EditorStyles.boldLabel);
            if (_object.ShadowManagers.Count == 0) {
                GUILayout.Label("No shadows are registered.");
            }

            int counter = 0;
            if (_foldoutState == null || _foldoutState.Length < _object.ShadowManagers.Count) {
                _foldoutState = new bool[_object.ShadowManagers.Count];
            }

            foreach (ShadowMeshManager meshManager in _object.ShadowManagers) {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.HelpBox("Material: " + meshManager.Material.name, MessageType.None, true);

                    EditorGUI.indentLevel++;
                    EditorGUILayout.ObjectField("Material: ", meshManager.Material, typeof(Material), false);
                    EditorGUILayout.LabelField(string.Format("Static: {0}", meshManager.IsStatic));
                    EditorGUILayout.LabelField(string.Format("Layer: {0}", LayerMask.LayerToName(meshManager.Layer)));
                    EditorGUILayout.LabelField(string.Format("Shadows total: {0}", meshManager.ShadowsCount));
                    EditorGUILayout.LabelField(string.Format("Shadows visible: {0}", meshManager.VisibleShadowsCount));
                    EditorGUILayout.ObjectField("Mesh preview: ", meshManager.Mesh, typeof(Mesh), false);

                    _foldoutState[counter] = EditorGUILayout.Foldout(_foldoutState[counter], "Shadows");

                    if (_foldoutState[counter]) {
                        EditorGUI.indentLevel++;
                        for (int i = 0; i < meshManager.ShadowsList.Count; i++) {
                            EditorGUILayout.BeginHorizontal();
                            {
                                EditorGUILayout.Toggle(meshManager.ShadowsList.Items[i].IsVisible, GUILayout.Width(16f));
                                EditorGUILayout.ObjectField("", meshManager.ShadowsList.Items[i], typeof(SwiftShadow), false);
                            }
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndVertical();

                counter++;
            }
        }
    }
}