using System;
using UnityEditor;
using UnityEngine;

namespace LostPolygon.SwiftShadows.Internal.Editor {
    /// <summary>
    /// A custom inspector for SwiftShadow.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SwiftShadow))]
    public class SwiftShadowEditor : PropertyEditor<SwiftShadow> {
        private SerializedProperty _material;
        private SerializedProperty _layerMask;
        private SerializedProperty _isStatic;
        private SerializedProperty _lightVectorSource;
        private SerializedProperty _lightVector;
        private SerializedProperty _lightSourceObject;
        private SerializedProperty _shadowSize;
        private SerializedProperty _shadowSizeScaleStartDistance;
        private SerializedProperty _shadowSizeScaleEndDistance;
        private SerializedProperty _shadowSizeEndScale;
        private SerializedProperty _shadowOffset;
        private SerializedProperty _projectionDistance;
        private SerializedProperty _isPerspectiveProjection;
        private SerializedProperty _autoStaticTime;
        private SerializedProperty _fadeDistance;
        private SerializedProperty _angleFadeMin;
        private SerializedProperty _angleFadeMax;
        private SerializedProperty _aspectRatio;
        private SerializedProperty _color;
        private SerializedProperty _textureUVRect;
        private SerializedProperty _cullInvisible;
        private SerializedProperty _useForceLayer;
        private SerializedProperty _forceLayer;
        private SerializedProperty _isSmoothRotation;
        private SerializedProperty _smoothRotationSpeed;
        private SerializedProperty _sprite;

        private bool _isRollFadeScaleOpened;
        private bool _isRollSmoothRotationOpened;

        protected override void OnEnable() {
            base.OnEnable();

            _material = serializedObject.FindProperty("_material");
            _layerMask = serializedObject.FindProperty("_layerMask");
            _isStatic = serializedObject.FindProperty("_isStatic");
            _lightVectorSource = serializedObject.FindProperty("_lightVectorSource");
            _lightVector = serializedObject.FindProperty("_lightVector");
            _lightSourceObject = serializedObject.FindProperty("_lightSourceObject");
            _shadowSize = serializedObject.FindProperty("_shadowSize");
            _shadowSizeScaleStartDistance = serializedObject.FindProperty("_shadowSizeScaleStartDistance");
            _shadowSizeScaleEndDistance = serializedObject.FindProperty("_shadowSizeScaleEndDistance");
            _shadowSizeEndScale = serializedObject.FindProperty("_shadowSizeEndScale");
            _shadowOffset = serializedObject.FindProperty("_shadowOffset");
            _projectionDistance = serializedObject.FindProperty("_projectionDistance");
            _isPerspectiveProjection = serializedObject.FindProperty("_isPerspectiveProjection");
            _autoStaticTime = serializedObject.FindProperty("_autoStaticTime");
            _fadeDistance = serializedObject.FindProperty("_fadeDistance");
            _angleFadeMin = serializedObject.FindProperty("_angleFadeMin");
            _angleFadeMax = serializedObject.FindProperty("_angleFadeMax");
            _aspectRatio = serializedObject.FindProperty("_aspectRatio");
            _color = serializedObject.FindProperty("_color");
            _textureUVRect = serializedObject.FindProperty("_textureUVRect");
            _cullInvisible = serializedObject.FindProperty("_cullInvisible");
            _useForceLayer = serializedObject.FindProperty("_useForceLayer");
            _forceLayer = serializedObject.FindProperty("_forceLayer");
            _isSmoothRotation = serializedObject.FindProperty("_isSmoothRotation");
            _smoothRotationSpeed = serializedObject.FindProperty("_smoothRotationSpeed");
            _sprite = serializedObject.FindProperty("_sprite");
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            SwiftShadow.LightVectorSourceEnum lightVectorSource = (SwiftShadow.LightVectorSourceEnum) DoFieldProperty<int>(
                _lightVectorSource,
                new GUIContent(
                    "Light direction from: ",
                    ""
                    ),
                "LightVectorSource",
                (rect, label, property) =>
                    EditorGUI.Popup(rect, label, property.enumValueIndex, new[] { new GUIContent("Static vector"), new GUIContent("Light source object") }));

            EditorGUI.indentLevel++;
            if (lightVectorSource == SwiftShadow.LightVectorSourceEnum.StaticVector) {
                DoFieldProperty(
                    _lightVector,
                    new GUIContent(
                        "Direction vector: ",
                        "Light direction vector relative to the object. For example, (0, -1, 0) means the light always come from right above."
                        ),
                    "LightVector",
                    (rect, label, property) => EditorGUI.Vector3Field(rect, label.text, property.vector3Value));
            } else {
                Transform lightSourceObject = DoFieldProperty<Transform>(
                    _lightSourceObject,
                    new GUIContent(
                        "Light source: ",
                        "The GameObject that'll be used as point light source for this shadow."
                        ),
                    "LightSourceObject",
                    (rect, label, property) => (Transform) EditorGUI.ObjectField(rect, label, property.objectReferenceValue, typeof(Transform), true));

                if (lightSourceObject == null) {
                    EditorGUILayout.HelpBox("No light source selected. Shadow will use the static vector instead.", MessageType.Warning, false);
                }
            }
            EditorGUI.indentLevel--;

            DoFieldProperty<LayerMask>(
                _layerMask,
                new GUIContent(
                    "Raycast layer mask: ",
                    "Layers to cast shadows on. " +
                    "You must to exclude the layer of the object " +
                    "itself if it has a collider attached to it, otherwise shadow may behave strange."
                    ),
                "LayerMask",
                (rect, label, property) => {
                    EditorGUIInternal.LayerMaskField(rect, property, label);
                    return 0;
                });

            if (_layerMask.intValue == 0) {
                EditorGUILayout.HelpBox("No layer mask is set. Shadows won't project on anything.", MessageType.Warning, false);
            }

            EditorGUILayout.HelpBox("Shadow", MessageType.None, true);

            Material shadowMaterial = DoFieldProperty(
                _material,
                new GUIContent(
                    "Material: ",
                    "Material that'll be used for rendering the shadow."
                    ),
                "Material",
                (rect, label, property) => (Material) EditorGUI.ObjectField(rect, label, property.objectReferenceValue, typeof(Material), false));

            if (shadowMaterial == null) {
                EditorGUILayout.HelpBox("No material assigned. Shadow will use default blob shadow material.", MessageType.Info, false);
            }

            Sprite sprite = DoFieldProperty(
                _sprite,
                new GUIContent(
                    "Sprite: ",
                    "Sprite that will be used for rendering the shadow."
                    ),
                "Sprite",
                (rect, label, property) => (Sprite) EditorGUI.ObjectField(rect, label, property.objectReferenceValue, typeof(Sprite), false));

            bool isValidSprite = sprite != null;
            if (sprite != null) {
                if (shadowMaterial != null) {
                    Texture texture = shadowMaterial.GetTexture("_MainTex");
                    if (texture != null && texture != sprite.texture) {
                        EditorGUILayout.HelpBox(
                            "Sprite parent texture and material main texture must match.",
                            MessageType.Error, 
                            false);
                        isValidSprite = false;
                    }
                }

                if (sprite.packed) {
                    EditorGUILayout.HelpBox(
                        "Packed sprites are not allowed.",
                        MessageType.Error, 
                        false);
                    isValidSprite = false;
                }

                TextureImporterSettings textureImporterSettings = new TextureImporterSettings();
                sprite.texture.GetTextureImporter().ReadTextureSettings(textureImporterSettings);
                if (textureImporterSettings.spriteMeshType != SpriteMeshType.FullRect) {
                    EditorGUILayout.HelpBox(
                        "\'Tight\' Sprite mesh type is not allowed.",
                        MessageType.Error, 
                        false);
                    isValidSprite = false;
                }
            }

            DoFieldProperty<Color32>(
                _color,
                new GUIContent(
                    "Shadow color: ",
                    "The color of the shadow."
                    ),
                "InitialColor",
                (rect, label, property) => EditorGUI.ColorField(rect, label, property.colorValue));

            DoFieldProperty(
                _shadowSize,
                new GUIContent(
                    "Shadow size: ",
                    "Scale of shadow relative to object's max dimensions."
                    ),
                "ShadowSize",
                (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(EditorGUIUtilityInternal.labelWidth - EditorGUILayoutExtensions.kIndentationWidth + 2f);
            if (GUILayout.Button("Estimate size")) {
                GUI.changed = true;
                _shadowSize.serializedObject.Update();
                foreach (SwiftShadow shadow in _objectList) {
                    GameObject go = shadow.gameObject;
                    Quaternion goRotation = go.transform.rotation;
                    go.transform.rotation = Quaternion.identity;

                    Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
                    Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
                    if (renderers.Length == 0) {
                        Debug.Log("No renderers found, can't estimate size", shadow);
                        continue;
                    }

                    foreach (Renderer renderer in renderers) {
                        bounds.Encapsulate(renderer.bounds);
                    }

                    if (bounds.size.magnitude > Vector3.kEpsilon) {
                        float scaleMin = Mathf.Min(go.transform.lossyScale.x, go.transform.lossyScale.y, go.transform.lossyScale.z);
                        shadow.ShadowSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / scaleMin;
                        shadow.ShadowSize = (float) Math.Round(shadow.ShadowSize, 5);
                    }

                    go.transform.rotation = goRotation;
                }
                _shadowSize.serializedObject.UpdateIfDirtyOrScript();
                _shadowSize.serializedObject.ApplyModifiedProperties();
                _shadowSize.serializedObject.SetIsDifferentCacheDirty();

                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            GUI.enabled = lightVectorSource == SwiftShadow.LightVectorSourceEnum.GameObject;
            DoFieldProperty(
                _isPerspectiveProjection,
                new GUIContent(
                    "Perspective projection: ",
                    "Makes shadows bigger as they move away from the light source. " +
                    "This usually looks more realistic, but may result in artifacts at extreme angles."
                    ),
                "IsPerspectiveProjection",
                (rect, label, property) => EditorGUI.Toggle(rect, label, property.boolValue));
            GUI.enabled = true;

            DoFieldProperty(
                _projectionDistance,
                new GUIContent(
                    "Projection distance: ",
                    "Maximal distance from the transform position to the surface shadow will fall on."
                    ),
                "ProjectionDistance",
                (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

            EditorGUILayout.BeginHorizontal(GUILayout.Width(100f));
            EditorGUILayout.HelpBox("Fading", MessageType.None, true);
            EditorGUILayout.EndHorizontal();

            DoFieldProperty(
                _fadeDistance,
                new GUIContent(
                    "Fade distance: ",
                    "Distance at which shadow will start fading out. Used for smooth transition to \"Projection distance\""
                    ),
                "FadeDistance",
                (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

            DoFieldProperty(
                _angleFadeMin,
                new GUIContent(
                    "Angle fade from: ",
                    "The angle at which the shadow will start fading out. Used for smooth transition to \"Max angle\""
                    ),
                "AngleFadeMin",
                (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

            DoFieldProperty(
                _angleFadeMax,
                new GUIContent(
                    "Max angle: ",
                    "The maximum angle at which the shadow is allowed to fall on surface. " +
                    "It it falls at a bigger angle, no shadow will be rendered."
                    ),
                "AngleFadeMax",
                (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

            _isRollFadeScaleOpened = EditorGUILayout.Foldout(_isRollFadeScaleOpened, "Size");
            if (_isRollFadeScaleOpened) {
                EditorGUI.indentLevel++;

                DoFieldProperty(
                    _shadowSizeScaleStartDistance,
                    new GUIContent(
                        "Start distance: ",
                        "Determines the beginning of interval in which the shadow will change size accordingly to the distance to the ground. " +
                        "When the distance is smaller than \"Start distance\", the size will remain the same as set with \"Shadow size\" option. " +
                        "When approaching \"End distance\", the \"Shadow size\" will be smoothly multiplied with \"End scale\" value."
                        ),
                    "SizeScaleStartDistance",
                    (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

                DoFieldProperty(
                    _shadowSizeScaleEndDistance,
                    new GUIContent(
                        "End distance: ",
                        "Determines the end of interval in which the shadow will change size accordingly to the distance to the ground. " +
                        "When the distance is smaller than \"Start distance\", the size will remain the same as set with \"Shadow size\" option. " +
                        "When approaching \"End distance\", the \"Shadow size\" will be smoothly multiplied with \"End scale\" value."
                        ),
                    "SizeScaleEndDistance",
                    (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

                DoFieldProperty(
                    _shadowSizeEndScale,
                    new GUIContent(
                        "End scale: ",
                        "Multiplier applied to the \"Shadow size\" when the distance to the ground is equal or larger that \"End distance\"."
                        ),
                    "SizeEndScale",
                    (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

                EditorGUI.indentLevel--;
            }

            _isRollSmoothRotationOpened = EditorGUILayout.Foldout(_isRollSmoothRotationOpened, "Smooth rotation");
            if (_isRollSmoothRotationOpened) {
                EditorGUI.indentLevel++;

                DoFieldProperty(
                    _isSmoothRotation,
                    new GUIContent(
                        "Use smooth rotation: ",
                        "When this option is enabled, shadow will smoothly rotate to the calculated orientation instead of changing it instantly, " +
                        "which may be distracting in some situations."
                        ),
                    "IsSmoothRotation",
                    (rect, label, property) => EditorGUI.Toggle(rect, label, property.boolValue));

                GUI.enabled = _isSmoothRotation.boolValue;
                DoFieldProperty(
                    _smoothRotationSpeed,
                    new GUIContent(
                        "Rotation speed: ",
                        "Controls how fast the shadow will orient itself to the calculated orientation. " +
                        "The bigger the value – the faster the shadow rotates."
                        ),
                    "SmoothRotationSpeed",
                    (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));
                GUI.enabled = true;

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(100f));
            EditorGUILayout.HelpBox("Static", MessageType.None, true);
            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying) {
                GUI.enabled = Mathf.Abs(_autoStaticTime.floatValue) <= Vector3.kEpsilon;
            }

            DoFieldProperty(
                _isStatic,
                new GUIContent(
                    "Static: ",
                    "If checked, the shadow will be calculated only at the creation. " +
                    "Use this for shadows that do not move for a huge perfomance boost."
                    ),
                "IsStatic",
                (rect, label, property) => {
                    EditorGUILayout.BeginHorizontal();

                    Rect labelRect = rect;
                    labelRect.xMin += EditorGUILayoutExtensions.kLeftPaddingWidth + EditorGUILayoutExtensions.kIndentationWidth * EditorGUI.indentLevel;
                    labelRect.width = GUI.skin.label.CalcSize(label).x;
                    rect.xMin = labelRect.xMax;

                    GUI.Label(labelRect, label);
                    bool result = EditorGUI.Toggle(rect, property.boolValue);
                    EditorGUILayout.EndHorizontal();

                    return result;
                });
            GUI.enabled = false;
            if (!Application.isPlaying && _isStatic.boolValue) {
                _autoStaticTime.floatValue = 0f;
            }

            GUI.enabled = !_isStatic.boolValue;
            DoFieldProperty(
                _autoStaticTime,
                new GUIContent(
                    "Set to static if not moving for",
                    "Makes shadow static if it hasn't moved for X seconds. " +
                    "Shadow will return to non-static state when moving or rotating. " +
                    "This is useful for optimizing performance if you have shadow that " +
                    "only move from time to time. Value of 0 disables this setting."
                    ),
                "AutoStaticTime",
                (rect, label, property) => {
                    GUIContent secText = new GUIContent("sec.");

                    Rect labelRect = rect;
                    labelRect.xMin += EditorGUILayoutExtensions.kLeftPaddingWidth + EditorGUILayoutExtensions.kIndentationWidth * EditorGUI.indentLevel;
                    labelRect.width = GUI.skin.label.CalcSize(label).x;
                    rect.xMin = labelRect.xMax;

                    GUI.Label(labelRect, label);

                    labelRect = rect;
                    labelRect.xMin = rect.xMax - GUI.skin.label.CalcSize(secText).x;

                    rect.xMax = labelRect.xMin;

                    float result = EditorGUI.FloatField(rect, property.floatValue);
                    GUI.Label(labelRect, secText);

                    return result;
                });
            GUI.enabled = true;

            EditorGUILayout.HelpBox("Advanced", MessageType.None, true);

            if (!isValidSprite) {
                DoFieldProperty(
                    _textureUVRect,
                    new GUIContent(
                        "Texture coordinates: ",
                        "The texture coordinates of shadow. Change this to use multiple shadow cookies within the material texture."
                        ),
                    "TextureUVRect",
                    (rect, label, property) => EditorGUI.RectField(rect, label, property.rectValue));
            }

            DoFieldProperty(
                _aspectRatio,
                new GUIContent("Aspect ratio: ", "The width/height aspect ratio of shadow."),
                "AspectRatio",
                (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

            DoFieldProperty(
                _shadowOffset,
                new GUIContent(
                    "Shadow offset: ",
                    "The distance at which the shadow hovers above the surface. " +
                    "Increase this value if you see shadows flickering due to Z-fighting"
                    ),
                "ShadowOffset",
                (rect, label, property) => EditorGUI.FloatField(rect, label, property.floatValue));

            DoFieldProperty(
                _cullInvisible,
                new GUIContent(
                    "Ignore culling: ",
                    "If selected, the camera culling will be disabled. " +
                    "Check this for a small performance gain if shadow is always in sight."
                    ),
                "CullInvisible",
                (rect, label, property) => !EditorGUI.Toggle(rect, label, !property.boolValue));

            bool useForceLayer = DoFieldProperty(
                _useForceLayer,
                new GUIContent(
                    "Shadow layer: ",
                    "The layer at which the shadow will be rendered."
                    ),
                "UseForceLayer",
                (rect, label, property) =>
                    EditorGUI.Popup(
                    rect,
                    label,
                    property.boolValue ? 1 : 0,
                    EditorGUIUtilityInternal.TempContent(new [] { "Same as GameObject", "Manual" })) == 1);

            if (useForceLayer) {
                EditorGUI.indentLevel++;
                DoFieldProperty(
                    _forceLayer,
                    new GUIContent(
                        "Layer: ",
                        ""
                        ),
                    "ForceLayer",
                    (rect, label, property) => {
                        int layer = Mathf.Clamp(_forceLayer.intValue, 0, 31);
                        if (string.IsNullOrEmpty(LayerMask.LayerToName(layer)))
                            layer = 0;

                        return EditorGUI.LayerField(rect, label, layer);
                    });
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}