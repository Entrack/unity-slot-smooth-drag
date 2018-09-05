using System;
using UnityEngine;
using LostPolygon.SwiftShadows.Internal;

namespace LostPolygon.SwiftShadows {
    /// <summary>
    /// Main Swift Shadows component.
    /// </summary>
    [AddComponentMenu("Lost Polygon/Swift Shadow")]
    [ExecuteInEditMode]
    public class SwiftShadow : MonoBehaviour {
        /// <summary>
        /// Shadow projection vertices.
        /// </summary>
        private readonly Vector3[] _baseVertices = new Vector3[4];

        /// <summary>
        /// Vertices of shadow quad.
        /// </summary>
        private readonly Vector3[] _shadowVertices = new Vector3[4];

        /// <summary>
        /// Texture UV coordinates.
        /// </summary>
        private readonly Vector2[] _textureUV = new Vector2[4];

        /// <summary>
        /// Amount of time passed since last going to non-static state.
        /// </summary>
        private float _autoStaticTimeCounter;

        /// <summary>
        /// The initial alpha of shadow.
        /// </summary>
        private float _initialAlpha;

        /// <summary>
        /// Whether the shadow is destroyed already.
        /// </summary>
        private bool _isDestroyed;

        /// <summary>
        /// Whether this is the first calculation of the shadow.
        /// </summary>
        private bool _isFirstCalculation;

        /// <summary>
        /// Whether the owner GameObject is active.
        /// </summary>
        private bool _isGameObjectActivePrev;

        /// <summary>
        /// Whether the shadow is initialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Whether the shadow is visible at the last recalculation.
        /// </summary>
        private bool _isVisible;

        /// <summary>
        /// Current surface normal
        /// </summary>
        private Vector3 _normal;

        /// <summary>
        /// Cached object transform.
        /// </summary>
        private Transform _transform;

        /// <summary>
        /// Previous value of transform.forward.
        /// </summary>
        private Vector3 _transformForwardPrev;

        /// <summary>
        /// Previous value of transform.position.
        /// </summary>
        private Vector3 _transformPositionPrev;

        /// <summary>
        /// Cached color in a more fast format.
        /// </summary>
        private Color32 _color32 = new Color32(0, 0, 0, 200);

        /// <summary>
        /// Cached layer mask.
        /// </summary>
        private int _layerMaskInt;

        /// <summary>
        /// Whether the Light attached to the LightSourceObject
        /// is directional.
        /// </summary>
        private bool _lightSourceObjectIsDirectionalLight;

        /// <summary>
        /// Last calculated light direction vector.
        /// </summary>
        private Vector3 _actualLightVectorPrev;

        /// <summary>
        /// Used for detecting the assembly recompile event.
        /// </summary>
        private object _recompileMarker;

        /// <summary>
        /// Cached Vector3.forward.
        /// </summary>
        private static readonly Vector3 kVector3Forward = Vector3.forward;

        /// <summary>
        /// Cached Vector3.up.
        /// </summary>
        private static readonly Vector3 kVector3Up = Vector3.up;

        /// <summary>
        /// The version of this component. Could be used in future to manage updates.
        /// </summary>
#pragma warning disable 0414
        [SerializeField]
        [HideInInspector]
        private int _componentVersion = 2;
#pragma warning restore 0414

        #region Property backing fields

        [SerializeField]
        private Color _color = new Color(0f, 0f, 0f, 1f);

        [SerializeField]
        private float _angleFadeMax = 70f;

        [SerializeField]
        private float _aspectRatio = 1f;

        [SerializeField]
        private float _angleFadeMin = 60f;

        [SerializeField]
        private float _autoStaticTime;

        [SerializeField]
        private float _fadeDistance = 5f;

        [SerializeField]
        private int _forceLayer;

        [SerializeField]
        private Vector3 _lightVector = new Vector3(0f, -1f, 0f);

        [SerializeField]
        private LightVectorSourceEnum _lightVectorSource = LightVectorSourceEnum.StaticVector;

        [SerializeField]
        private Transform _lightSourceObject;

        [SerializeField]
        private float _projectionDistance = 20f;

        [SerializeField]
        private float _shadowOffset = 0.01f;

        [SerializeField]
        private float _shadowSize = 1f;

        [SerializeField]
        private float _shadowSizeScaleStartDistance = 3f;

        [SerializeField]
        private float _shadowSizeScaleEndDistance = 7f;

        [SerializeField]
        private float _shadowSizeEndScale = 1.5f;

        [SerializeField]
        private bool _isSmoothRotation = false;

        [SerializeField]
        private float _smoothRotationSpeed = 2f;

        [SerializeField]
        private Material _material;

        [SerializeField]
        private Sprite _sprite;

        [SerializeField]
        private Rect _textureUVRect = new Rect(0f, 0f, 1f, 1f);

        [SerializeField]
        private LayerMask _layerMask = unchecked(~0);

        [SerializeField]
        private bool _useForceLayer;

        [SerializeField]
        private bool _isPerspectiveProjection;

        [SerializeField]
        private bool _cullInvisible = true;

        [SerializeField]
        private bool _isStatic;

        #endregion

        #region Properties

        public bool IsVisible {
            get {
                return _isVisible;
            }
        }

        public Vector3 Normal {
            get {
                return _normal;
            }
        }

        public Color32 CurrentColor {
            get {
                return _color32;
            }
        }

        public Color32 InitialColor {
            get {
                return _color;
            }

            set {
                _color = value;
                _color32 = value;
                _initialAlpha = _color.a;

                if (
#if UNITY_EDITOR
                    Application.isPlaying &&
#endif
                    _material != null) {
                    _material.color = value;
                }
            }
        }

        public Vector2[] TextureUV {
            get {
                return _textureUV;
            }
        }

        public Rect TextureUVRect {
            get {
                return _textureUVRect;
            }

            set {
                value.xMin = Mathf.Clamp(value.xMin, 0f, 1f);
                value.yMin = Mathf.Clamp(value.yMin, 0f, 1f);
                value.xMax = Mathf.Clamp(value.xMax, 0f, 1f);
                value.yMax = Mathf.Clamp(value.yMax, 0f, 1f);
                _textureUVRect = value;
                UpdateTextureUV();
            }
        }

        public Sprite Sprite {
            get {
                return _sprite;
            }


            set {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (_sprite == value)
                    return;

                _sprite = value;
                UpdateTextureUV();
            }
        }

        public Material Material {
            get {
                return _material;
            }

            set {
                if (_material != value) {
                    UnregisterShadow();
                    _material = value;
                    RegisterShadow();
                }
            }
        }

        public LayerMask LayerMask {
            get {
                return _layerMask;
            }

            set {
                if (_layerMask != value) {
                    UnregisterShadow();
                    _layerMask = value;
                    _layerMaskInt = value;
                    RegisterShadow();
                }
            }
        }

        public bool IsStatic {
            get {
                return _isStatic;
            }

            set {
                if (_isStatic != value) {
                    UnregisterShadow();
                    _isStatic = value;
                    RegisterShadow();
                }
            }
        }

        public LightVectorSourceEnum LightVectorSource {
            get {
                return _lightVectorSource;
            }

            set {
                _lightVectorSource = value;
            }
        }

        public Vector3 LightVector {
            get {
                return _lightVector;
            }

            set {
                _lightVector = value.FastNormalized();
            }
        }

        public Transform LightSourceObject {
            get {
                return _lightSourceObject;
            }

            set {
                if (_lightSourceObject != value) {
                    _lightSourceObject = value;

                    UpdateDirectionalLight();
                }
            }
        }

        public float ShadowSize {
            get {
                return _shadowSize;
            }

            set {
                _shadowSize = Mathf.Max(0f, value);
            }
        }

        public float SizeScaleStartDistance {
            get {
                return _shadowSizeScaleStartDistance;
            }
            set {
                _shadowSizeScaleStartDistance = Mathf.Max(0f, Mathf.Min(value, _shadowSizeScaleEndDistance));
            }
        }

        public float SizeScaleEndDistance {
            get {
                return _shadowSizeScaleEndDistance;
            }
            set {
                _shadowSizeScaleEndDistance = Mathf.Max(_shadowSizeScaleStartDistance, value);
            }
        }

        public float SizeEndScale {
            get {
                return _shadowSizeEndScale;
            }
            set {
                _shadowSizeEndScale = Mathf.Max(0f, value);
            }
        }

        public float ShadowOffset {
            get {
                return _shadowOffset;
            }

            set {
                _shadowOffset = Mathf.Max(0f, value);
            }
        }

        public float ProjectionDistance {
            get {
                return _projectionDistance;
            }

            set {
                _projectionDistance = Mathf.Max(0f, value);
                _fadeDistance = Mathf.Clamp(value, 0f, _projectionDistance);
            }
        }

        public float FadeDistance {
            get {
                return _fadeDistance;
            }

            set {
                _fadeDistance = Mathf.Clamp(value, 0f, _projectionDistance);
            }
        }

        public bool IsPerspectiveProjection {
            get {
                return _isPerspectiveProjection;
            }

            set {
                _isPerspectiveProjection = value;
            }
        }

        public float AutoStaticTime {
            get {
                return _autoStaticTime;
            }

            set {
                if (_autoStaticTime != value) {
                    _autoStaticTime = Mathf.Max(0f, value);
                    _autoStaticTimeCounter = 0;
                }
            }
        }

        public float AngleFadeMin {
            get {
                return _angleFadeMin;
            }

            set {
                _angleFadeMin = Mathf.Clamp(value, 0f, 90f);
            }
        }

        public float AngleFadeMax {
            get {
                return _angleFadeMax;
            }

            set {
                _angleFadeMax = Mathf.Clamp(value, 0f, 90f);
            }
        }

        public float AspectRatio {
            get {
                return _aspectRatio;
            }

            set {
                _aspectRatio = Mathf.Max(0f, value);
            }
        }

        public Vector3[] ShadowVertices {
            get {
                return _shadowVertices;
            }
        }

        public bool CullInvisible {
            get {
                return _cullInvisible;
            }

            set {
                _cullInvisible = value;
            }
        }

        public int Layer {
            get {
                return _useForceLayer ? _forceLayer : gameObject.layer;
            }
        }

        public bool UseForceLayer {
            get {
                return _useForceLayer;
            }

            set {
                if (_useForceLayer != value) {
                    UnregisterShadow();
                    _useForceLayer = value;
                    RegisterShadow();
                }
            }
        }

        public int ForceLayer {
            get {
                return _forceLayer;
            }

            set {
                if (_forceLayer != value) {
                    UnregisterShadow();
                    _forceLayer = value;
                    RegisterShadow();
                }
            }
        }

        public bool IsSmoothRotation {
            get {
                return _isSmoothRotation;
            }
            set {
                _isSmoothRotation = value;
            }
        }

        public float SmoothRotationSpeed {
            get {
                return _smoothRotationSpeed;
            }
            set {
                _smoothRotationSpeed = Mathf.Max(0f, value);
            }
        }

        #endregion

        /// <summary>
        /// Registers the shadow in the manager.
        /// </summary>
        public void RegisterShadow() {
            ShadowManager.Instance.RegisterShadow(this);
        }

        /// <summary>
        /// Unregister the shadow from the manager.
        /// </summary>
        public void UnregisterShadow() {
            if (!ShadowManager.IsDestroyed) {
                ShadowManager.Instance.UnregisterShadow(this);
            }
        }

        /// <summary>
        /// Updates the shadow transformation and state.
        /// </summary>
        /// <param name="frustumPlanes">
        /// The frustum planes used for culling the shadow.
        /// </param>
        /// <param name="force">
        /// Whether to force the update, even if it is not needed.
        /// </param>
        /// <returns>
        /// The <see cref="RecalculateShadowResult"/> indicating the 
        /// shadow behaviour change during calculation.
        /// </returns>
        public RecalculateShadowResult RecalculateShadow(Plane[] frustumPlanes, bool force) {
#if UNITY_EDITOR
            if (_isDestroyed)
                return RecalculateShadowResult.Skipped;
#endif
            _isVisible = _isStatic;

            // Determine whether the owner GameObject changed the active state
            // and react correspondingly
            bool isGameObjectActive = gameObject.activeInHierarchy;
            if (isGameObjectActive != _isGameObjectActivePrev) {
                _isGameObjectActivePrev = isGameObjectActive;
                if (isGameObjectActive) {
                    RegisterShadow();
                    return RecalculateShadowResult.ChangedManager;
                }

                UnregisterShadow();
                return RecalculateShadowResult.ChangedManager;
            }

            _isGameObjectActivePrev = isGameObjectActive;
            if (!isGameObjectActive)
                return RecalculateShadowResult.Skipped;

            // Updating the transform state (position and forward vectors)
            // Determine whether the transform has moved
            Vector3 transformPosition = _transform.position;
            Quaternion transformRotation = _transform.rotation;
            Vector3 transformForward = transformRotation * kVector3Forward;

            bool transformChanged = false;
            if (_autoStaticTime > 0f) {
                if (transformPosition.x != _transformPositionPrev.x ||
                    transformPosition.y != _transformPositionPrev.y ||
                    transformPosition.z != _transformPositionPrev.z ||
                    transformForward.x != _transformForwardPrev.x ||
                    transformForward.y != _transformForwardPrev.y ||
                    transformForward.z != _transformForwardPrev.z
                    ) {
                    _autoStaticTimeCounter = 0f;
                    transformChanged = true;
                }

                _transformPositionPrev = transformPosition;
                _transformForwardPrev = transformForward;
            }

            float deltaTime = Time.deltaTime;

            if (!_isFirstCalculation) {
                // If we have AutoStatic
#if UNITY_EDITOR
                if (Application.isPlaying) {
#endif
                    if (_autoStaticTime > 0f) {
                        // If the object has moved - remove the shadow
                        // from static manager and move to non-static
                        if (_isStatic && transformChanged) {
                            UnregisterShadow();
                            _isStatic = false;
                            RegisterShadow();
                            return RecalculateShadowResult.ChangedManager;
                        }

                        // If the object hasn't moved for AutoStaticTime seconds,
                        // then mark it as static
                        _autoStaticTimeCounter += deltaTime;
                        if (!_isStatic && _autoStaticTimeCounter > _autoStaticTime) {
                            UnregisterShadow();
                            _isStatic = true;
                            RegisterShadow();
                            return RecalculateShadowResult.ChangedManager;
                        }
                    }

                    // Do not update static shadows by default
                    if (_isStatic && !force) {
                        return RecalculateShadowResult.Skipped;
                    }
#if UNITY_EDITOR
                }
#endif
            }

            // Is this our first update?
            _isFirstCalculation = false;

            // Determine the light source position
            bool useLightSource = _lightVectorSource == LightVectorSourceEnum.GameObject && _lightSourceObject != null;
            Vector3 lightSourcePosition;
            if (useLightSource) {
                lightSourcePosition = _lightSourceObject.position;
            } else {
                lightSourcePosition.x = 0f;
                lightSourcePosition.y = 0f;
                lightSourcePosition.z = 0f;
            }

            // The actual light direction vector that'll be used
            Vector3 actualLightVector;
            if (useLightSource) {
                if (_lightSourceObjectIsDirectionalLight) {
                    actualLightVector = _lightSourceObject.rotation * kVector3Forward;
                } else {
                    actualLightVector.x = transformPosition.x - lightSourcePosition.x;
                    actualLightVector.y = transformPosition.y - lightSourcePosition.y;
                    actualLightVector.z = transformPosition.z - lightSourcePosition.z;
                    actualLightVector = actualLightVector.FastNormalized();
                }
            } else {
                actualLightVector = _lightVector;
            }

            _actualLightVectorPrev = actualLightVector;

            // Do a raycast from transform.position to the center of the shadow
            RaycastHit hitInfo;
            bool raycastResult = Physics.Raycast(transformPosition, actualLightVector, out hitInfo, _projectionDistance, _layerMaskInt);

            if (raycastResult) {
                Vector3 hitPoint = hitInfo.point;

                // Scale the shadow respectively
                Vector3 lossyScale = _transform.lossyScale;
                float scaledDoubleShadowSize = Mathf.Max(Mathf.Max(lossyScale.x, lossyScale.y), lossyScale.z) * _shadowSize;

                // Distance scale
                float distance = hitInfo.distance;
                float scaleWeight = (distance - _shadowSizeScaleStartDistance) / (_shadowSizeScaleEndDistance - _shadowSizeScaleStartDistance);
                scaleWeight = scaleWeight > 1f ? 1f : (scaleWeight < 0f ? 0f : scaleWeight);
                if (scaleWeight != 0f) {
                    float scale = _shadowSizeEndScale + (1f - _shadowSizeEndScale) * (1f - scaleWeight);
                    scaledDoubleShadowSize *= scale;
                }

                float scaledShadowSize = scaledDoubleShadowSize * 0.5f;
                if (!_isStatic && _cullInvisible) {
                    // We can calculate approximate bounds for orthographic shadows easily
                    // and cull shadows based on these bounds and camera frustum
                    if (!_isPerspectiveProjection) {
                        Vector3 shadowScale;
                        shadowScale.x = scaledShadowSize;
                        shadowScale.y = scaledShadowSize;
                        shadowScale.z = scaledShadowSize;
                        Bounds bounds = new Bounds();
                        bounds.center = hitPoint;
                        bounds.extents = shadowScale;
                        _isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
                        if (!_isVisible)
                            return RecalculateShadowResult.Skipped;
                    } else {
                        // For perspective shadows, we can at least try to 
                        // not draw shadows that fall on invisible objects
                        Bounds rendererBounds;
                        bool result = ShadowManager.Instance.ColliderToRendererBoundsCache.GetRendererBoundsFromCollider(hitInfo.collider, out rendererBounds);
                        if (result) {
                            _isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, rendererBounds);
                            if (!_isVisible)
                                return RecalculateShadowResult.Skipped;
                        }
                    }
                }

                // Calculate angle from light direction vector to surface normal
                if (_isSmoothRotation) {
                    _normal = Vector3.Lerp(_normal, hitInfo.normal, _smoothRotationSpeed * deltaTime).FastNormalized();
                } else {
                    _normal = hitInfo.normal;
                }

                float lightVectorToNormalDot = -actualLightVector.x * _normal.x - actualLightVector.y * _normal.y - actualLightVector.z * _normal.z;
                float angleToNormal = FastMath.FastPseudoAcos(lightVectorToNormalDot) * Mathf.Rad2Deg;
                if (angleToNormal > _angleFadeMax) {
                    // Skip shadows that fall with extreme angles
                    _isVisible = false;
                    return RecalculateShadowResult.Skipped;
                }

                // Determine the forward direction of shadow base quad
                Vector3 forward;
                float dot = transformForward.x * actualLightVector.x + transformForward.y * actualLightVector.y + transformForward.z * actualLightVector.z;
                if (Mathf.Abs(dot) < 1f - Vector3.kEpsilon) {
                    forward.x = transformForward.x - dot * actualLightVector.x;
                    forward.y = transformForward.y - dot * actualLightVector.y;
                    forward.z = transformForward.z - dot * actualLightVector.z;
                    forward = forward.FastNormalized();
                } else {
                    // If the forward direction matches the light direction vector somehow
                    Vector3 transformUp = transformRotation * kVector3Up;
                    dot = transformUp.x * actualLightVector.x + transformUp.y * actualLightVector.y + transformUp.z * actualLightVector.z;
                    forward.x = transformUp.x - dot * actualLightVector.x;
                    forward.y = transformUp.y - dot * actualLightVector.y;
                    forward.z = transformUp.z - dot * actualLightVector.z;
                    forward = forward.FastNormalized();
                }

                // Rotation of shadow base quad
                Vector3 actualLightVectorNegated;
                actualLightVectorNegated.x = -actualLightVector.x;
                actualLightVectorNegated.y = -actualLightVector.y;
                actualLightVectorNegated.z = -actualLightVector.z;
                Quaternion rotation = Quaternion.LookRotation(forward, actualLightVectorNegated);

                // Optimized version of
                // Vector3 right = rotation * Vector3.right;
                float num2 = rotation.y * 2f;
                float num3 = rotation.z * 2f;
                float num5 = rotation.y * num2;
                float num6 = rotation.z * num3;
                float num7 = rotation.x * num2;
                float num8 = rotation.x * num3;
                float num11 = rotation.w * num2;
                float num12 = rotation.w * num3;
                Vector3 right;
                right.x = 1f - (num5 + num6);
                right.y = num7 + num12;
                right.z = num8 - num11;

                // Base vertices calculation
                float aspectRatioRec = 1f / _aspectRatio;

                Vector3 tmp1;
                tmp1.x = forward.x * scaledShadowSize;
                tmp1.y = forward.y * scaledShadowSize;
                tmp1.z = forward.z * scaledShadowSize;
                Vector3 tmp2;
                tmp2.x = right.x * aspectRatioRec * scaledShadowSize;
                tmp2.y = right.y * aspectRatioRec * scaledShadowSize;
                tmp2.z = right.z * aspectRatioRec * scaledShadowSize;

                Vector3 diff;
                diff.x = tmp1.x - tmp2.x;
                diff.y = tmp1.y - tmp2.y;
                diff.z = tmp1.z - tmp2.z;
                Vector3 sum;
                sum.x = tmp1.x + tmp2.x;
                sum.y = tmp1.y + tmp2.y;
                sum.z = tmp1.z + tmp2.z;

                Vector3 baseVertex;
                baseVertex.x = transformPosition.x - sum.x;
                baseVertex.y = transformPosition.y - sum.y;
                baseVertex.z = transformPosition.z - sum.z;
                _baseVertices[0] = baseVertex;

                baseVertex.x = transformPosition.x + diff.x;
                baseVertex.y = transformPosition.y + diff.y;
                baseVertex.z = transformPosition.z + diff.z;
                _baseVertices[1] = baseVertex;

                baseVertex.x = transformPosition.x + sum.x;
                baseVertex.y = transformPosition.y + sum.y;
                baseVertex.z = transformPosition.z + sum.z;
                _baseVertices[2] = baseVertex;

                baseVertex.x = transformPosition.x - diff.x;
                baseVertex.y = transformPosition.y - diff.y;
                baseVertex.z = transformPosition.z - diff.z;
                _baseVertices[3] = baseVertex;

                // Calculate a plane from normal and position
                Vector3 offsetPoint;
                offsetPoint.x = hitPoint.x + _normal.x * _shadowOffset;
                offsetPoint.y = hitPoint.y + _normal.y * _shadowOffset;
                offsetPoint.z = hitPoint.z + _normal.z * _shadowOffset;
                FastMath.Plane shadowPlane = new FastMath.Plane(_normal, offsetPoint);

                float distanceToPlane;
                FastMath.Ray ray = new FastMath.Ray();

                // Calculate the shadow vertices
                if (_isPerspectiveProjection && useLightSource) {
                    ray.Direction.x = lightSourcePosition.x - _baseVertices[0].x;
                    ray.Direction.y = lightSourcePosition.y - _baseVertices[0].y;
                    ray.Direction.z = lightSourcePosition.z - _baseVertices[0].z;
                    ray.Origin = _baseVertices[0];
                    shadowPlane.Raycast(ref ray, out distanceToPlane);
                    _shadowVertices[0].x = ray.Origin.x + ray.Direction.x * distanceToPlane;
                    _shadowVertices[0].y = ray.Origin.y + ray.Direction.y * distanceToPlane;
                    _shadowVertices[0].z = ray.Origin.z + ray.Direction.z * distanceToPlane;

                    ray.Direction.x = lightSourcePosition.x - _baseVertices[1].x;
                    ray.Direction.y = lightSourcePosition.y - _baseVertices[1].y;
                    ray.Direction.z = lightSourcePosition.z - _baseVertices[1].z;
                    ray.Origin = _baseVertices[1];
                    shadowPlane.Raycast(ref ray, out distanceToPlane);
                    _shadowVertices[1].x = ray.Origin.x + ray.Direction.x * distanceToPlane;
                    _shadowVertices[1].y = ray.Origin.y + ray.Direction.y * distanceToPlane;
                    _shadowVertices[1].z = ray.Origin.z + ray.Direction.z * distanceToPlane;

                    ray.Direction.x = lightSourcePosition.x - _baseVertices[2].x;
                    ray.Direction.y = lightSourcePosition.y - _baseVertices[2].y;
                    ray.Direction.z = lightSourcePosition.z - _baseVertices[2].z;
                    ray.Origin = _baseVertices[2];
                    shadowPlane.Raycast(ref ray, out distanceToPlane);
                    _shadowVertices[2].x = ray.Origin.x + ray.Direction.x * distanceToPlane;
                    _shadowVertices[2].y = ray.Origin.y + ray.Direction.y * distanceToPlane;
                    _shadowVertices[2].z = ray.Origin.z + ray.Direction.z * distanceToPlane;

                    ray.Direction.x = lightSourcePosition.x - _baseVertices[3].x;
                    ray.Direction.y = lightSourcePosition.y - _baseVertices[3].y;
                    ray.Direction.z = lightSourcePosition.z - _baseVertices[3].z;
                    ray.Origin = _baseVertices[3];
                    shadowPlane.Raycast(ref ray, out distanceToPlane);
                    _shadowVertices[3].x = ray.Origin.x + ray.Direction.x * distanceToPlane;
                    _shadowVertices[3].y = ray.Origin.y + ray.Direction.y * distanceToPlane;
                    _shadowVertices[3].z = ray.Origin.z + ray.Direction.z * distanceToPlane;
                } else {
                    ray.Direction = actualLightVector;

                    ray.Origin = _baseVertices[0];
                    shadowPlane.Raycast(ref ray, out distanceToPlane);
                    _shadowVertices[0].x = ray.Origin.x + ray.Direction.x * distanceToPlane;
                    _shadowVertices[0].y = ray.Origin.y + ray.Direction.y * distanceToPlane;
                    _shadowVertices[0].z = ray.Origin.z + ray.Direction.z * distanceToPlane;

                    ray.Origin = _baseVertices[1];
                    shadowPlane.Raycast(ref ray, out distanceToPlane);
                    _shadowVertices[1].x = ray.Origin.x + ray.Direction.x * distanceToPlane;
                    _shadowVertices[1].y = ray.Origin.y + ray.Direction.y * distanceToPlane;
                    _shadowVertices[1].z = ray.Origin.z + ray.Direction.z * distanceToPlane;

                    ray.Origin = _baseVertices[2];
                    shadowPlane.Raycast(ref ray, out distanceToPlane);
                    _shadowVertices[2].x = ray.Origin.x + ray.Direction.x * distanceToPlane;
                    _shadowVertices[2].y = ray.Origin.y + ray.Direction.y * distanceToPlane;
                    _shadowVertices[2].z = ray.Origin.z + ray.Direction.z * distanceToPlane;

                    ray.Origin = _baseVertices[3];
                    shadowPlane.Raycast(ref ray, out distanceToPlane);
                    _shadowVertices[3].x = ray.Origin.x + ray.Direction.x * distanceToPlane;
                    _shadowVertices[3].y = ray.Origin.y + ray.Direction.y * distanceToPlane;
                    _shadowVertices[3].z = ray.Origin.z + ray.Direction.z * distanceToPlane;
                }

                // Calculate the shadow alpha
                float shadowAlpha = _initialAlpha;

                // Alpha base on distance to the surface
                if (distance > _fadeDistance) {
                    shadowAlpha = shadowAlpha - (distance - _fadeDistance) / (_projectionDistance - _fadeDistance) * shadowAlpha;
                }

                // Alpha based on shadow fall angle
                if (angleToNormal > _angleFadeMin) {
                    shadowAlpha = shadowAlpha - (angleToNormal - _angleFadeMin) / (_angleFadeMax - _angleFadeMin) * shadowAlpha;
                }

                // Convert float alpha to byte
                //_color.a = shadowAlpha;
                _color32.a = (byte) (shadowAlpha * 255f);

                _isVisible = true;
            }

            return RecalculateShadowResult.Recalculated;
        }

        public int GetMeshManagerHashCode() {
            int layer = _useForceLayer ? _forceLayer : gameObject.layer;
            return ShadowMeshManager.CalculateMeshManagerHashCode(_isStatic, _material, layer);
        }

        /// <summary>
        /// Updates the directional light state.
        /// </summary>
        private void UpdateDirectionalLight() {
            if (_lightSourceObject != null) {
                Light lightSource = _lightSourceObject.GetComponent<Light>();
                _lightSourceObjectIsDirectionalLight = lightSource != null;
                if (lightSource != null && lightSource.type != LightType.Directional) {
                    _lightSourceObjectIsDirectionalLight = false;
                }
            } else {
                _lightSourceObjectIsDirectionalLight = false;
            }
        }

        /// <summary>
        /// Calculates initial field values.
        /// </summary>
        private void UpdateProperties() {
            _color32 = _color;
            _layerMaskInt = _layerMask;
            _initialAlpha = _color.a;
            UpdateTextureUV();
            UpdateDirectionalLight();
        }

        private void UpdateTextureUV() {
            bool canUseSprite = _sprite != null;
            if (canUseSprite) {
                Texture texture = _material.GetTexture("_MainTex");
                canUseSprite = texture != null && texture == _sprite.texture;
            }

            if (canUseSprite) {
                Rect textureRect = _sprite.textureRect;
                float invWidth = 1f / _sprite.texture.width;
                float invHeight = 1f / _sprite.texture.height;
                textureRect.x *= invWidth;
                textureRect.width *= invWidth;
                textureRect.y *= invHeight;
                textureRect.height *= invHeight;
                SetTextureUV(textureRect);
            }
            else {
                SetTextureUV(_textureUVRect);
            }
        }

        private void Initialize() {
            _isInitialized = false;
            _recompileMarker = new object();

            if (_material == null) {
                const string defaultMaterialPath = "Materials/Shadow Multiply";
                _material = Resources.Load<Material>(defaultMaterialPath);
                if (_material == null) {
                    Debug.LogError(
                        "Standard \"" + defaultMaterialPath + "\" material was not found. " +
                        "Please assign a shadow material manually for shadow to work.", 
                        this);

                    return;
                }
            }

            if (
#if UNITY_EDITOR
                Application.isPlaying &&
#endif
                _layerMask == 0) {
                Debug.LogError("LayerMask is empty! Reverting to Everything", this);
                _layerMask = unchecked(~0);
                _layerMaskInt = _layerMask;
            }

            UpdateComponents();
            UpdateProperties();

            _isGameObjectActivePrev = gameObject.activeInHierarchy;
            _isFirstCalculation = true;

            _isInitialized = true;
        }

        /// <summary>
        /// Updates the cached GameObject components.
        /// </summary>
        private void UpdateComponents() {
            _transform = transform;
        }

        /// <summary>
        /// Calculates UV vectors from Rect.
        /// </summary>
        /// <param name="uvRect">
        /// UV rect.
        /// </param>
        private void SetTextureUV(Rect uvRect) {
            _textureUV[0] = new Vector2(uvRect.x, uvRect.y);
            _textureUV[1] = new Vector2(uvRect.x, uvRect.y + uvRect.height);
            _textureUV[2] = new Vector2(uvRect.x + uvRect.width, uvRect.y + uvRect.height);
            _textureUV[3] = new Vector2(uvRect.x + uvRect.width, uvRect.y);
        }

        #region Unity methods

        private void OnEnable() {
            if (!_isInitialized || _recompileMarker == null)
                Initialize();

            RegisterShadow();
        }

        private void OnDestroy() {
            UnregisterShadow();
            _isDestroyed = true;
        }

        private void OnDisable() {
            if (!_isInitialized || _isDestroyed || !gameObject.activeSelf)
                return;

            UnregisterShadow();
        }

        /// <summary>
        /// Called when the user hits the Reset button in the Inspector's context menu or when adding the component the first time. 
        /// </summary>
        private void Reset() {
            int goLayerMask = 1 << gameObject.layer;
            _forceLayer = goLayerMask;
            _layerMask = unchecked(~0);
        }

        /// <summary>
        /// Draws gizmos.
        /// </summary>
        private void OnDrawGizmosSelected() {
            if (!enabled || !gameObject.activeSelf)
                return;

            const float alpha = 0.7f;
            Color color = Color.red;
            color.a = alpha;
            Gizmos.color = color;
            Gizmos.DrawRay(transform.position, _actualLightVectorPrev * _projectionDistance);

            if (_isVisible) {
                color = Color.yellow;
                color.a = alpha;
                Gizmos.color = color;

                Gizmos.DrawLine(_baseVertices[0], _baseVertices[1]);
                Gizmos.DrawLine(_baseVertices[1], _baseVertices[2]);
                Gizmos.DrawLine(_baseVertices[2], _baseVertices[3]);
                Gizmos.DrawLine(_baseVertices[3], _baseVertices[0]);

                Gizmos.DrawLine(_baseVertices[0], _shadowVertices[0]);
                Gizmos.DrawLine(_baseVertices[1], _shadowVertices[1]);
                Gizmos.DrawLine(_baseVertices[2], _shadowVertices[2]);
                Gizmos.DrawLine(_baseVertices[3], _shadowVertices[3]);

                Gizmos.DrawLine(_shadowVertices[0], _shadowVertices[1]);
                Gizmos.DrawLine(_shadowVertices[1], _shadowVertices[2]);
                Gizmos.DrawLine(_shadowVertices[2], _shadowVertices[3]);
                Gizmos.DrawLine(_shadowVertices[3], _shadowVertices[0]);
            }
        }

        #endregion


        /// <summary>
        /// Enum representing the source of light direction vector.
        /// </summary>
        public enum LightVectorSourceEnum {
            StaticVector,
            GameObject
        }

        /// <summary>
        /// The result of RecalculateShadow().
        /// </summary>
        public enum RecalculateShadowResult {
            /// <summary>
            /// The shadow was recalculated successfully.
            /// </summary>
            Recalculated,

            /// <summary>
            /// The shadow recalculation was skipped.
            /// </summary>
            Skipped,

            /// <summary>
            /// The shadow moved to another mesh manager.
            /// </summary>
            ChangedManager
        }
    }
}