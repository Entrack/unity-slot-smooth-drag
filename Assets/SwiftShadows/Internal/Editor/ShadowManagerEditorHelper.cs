using UnityEditor;
using UnityEngine;

namespace LostPolygon.SwiftShadows.Internal.Editor {
    /// <summary>
    /// Handles shadows drawing in Edit mode.
    /// </summary>
    [InitializeOnLoad]
    [ExecuteInEditMode]
    public class ShadowManagerEditorHelper : ScriptableObject {
        private static bool _isMustCreateHelper;
        private static ShadowManagerEditorHelper _instance;

        [HideInInspector]
        [SerializeField]
        private int _updateFrameCount;

        [HideInInspector]
        [SerializeField]
        private string _lastScene;

        [HideInInspector]
        [SerializeField]
        private string _lastEditorScene;

        [HideInInspector]
        [SerializeField]
        private bool _isClearedOnPlaymodeChange;

        [HideInInspector]
        [SerializeField]
        private bool _isPausedOnPlaymodeChangeLast;

        [HideInInspector]
        [SerializeField]
        private bool _isPausedOnPlaymodeChangeToPlay;

        private object _reloadMarker;

        static ShadowManagerEditorHelper() {
            _isMustCreateHelper = true;
            EditorApplication.delayCall += CreateHelper;
        }

        public static ShadowManagerEditorHelper Instance {
            get {
                return _instance;
            }
        }

        private static void CreateHelper() {
            if (!_isMustCreateHelper)
                return;

            _isMustCreateHelper = false;

            // Load ShadowManagerEditorHelper from resources and re-attach the events
            ShadowManagerEditorHelper persistentHelper = GetPersistentHelper();
            _instance = persistentHelper;
            EditorApplication.update += _instance.EditorUpdate;
            EditorApplication.playmodeStateChanged += _instance.PlaymodeStateChangedHandler;
        }

        private static ShadowManagerEditorHelper GetPersistentHelper() {
            return Resources.Load<ShadowManagerEditorHelper>("ShadowManagerEditorHelper");
        }

        private void PlaymodeStateChangedHandler() {
            if (ShadowManager.IsDestroyed)
                return;

            ShadowManager shadowManager = ShadowManager.Instance;
            if (EditorApplication.isPlaying && !EditorApplication.isPaused) {
                shadowManager.Clear(false, true);
                shadowManager.UpdateStaticShadows();
                _updateFrameCount = 1;

                _isClearedOnPlaymodeChange = false;
                _isPausedOnPlaymodeChangeToPlay = _isPausedOnPlaymodeChangeLast;
            }

            _isPausedOnPlaymodeChangeLast = EditorApplication.isPaused;
        }

        private void EditorUpdate() {
            string currentScene = Application.loadedLevelName;
            string currentEditorScene = EditorApplication.currentScene;
            
            ShadowManager shadowManager = ShadowManager.Instance;

            // Re-attach camera events and repaint when
            // some frames have passed
            if (_updateFrameCount > 0) {
                _updateFrameCount--;

                EditorApplication.delayCall += () => {
                    shadowManager.UpdateCameraEvents(Camera.allCameras);
                    shadowManager.UpdateCameraEvents(SceneView.GetAllSceneCameras());
                    shadowManager.UpdateStaticShadows();

                    SceneView.RepaintAll();
                };
            }

            // Clear before entering Play mode
            if (!_isPausedOnPlaymodeChangeToPlay &&
                !_isClearedOnPlaymodeChange && 
                _reloadMarker != null && 
                EditorApplication.isPlayingOrWillChangePlaymode && 
                !ShadowManager.IsDestroyed) {
                ShadowManager.Instance.Clear(false, true);
                ShadowManager.Instance.UpdateStaticShadows();

                _isClearedOnPlaymodeChange = true;
            }

            // When current scene has changed
            if (_reloadMarker != null && (_lastScene != currentScene || _lastEditorScene != currentEditorScene) && !ShadowManager.IsDestroyed) {
                // Destroy ShadowManager if there are no shadows in the scene
                if (ShadowManager.Instance.ShadowManagers.Count == 0) {
                    DestroyImmediate(ShadowManager.Instance.gameObject);
                } else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode) {
                    // Otherwise just clean
                    ShadowManager.Instance.Clear(false, true);
                    ShadowManager.Instance.UpdateStaticShadows();
                    EditorApplication.delayCall += () => {
                        shadowManager.UpdateCameraEvents(Camera.allCameras);
                    };
                }
            }

            if (_reloadMarker == null) {
                _reloadMarker = new object();
                SetIsMustUpdate();
            }

            _lastScene = currentScene;
            _lastEditorScene = currentEditorScene;
        }

        public void SetIsMustUpdate() {
            _updateFrameCount++;
        }
    }
}