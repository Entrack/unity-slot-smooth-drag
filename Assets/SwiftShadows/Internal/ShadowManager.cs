#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE) && !ENABLE_IL2CPP
#  define SUPPORTS_REFLECTION
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using LostPolygon.SwiftShadows.Internal;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LostPolygon.SwiftShadows {
    using ShadowMeshManagerMapEnumerator = ExposedDictionary<int, ShadowMeshManager>.Enumerator;

    /// <summary>
    /// Main shadow manager class. Controls the rendering, triggers the calculation, creates and manages mesh managers.
    /// </summary>
    [ExecuteInEditMode]
    public class ShadowManager : MonoBehaviour {
        private const string kGameObjectName = "_SwiftShadowManager";

        /// <summary>
        /// An instance of the singleton.
        /// </summary>
        private static ShadowManager _instance;

        /// <summary>
        /// A value indicating whether an instance of <see cref="ShadowManager"/> is instantiated
        /// </summary>
        private static bool _isDestroyed = true;

        /// <summary>
        /// Dictionary of shadows mesh managers. The key is a numeric value, unique for a combination of IsStatic, Material, and Layer.
        /// </summary>
        private readonly ExposedDictionary<int, ShadowMeshManager> _meshManagers = new ExposedDictionary<int, ShadowMeshManager>();

        /// <summary>
        /// The list of shadows mesh managers that were added mid-calculation.
        /// </summary>
        private readonly ExposedDictionary<int, ShadowMeshManager> _meshManagersNew = new ExposedDictionary<int, ShadowMeshManager>();

        /// <summary>
        /// Maps Collider to the bounds of the attached Renderer.
        /// </summary>
        private readonly ColliderToRendererBoundsCache _colliderToRendererBoundsCache = new ColliderToRendererBoundsCache();

        private Plane[] _cameraFrustumPlanes = new Plane[6];

        /// <summary>
        /// Whether the shadow recalculation is going on right now.
        /// </summary>
        private bool _isRecalculatingMesh;

        /// <summary>
        /// Prevents a default instance of the <see cref="ShadowManager"/> class from being created.
        /// </summary>
        private ShadowManager() {
        }

        /// <summary>
        /// Gets a value indicating whether an instance of <see cref="ShadowManager"/> is instantiated.
        /// </summary>
        public static bool IsDestroyed {
            get {
                return _isDestroyed;
            }
        }

        /// <summary>
        /// Gets the instance of <see cref="ShadowManager"/>, creating one if needed.
        /// </summary>
        public static ShadowManager Instance {
            get {
                if (_instance == null) {
                    // Trying to find an existing instance in the scene
                    _instance = FindObjectOfType<ShadowManager>();

                    // Creating a new instance in case there are no instances present in the scene
                    if (_instance == null) {
                        GameObject shadowManagerGameObject = new GameObject(kGameObjectName);
                        _instance = shadowManagerGameObject.AddComponent<ShadowManager>();
                        _isDestroyed = false;

                        shadowManagerGameObject.hideFlags = HideFlags.HideInHierarchy;
#if UNITY_EDITOR
                        if (!Application.isPlaying) {
                            shadowManagerGameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
                        }
#endif
                    }
                }

#if UNITY_EDITOR
                if (Application.isPlaying) {
                    _instance.gameObject.hideFlags &= ~HideFlags.HideInHierarchy;
                } else {
                    _instance.gameObject.hideFlags |= HideFlags.HideInHierarchy;
                }
#endif

                return _instance;
            }
        }

        /// <summary>
        /// Gets the list of shadow mesh managers.
        /// </summary>
        public ICollection<ShadowMeshManager> ShadowManagers {
            get {
                return _meshManagers.Values;
            }
        }


        /// <summary>
        /// Gets a helper that maps a Collider to the Bounds of a Renderer attached to the same GameObject as Renderer.
        /// </summary>
        public ColliderToRendererBoundsCache ColliderToRendererBoundsCache {
            get {
                return _colliderToRendererBoundsCache;
            }
        }

        /// <summary>
        /// Attaches ShadowsCameraEvents to every enabled cameras, if needed.
        /// </summary>
        public void UpdateCameraEvents() {
            UpdateCameraEvents(Camera.allCameras);
        }

        /// <summary>
        /// Attaches ShadowsCameraEvents to <paramref name="camera"/>.
        /// </summary>
        /// <param name="camera">
        /// The camera to attach the ShadowsCameraEvents component.
        /// </param>
        public void UpdateCameraEvents(Camera camera) {
            UpdateCameraEvents(camera, Application.isPlaying ? ShadowsCameraEvents.kRuntimeHideFlags : ShadowsCameraEvents.kEditorHideFlags);
        }

        /// <summary>
        /// Attaches ShadowsCameraEvents to <paramref name="camera"/>.
        /// </summary>
        /// <param name="camera">
        /// The camera to attach the ShadowsCameraEvents component.
        /// </param>
        /// <param name="hideFlags">
        /// <see cref="HideFlags"/> to apply to <paramref name="camera"/>.
        /// </param>
        public void UpdateCameraEvents(Camera camera, HideFlags hideFlags) {
            NoShadowsCamera noShadowsCamera = camera.GetComponent<NoShadowsCamera>();
            if (noShadowsCamera != null)
                return;

            ShadowsCameraEvents cameraEvents = camera.GetComponent<ShadowsCameraEvents>();
            if (cameraEvents == null) {
                cameraEvents = camera.gameObject.AddComponent<ShadowsCameraEvents>();
            }

            cameraEvents.hideFlags = hideFlags;
        }

        /// <summary>
        /// Attaches ShadowsCameraEvents to each camera in <paramref name="cameras"/>.
        /// </summary>
        /// <param name="cameras">
        /// Cameras to attach the ShadowsCameraEvents component.
        /// </param>
        public void UpdateCameraEvents(Camera[] cameras) {
            UpdateCameraEvents(cameras, Application.isPlaying ? ShadowsCameraEvents.kRuntimeHideFlags : ShadowsCameraEvents.kEditorHideFlags);
        }

        /// <summary>
        /// Attaches ShadowsCameraEvents to each camera in <paramref name="cameras"/>.
        /// </summary>.
        /// <param name="cameras">
        /// Cameras to attach the ShadowsCameraEvents component.
        /// </param>
        /// <param name="hideFlags">
        /// <see cref="HideFlags"/> to apply to each camera in <paramref name="cameras"/>.
        /// </param>
        public void UpdateCameraEvents(Camera[] cameras, HideFlags hideFlags) {
            for (int i = 0; i < cameras.Length; i++) {
                Camera camera = cameras[i];
                UpdateCameraEvents(camera, hideFlags);
            }
        }

        /// <summary>
        /// Removes ShadowsCameraEvents from the camera.
        /// </summary>
        /// <param name="cameras">
        /// Cameras to remove the ShadowsCameraEvents component from.
        /// </param>
        public void RemoveCameraEvents(Camera[] cameras) {
            for (int i = 0; i < cameras.Length; i++) {
                ShadowsCameraEvents cameraEvents = cameras[i].GetComponent<ShadowsCameraEvents>();
                if (cameraEvents != null) {
#if UNITY_EDITOR
                    DestroyImmediate(cameraEvents);
#else
                    Destroy(cameraEvents);
#endif
                }
            }
        }

        /// <summary>
        /// Submits updating and rendering all shadows that are in seen by <paramref name="camera"/>.
        /// �alled by camera on OnPreCull event.
        /// </summary>
        /// <param name="camera">
        /// The camera to render the shadows.
        /// </param>
        public void OnCameraPreCull(Camera camera) {
            Render(camera);
        }

        /// <summary>
        /// Forces the recalculation of static shadows.
        /// </summary>
        public void UpdateStaticShadows() {
            ShadowMeshManagerMapEnumerator meshManagersEnumerator = new ShadowMeshManagerMapEnumerator(_meshManagers);
            while (meshManagersEnumerator.MoveNext()) {
                ShadowMeshManager meshManager = meshManagersEnumerator.CurrentValue;
                if (meshManager.IsStatic) {
                    meshManager.ForceStaticRecalculate();
                }
            }
        }

        /// <summary>
        /// Registers the shadow.
        /// </summary>
        /// <param name="shadow">
        /// The shadow to register.
        /// </param>
        public void RegisterShadow(SwiftShadow shadow) {
            GetMeshManager(shadow).RegisterShadow(shadow);
        }

        /// <summary>
        /// Unregisters the shadow.
        /// </summary>
        /// <param name="shadow">
        /// The shadow to unregister.
        /// </param>
        public void UnregisterShadow(SwiftShadow shadow) {
            ShadowMeshManager meshManager;
            int shadowHash = shadow.GetMeshManagerHashCode();
            if (_meshManagers.TryGetValue(shadowHash, out meshManager)) {
                meshManager.UnregisterShadow(shadow);
            }
        }

        /// <summary>
        /// Clears the manager state and all allocated resources.
        /// </summary>
        public void Clear() {
            Clear(true, true);
        }

        /// <summary>
        /// Clears the manager state and all allocated resources.
        /// </summary>
        public void Clear(bool isClearManagers) {
            Clear(isClearManagers, true);
        }

        /// <summary>
        /// Clears the manager and all allocated resources.
        /// </summary>
        public void Clear(bool isClearManagers, bool isClearCameras) {
            foreach (ShadowMeshManager meshManager in _meshManagers.Values) {
                meshManager.FreeMesh();
            }

            foreach (ShadowMeshManager meshManager in _meshManagersNew.Values) {
                meshManager.FreeMesh();
            }

            if (isClearManagers) {
                _meshManagers.Clear();
                _meshManagersNew.Clear();
            }

            if (isClearCameras) {
                RemoveCameraEvents(Camera.allCameras);
            }
        }

        /// <summary>
        /// Sets up the manager.
        /// </summary>
        private void Initialize() {
            _instance = this;
            _isDestroyed = false;
#if UNITY_EDITOR
            foreach (ShadowMeshManager meshManager in _meshManagers.Values) {
                meshManager.RecalculateGeometry(null);
            }
#endif
            UpdateCameraEvents();
        }

        /// <summary>
        /// Renders the shadows to a camera
        /// </summary>
        /// <param name="camera">
        /// The camera to render shadows on.
        /// </param>
        private void Render(Camera camera) {
            // Calculate the camera frustum planes
            int cameraCullingMask;
            if (camera != null) {
#if SUPPORTS_REFLECTION
                GeometryUtilityInternal.CalculateFrustumPlanes(_cameraFrustumPlanes, camera);
#else
                _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
#endif
                cameraCullingMask = camera.cullingMask;
            } else {
                cameraCullingMask = unchecked(~0);
            }

            _isRecalculatingMesh = true;

            // Clearing list of managers added on previous recalculations
            _meshManagersNew.Clear();

            // Loop known mesh managers, we don't care if new items will be added inside the loop
            ShadowMeshManagerMapEnumerator meshManagersEnumerator = new ShadowMeshManagerMapEnumerator(_meshManagers);
            while (meshManagersEnumerator.MoveNext()) {
                ShadowMeshManager meshManager = meshManagersEnumerator.CurrentValue;
                UpdateAndDrawMeshManager(meshManager, camera, cameraCullingMask);
            }

            _isRecalculatingMesh = false;

            // Calculate & draw the newly added managers (if AutoStatic is used)
            // and add them to the actual list
            ShadowMeshManagerMapEnumerator meshManagersNewEnumerator = new ShadowMeshManagerMapEnumerator(_meshManagersNew);
            while (meshManagersNewEnumerator.MoveNext()) {
                ShadowMeshManager meshManager = meshManagersNewEnumerator.CurrentValue;
                UpdateAndDrawMeshManager(meshManager, camera, cameraCullingMask);

                _meshManagers.Add(meshManager.GetInstanceHashCode(), meshManager);
            }
        }

        private void UpdateAndDrawMeshManager(ShadowMeshManager meshManager, Camera camera, int cameraCullingMask) {
            // Skip calculating and rendering shadows that are on 
            // layers culled by the camera
            if ((cameraCullingMask & meshManager.LayerMask) == 0)
                return;

            meshManager.RecalculateGeometry(camera != null ? _cameraFrustumPlanes : null);
            meshManager.DrawMesh(camera);
        }

        /// <summary>
        /// Returns the mesh managers suitable for a shadow.
        /// </summary>
        /// <param name="shadow">
        /// The shadow to get mesh manager for.
        /// </param>
        /// <returns>
        /// The <see cref="ShadowMeshManager"/>.
        /// </returns>
        private ShadowMeshManager GetMeshManager(SwiftShadow shadow) {
            ShadowMeshManager meshManager;
            if (_meshManagers.TryGetValue(shadow.GetMeshManagerHashCode(), out meshManager))
                return meshManager;

            if (shadow.Material == null)
                throw new ArgumentNullException("shadow.material");

            if (_meshManagersNew.TryGetValue(shadow.GetMeshManagerHashCode(), out meshManager))
                return meshManager;

            meshManager = new ShadowMeshManager(shadow.Material, shadow.Layer, shadow.IsStatic);
            if (_isRecalculatingMesh) {
                _meshManagersNew.Add(meshManager.GetInstanceHashCode(), meshManager);
            } else {
                _meshManagers.Add(meshManager.GetInstanceHashCode(), meshManager);
            }

            return meshManager;
        }

        #region Unity methods

        private void LateUpdate() {
            _colliderToRendererBoundsCache.Clear();

#if UNITY_EDITOR
            UnityEngine.Profiling.Profiler.BeginSample("Editor-only allocations");
            UpdateCameraEvents(SceneView.GetAllSceneCameras(), HideFlags.DontSave);
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }

#if UNITY_EDITOR
        private void Update() {
            if (!EditorApplication.isPaused)
                return;

            OnCameraPreCull(null);
        }
#endif

        private void OnEnable() {
            // Kill other instances. First try FindObjectsOfType, then kill other singleton instance
            // For some reason, FindObjectsOfType ignores objects with non-standard HideFlags
#if UNITY_EDITOR
            int maxInstances = (gameObject.hideFlags & HideFlags.DontSave) != 0 ? 0 : 1;
#else
            const int maxInstances = 1;
#endif
            if (FindObjectsOfType<ShadowManager>().Length > maxInstances) {
                DestroyImmediate(gameObject);
                return;
            }

            if (_instance != null && _instance != this) {
                DestroyImmediate(_instance.gameObject);
            }

            // Set yourself to be the active singleton instance
            _instance = this;
            _isDestroyed = false;

            Initialize();
#if UNITY_EDITOR
            UpdateCameraEvents(SceneView.GetAllSceneCameras(), HideFlags.DontSave);
#endif
        }

        private void OnDestroy() {
            if (_instance == this) {
                _instance = null;
                _isDestroyed = true;
            }

            Clear();
        }

        private void OnLevelWasLoaded(int level) {
            UpdateCameraEvents();

            // Remove empty mesh managers
            ExposedList<ShadowMeshManager> emptyShadowMeshManagers = new ExposedList<ShadowMeshManager>();
            ShadowMeshManagerMapEnumerator meshManagersEnumerator = new ShadowMeshManagerMapEnumerator(_meshManagers);
            while (meshManagersEnumerator.MoveNext()) {
                ShadowMeshManager meshManager = meshManagersEnumerator.CurrentValue;
                if (meshManager.ShadowsCount == 0) {
                     emptyShadowMeshManagers.Add(meshManager);
                }
            }

            for (int i = 0; i < emptyShadowMeshManagers.Count; i++) {
                _meshManagers.Remove(emptyShadowMeshManagers.Items[i].GetInstanceHashCode());
            }
        }

        #endregion

    }
}