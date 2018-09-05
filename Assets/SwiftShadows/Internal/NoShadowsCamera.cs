using UnityEngine;

namespace LostPolygon.SwiftShadows {
    /// <summary>
    /// Marks camera as one that should not render shadows.
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class NoShadowsCamera : MonoBehaviour {
        private void OnEnable() {
            ShadowsCameraEvents cameraEvents = GetComponent<ShadowsCameraEvents>();
            if (cameraEvents == null)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying) {
                Destroy(cameraEvents);
            } else {
                DestroyImmediate(cameraEvents);
            }
#else
            Destroy(cameraEvents);
#endif
        }
    }
}