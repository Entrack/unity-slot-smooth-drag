using UnityEngine;

namespace LostPolygon.SwiftShadows {
    /// <summary>
    /// Submits rendering the shadows for camera this component is attached to.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class ShadowsCameraEvents : MonoBehaviour {
        public const HideFlags kRuntimeHideFlags = 0;
        public const HideFlags kEditorHideFlags = HideFlags.DontSave | HideFlags.NotEditable;

        private Camera _camera;

        private void OnEnable() {
            _camera = gameObject.GetComponent<Camera>();
        }

        /// <summary>
        /// Called right before camera is going to cull the scene.
        /// </summary>
        private void OnPreCull() {
            // Only render when there is manager available
            if (!ShadowManager.IsDestroyed) {
                ShadowManager.Instance.OnCameraPreCull(_camera);
            }
        }
    }
}