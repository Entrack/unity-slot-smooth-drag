#define SET_MESH_NORMALS1

using System;
using UnityEngine;
using LostPolygon.SwiftShadows.Internal;

namespace LostPolygon.SwiftShadows {
    /// <summary>
    /// Manages the mesh generated for the collection of similar shadows.
    /// </summary>
    public class ShadowMeshManager : IDisposable {
        private static readonly Matrix4x4 kMatrixIdentity = Matrix4x4.identity;
        private readonly Material _material;
        private readonly int _layer;
        private readonly int _layerMask;
        private readonly bool _isStatic;
        private readonly ExposedList<SwiftShadow> _shadowsList = new ExposedList<SwiftShadow>();
        private Mesh _mesh;
        private readonly ShadowMeshDataContainer _meshData;
        private int _visibleShadowsCount;
        private int _lastVisibleShadowsCount;
        private bool _isStaticDirty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShadowMeshManager"/> class.
        /// </summary>
        /// <param name="material">
        /// Material to use for shadows.
        /// </param>
        /// <param name="layer">
        /// Layer on which the shadows are rendered.
        /// </param>
        /// <param name="isStatic">
        /// Whether the mesh manager is not updated automatically.
        /// </param>
        public ShadowMeshManager(Material material, int layer, bool isStatic) {
            _material = material;
            _isStatic = isStatic;
            _layer = layer;
            _layerMask = 1 << _layer;

            _meshData = new ShadowMeshDataContainer();
            CreateMesh();

            _isStaticDirty = true;
        }

        public int ShadowsCount {
            get {
                return _shadowsList.Count;
            }
        }

        public int VisibleShadowsCount {
            get {
                return _visibleShadowsCount;
            }
        }

        public ExposedList<SwiftShadow> ShadowsList {
            get { 
                return _shadowsList;
            }
        }

        public bool IsStatic {
            get {
                return _isStatic;
            }
        }

        public Material Material {
            get {
                return _material;
            }
        }

        public int Layer {
            get {
                return _layer;
            }
        }

        public int LayerMask {
            get {
                return _layerMask;
            }
        }

        public Mesh Mesh {
            get {
                return _mesh;
            }
        }

        /// <summary>
        /// Frees the mesh resources.
        /// </summary>
        public void FreeMesh() {
            if (_mesh != null) {
                UnityEngine.Object.DestroyImmediate(_mesh, true);
                _mesh = null;
            }
        }

        /// <summary>
        /// Returns the numeric hash code of mesh manager.
        /// </summary>
        /// <returns>
        /// An <see cref="int"/> value representing the unique combination of material, layer, and static state.
        /// </returns>
        public int GetInstanceHashCode() {
            return CalculateMeshManagerHashCode(_isStatic, _material, _layer);
        }

        /// <summary>
        /// Registers the shadow in this manager.
        /// </summary>
        /// <param name="shadow">
        /// The shadow to register.
        /// </param>
        public void RegisterShadow(SwiftShadow shadow) {
            if (_isStatic)
                shadow.RecalculateShadow(null, true);

            _shadowsList.Add(shadow);
            _isStaticDirty = true;
        }

        /// <summary>
        /// Unregisters the shadow from this manager.
        /// </summary>
        /// <param name="shadow">
        /// The shadow to unregister.
        /// </param>
        public void UnregisterShadow(SwiftShadow shadow) {
            _shadowsList.Remove(shadow);
            _isStaticDirty = true;
        }

        /// <summary>
        /// Recalculates the geometry of every attached shadow and builds a batched mesh
        /// </summary>
        /// <param name="frustumPlanes">
        /// The frustum planes of camera that renders the scene.
        /// </param>
        public void RecalculateGeometry(Plane[] frustumPlanes) {
            RecalculateGeometry(frustumPlanes, false);
        }

        /// <summary>
        /// Forces recalculation of static shadows.
        /// </summary>
        public void ForceStaticRecalculate() {
            _isStaticDirty = true;
        }

        /// <summary>
        /// Submits rendering of the batched shadows mesh.
        /// </summary>
        public void DrawMesh(Camera camera) {
            if (_shadowsList.Count == 0 || _visibleShadowsCount == 0)
                return;

            Graphics.DrawMesh(_mesh, kMatrixIdentity, _material, _layer, camera, 0, null, false, false);
        }

        /// <summary>
        /// Allocates a new Mesh instance.
        /// </summary>
        private void CreateMesh() {
            if (_mesh != null)
                return;

            _mesh = new Mesh();
            _mesh.name = string.Format("_ShadowMesh_{0}_{1}", _isStatic ? "Static" : "Dynamic", _material.name);

            if (!_isStatic) {
                _mesh.MarkDynamic();
            }
        }

        /// <summary>
        /// Recalculates the geometry of every attached shadow and builds a batched mesh.
        /// </summary>
        /// <param name="frustumPlanes">
        /// The frustum planes of camera that renders the scene.
        /// Ignored when recalculating static shadows.
        /// </param>
        /// <param name="skipRecalculate">
        /// Whether to skip recalculation of shadow geometry.
        /// </param>
        private void RecalculateGeometry(Plane[] frustumPlanes, bool skipRecalculate) {
            // No need to rebuild static mesh when it hasn't changed
            bool mustRebuildMesh = !_isStatic || _isStaticDirty;

            int currentVisibleShadowsCount = 0;
            if (_isStatic) {
                frustumPlanes = null;
            }

            for (int i = 0, shadowCount = _shadowsList.Count; i < shadowCount; i++) {
                SwiftShadow shadow = _shadowsList.Items[i];
                if (!skipRecalculate) {
                    SwiftShadow.RecalculateShadowResult recalculateResult = shadow.RecalculateShadow(frustumPlanes, false);
                    switch (recalculateResult) {
                        case SwiftShadow.RecalculateShadowResult.ChangedManager:
                            i--;
                            shadowCount = _shadowsList.Count;
                            continue;
                        case SwiftShadow.RecalculateShadowResult.Recalculated:
                            mustRebuildMesh = true;
                            break;
                    }
                }

                if (shadow.IsVisible) {
                    currentVisibleShadowsCount++;
                }
            }

            if (!mustRebuildMesh || (_visibleShadowsCount == 0 && currentVisibleShadowsCount == 0))
                return;

            _visibleShadowsCount = currentVisibleShadowsCount;

            if (_visibleShadowsCount != 0) {
                bool isMeshNull = _mesh == null;
                if (isMeshNull) {
                    CreateMesh();
                }

                RebuildMesh(isMeshNull || _isStatic || _isStaticDirty);
                _isStaticDirty = false;
            }

            _lastVisibleShadowsCount = _visibleShadowsCount;
        }

        /// <summary>
        /// Rebuilds the batched mesh.
        /// </summary>
        /// <param name="forceRebuildTriangles">
        /// Whether to force the rebuild of vertex indices.
        /// </param>
        private void RebuildMesh(bool forceRebuildTriangles) {
            bool mustRebuildTriangles =
                forceRebuildTriangles ||
                _lastVisibleShadowsCount != _visibleShadowsCount;

            _meshData.EnsureCapacity(_visibleShadowsCount);

            Vector3 meshBoundsMin;
            meshBoundsMin.x = float.MaxValue;
            meshBoundsMin.y = float.MaxValue;
            meshBoundsMin.z = float.MaxValue;
            Vector3 meshBoundsMax;
            meshBoundsMax.x = float.MinValue;
            meshBoundsMax.y = float.MinValue;
            meshBoundsMax.z = float.MinValue;
            int index = 1;
            int triangleIndex = 0;
            for (int i = 0, shadowsCount = _shadowsList.Count; i < shadowsCount; i++) {
                SwiftShadow shadow = _shadowsList.Items[i];
                if (!shadow.IsVisible)
                    continue;

                Vector3[] shadowVertices = shadow.ShadowVertices;
#if SET_MESH_NORMALS
                Vector3 normal = shadow.Normal;
#endif
                Vector2[] textureUV = shadow.TextureUV;
                Color32 color32 = shadow.CurrentColor;

                _meshData.Vertices[index] = shadowVertices[0];
                _meshData.Vertices[index + 1] = shadowVertices[1];
                _meshData.Vertices[index + 2] = shadowVertices[2];
                _meshData.Vertices[index + 3] = shadowVertices[3];

                // Calculate min/max X
                if (shadowVertices[0].x < meshBoundsMin.x)
                    meshBoundsMin.x = shadowVertices[0].x;
                else if (shadowVertices[0].x > meshBoundsMax.x)
                    meshBoundsMax.x = shadowVertices[0].x;
                if (shadowVertices[1].x < meshBoundsMin.x)
                    meshBoundsMin.x = shadowVertices[1].x;
                else if (shadowVertices[1].x > meshBoundsMax.x)
                    meshBoundsMax.x = shadowVertices[1].x;
                if (shadowVertices[2].x < meshBoundsMin.x)
                    meshBoundsMin.x = shadowVertices[2].x;
                else if (shadowVertices[2].x > meshBoundsMax.x)
                    meshBoundsMax.x = shadowVertices[2].x;
                if (shadowVertices[3].x < meshBoundsMin.x)
                    meshBoundsMin.x = shadowVertices[3].x;
                else if (shadowVertices[3].x > meshBoundsMax.x)
                    meshBoundsMax.x = shadowVertices[3].x;

                // Calculate min/max Y
                if (shadowVertices[0].y < meshBoundsMin.y)
                    meshBoundsMin.y = shadowVertices[0].y;
                else if (shadowVertices[0].y > meshBoundsMax.y)
                    meshBoundsMax.y = shadowVertices[0].y;
                if (shadowVertices[1].y < meshBoundsMin.y)
                    meshBoundsMin.y = shadowVertices[1].y;
                else if (shadowVertices[1].y > meshBoundsMax.y)
                    meshBoundsMax.y = shadowVertices[1].y;
                if (shadowVertices[2].y < meshBoundsMin.y)
                    meshBoundsMin.y = shadowVertices[2].y;
                else if (shadowVertices[2].y > meshBoundsMax.y)
                    meshBoundsMax.y = shadowVertices[2].y;
                if (shadowVertices[3].y < meshBoundsMin.y)
                    meshBoundsMin.y = shadowVertices[3].y;
                else if (shadowVertices[3].y > meshBoundsMax.y)
                    meshBoundsMax.y = shadowVertices[3].y;

                // Calculate min/max Z
                if (shadowVertices[0].z < meshBoundsMin.z)
                    meshBoundsMin.z = shadowVertices[0].z;
                else if (shadowVertices[0].z > meshBoundsMax.z)
                    meshBoundsMax.z = shadowVertices[0].z;
                if (shadowVertices[1].z < meshBoundsMin.z)
                    meshBoundsMin.z = shadowVertices[1].z;
                else if (shadowVertices[1].z > meshBoundsMax.z)
                    meshBoundsMax.z = shadowVertices[1].z;
                if (shadowVertices[2].z < meshBoundsMin.z)
                    meshBoundsMin.z = shadowVertices[2].z;
                else if (shadowVertices[2].z > meshBoundsMax.z)
                    meshBoundsMax.z = shadowVertices[2].z;
                if (shadowVertices[3].z < meshBoundsMin.z)
                    meshBoundsMin.z = shadowVertices[3].z;
                else if (shadowVertices[3].z > meshBoundsMax.z)
                    meshBoundsMax.z = shadowVertices[3].z;

                _meshData.UV[index] = textureUV[0];
                _meshData.UV[index + 1] = textureUV[1];
                _meshData.UV[index + 2] = textureUV[2];
                _meshData.UV[index + 3] = textureUV[3];

#if SET_MESH_NORMALS
                _meshData.Normals[index] = normal;
                _meshData.Normals[index + 1] = normal;
                _meshData.Normals[index + 2] = normal;
                _meshData.Normals[index + 3] = normal;
#endif

                _meshData.Colors32[index] = color32;
                _meshData.Colors32[index + 1] = color32;
                _meshData.Colors32[index + 2] = color32;
                _meshData.Colors32[index + 3] = color32;

                if (mustRebuildTriangles) {
                    _meshData.Indices[triangleIndex] = index + 0;
                    _meshData.Indices[triangleIndex + 1] = index + 1;
                    _meshData.Indices[triangleIndex + 2] = index + 2;
                    _meshData.Indices[triangleIndex + 3] = index + 0;
                    _meshData.Indices[triangleIndex + 4] = index + 2;
                    _meshData.Indices[triangleIndex + 5] = index + 3;   
                    triangleIndex += 6;
                }

                index += 4;
            }

            _mesh.vertices = _meshData.Vertices;
#if SET_MESH_NORMALS
            _mesh.normals = _meshData.Normals;
#endif
            _mesh.uv = _meshData.UV;
            _mesh.colors32 = _meshData.Colors32;

            if (mustRebuildTriangles) {
                _mesh.triangles = _meshData.Indices;
            }

            if (meshBoundsMax.x == float.MinValue)
                meshBoundsMax.x = meshBoundsMin.x;
            if (meshBoundsMax.y == float.MinValue)
                meshBoundsMax.y = meshBoundsMin.y;
            if (meshBoundsMax.z == float.MinValue)
                meshBoundsMax.z = meshBoundsMin.z;

            Vector3 meshBoundsExtents = meshBoundsMax - meshBoundsMin;
            Vector3 meshBoundsCenter = meshBoundsMin + meshBoundsExtents * 0.5f;
            _mesh.bounds = new Bounds(meshBoundsCenter, meshBoundsExtents);
        }

        public static int CalculateMeshManagerHashCode(bool isStatic, Material material, int layer) {
            unchecked {
                const int initialHash = 17; // Prime number
                const int multiplier = 29; // Another prime number

                int hash = initialHash;
                hash = hash * multiplier + (!isStatic ? 0 : 1);
                hash = hash * multiplier + material.GetInstanceID();
                hash = hash * multiplier + layer;

                return hash;
            }
        }

        /// <summary>
        /// Shadow mesh data container that allows for easy resizing.
        /// </summary>
        private class ShadowMeshDataContainer {
            public int[] Indices;
            public Vector3[] Vertices;
#if SET_MESH_NORMALS
            public Vector3[] Normals;
#endif
            public Vector2[] UV;
            public Color32[] Colors32;

            private const int kCapacityGrowStep = 5;
            private int _capacity;

            public void EnsureCapacity(int shadowCount) {
                int numIndices = shadowCount * 6; // Two triangles per quad

                // Update indices only if we really need it
                bool mustClearIndices = Indices != null && numIndices < Indices.Length;
                if (shadowCount > _capacity) {
                    // We have to increase the arrays capacity
                    _capacity = (shadowCount / kCapacityGrowStep + 1) * kCapacityGrowStep;

                    // Two triangles per quad
                    numIndices = _capacity * 6;
                    Indices = new int[numIndices];

                    int numVertices = _capacity * 4;
                    Vertices = new Vector3[numVertices + 1];
#if SET_MESH_NORMALS
                    Normals = new Vector3[numVertices + 1];
#endif
                    UV = new Vector2[numVertices + 1];
                    Colors32 = new Color32[numVertices + 1];
                }

                if (mustClearIndices) {
                    // Apparently, Array.Clear is slower than for-loop when
                    // number of elements is less than than ~45
                    int indicesLength = Indices.Length;
                    if (indicesLength < 45) {
                        for (int i = numIndices; i < indicesLength; i++) {
                            Indices[i] = 0;
                        }
                    } else {
                        Array.Clear(Indices, numIndices, indicesLength - numIndices);
                    }
                }
            }
        }

        public void Dispose() {
            FreeMesh();
        }
    }
}