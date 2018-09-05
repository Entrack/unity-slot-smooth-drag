#if UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2
#  define PRE_UNITY_4_3
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LostPolygon.SwiftShadows.Internal.Editor {
    /// <summary>
    /// Extension methods for UnityEditor classes.
    /// </summary>
    public static class EditorClassesExtensions {
        public static string[] GetLayerMaskNames(this SerializedProperty property) {
            return (string[]) typeof(SerializedProperty).GetMethod("GetLayerMaskNames", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Invoke(property, null);
        }
    }

    /// <summary>
    /// Utilities for working with TextureImported and Texture2D.
    /// </summary>
    public static class TextureImporterHelper {
        public static TextureImporter GetTextureImporter(this Texture2D texture) {
            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            return textureImporter;
        }

        public static void SetIsReadable(this Texture2D texture, bool readable) {
            TextureImporter textureImporter = texture.GetTextureImporter();
            if (textureImporter != null) {
                textureImporter.isReadable = readable;
                textureImporter.ImportNow();
            }
        }

        public static bool GetIsReadable(this Texture2D texture) {
            TextureImporter textureImporter = texture.GetTextureImporter();
            return textureImporter == null || textureImporter.isReadable;
        }

        public static void ImportNow(this TextureImporter textureImporter) {
            AssetDatabase.ImportAsset(textureImporter.assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }
    }

    /// <summary>
    /// Utilities for working with EditorGUILayout.
    /// </summary>
    public static class EditorGUILayoutExtensions {
        public const float kIndentationWidth = 9f;
        public const float kLeftPaddingWidth = 4f;

        // FixedWidthLabel class. Extends IDisposable, so that it can be used with the "using" keyword.
        private class FixedWidthLabel : IDisposable {
            private readonly ZeroIndent _indentReset; // Helper class to reset and restore indentation

            public FixedWidthLabel(GUIContent label) {
#if PRE_UNITY_4_3
                float indentation = kIndentationWidth * EditorGUI.indentLevel + kLeftPaddingWidth;
#else
                float indentation = kIndentationWidth * EditorGUI.indentLevel;
#endif

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(indentation);
                float width = Mathf.Max(EditorGUIUtilityInternal.labelWidth - indentation,
                    GUI.skin.label.CalcSize(label).x);
                GUILayout.Label(label, GUILayout.Width(width));

                _indentReset = new ZeroIndent();
            }

            public FixedWidthLabel(string label) : this(new GUIContent(label)) {
            }

            public void Dispose() {
                _indentReset.Dispose();
                EditorGUILayout.EndHorizontal();
            }
        }

        private class ZeroIndent : IDisposable {
            private readonly int _originalIndent;

            public ZeroIndent() {
                _originalIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
            }

            public void Dispose() {
                EditorGUI.indentLevel = _originalIndent;
            }
        }

        public static bool ToggleFixedWidth(Rect rect, GUIContent label, bool value) {
            using (new FixedWidthLabel(label)) {
                value =
                    GUI.Toggle(
                        rect,
                        value,
                        ""
                        );

                return value;
            }
        }

        public static bool ToggleFixedWidth(GUIContent label, bool value) {
            return ToggleFixedWidth(
                EditorGUILayoutInternal.GetControlRect(
                    false,
                    16f,
                    EditorStyles.toggle,
                    null
                    ),
                label,
                value);
        }

        public static bool ToggleFixedWidth(string label, bool value) {
            return ToggleFixedWidth(new GUIContent(label), value);
        }

        public static float FloatFieldFixedWidth(Rect rect, GUIContent label, float value) {
            using (new FixedWidthLabel(label)) {
                value =
                    EditorGUI.FloatField(
                        rect,
                        "",
                        value
                        );

                return value;
            }
        }

        public static float FloatFieldFixedWidth(GUIContent label, float value) {
            return FloatFieldFixedWidth(
                EditorGUILayoutInternal.GetControlRect(
                    false,
                    16f,
                    EditorStyles.textField,
                    null
                    ),
                label,
                value);
        }

        public static int IntSliderFixedWidth(GUIContent label, int value, int leftValue, int rightValue) {
            using (new FixedWidthLabel(label)) {
                value =
                    EditorGUI.IntSlider(
                        EditorGUILayoutInternal.GetControlRect(
                            false,
                            16f,
                            EditorStyles.toggle,
                            null
                            ),
                        "",
                        value,
                        leftValue,
                        rightValue
                        );

                return value;
            }
        }

        public static int IntSliderFixedWidth(string label, int value, int leftValue, int rightValue) {
            return IntSliderFixedWidth(new GUIContent(label), value, leftValue, rightValue);
        }
    }

    /// <summary>
    ///     Reflection wrapper around EditorGUI internal members.
    /// </summary>
    public static class EditorGUIInternal {
        public const float kNumberW = 40f;
        private static readonly Type _type;
        private static readonly Func<SerializedProperty, GUIContent, bool, float> _getPropertyHeightFunc;
        private static readonly Action<Rect, SerializedProperty, GUIContent> _layerMaskFieldDelegateDelegateFunc;

        static EditorGUIInternal() {
            _type = typeof(EditorGUI);
            
            MethodInfo method;
            method = _type
                .GetMethod(
                    "GetPropertyHeight",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new Type[] {typeof (SerializedProperty), typeof (GUIContent), typeof (bool)},
                    null
                );
            _getPropertyHeightFunc = (Func<SerializedProperty, GUIContent, bool, float>) Delegate.CreateDelegate(typeof(Func<SerializedProperty, GUIContent, bool, float>), method);

            method = _type
                .GetMethod(
                    "LayerMaskField",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                    null,
                    new Type[] {typeof(Rect), typeof(SerializedProperty), typeof(GUIContent)},
                    null
                );
            _layerMaskFieldDelegateDelegateFunc = (Action<Rect, SerializedProperty, GUIContent>) Delegate.CreateDelegate(typeof(Action<Rect, SerializedProperty, GUIContent>), method);
        }

        public static float GetPropertyHeight(SerializedProperty property, GUIContent label, bool includeChildren) {
            return _getPropertyHeightFunc(property, label, includeChildren);
        }

        public static void LayerMaskField(Rect position, SerializedProperty property, GUIContent label) {
            _layerMaskFieldDelegateDelegateFunc(position, property, label);
        }
    }

    #region EditorGUIUtility internal

    /// <summary>
    ///     Reflection wrapper around EditorGUIUtility internal members.
    /// </summary>
    public static class EditorGUIUtilityInternal {
        public static float labelWidth {
            get {
#if PRE_UNITY_4_3
                Type type = typeof(EditorGUIUtility);
                FieldInfo info = type.GetField("labelWidth", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (info != null) {
                    object value = info.GetValue(null);
                    return (float) value;
                }

                return 0f;
#else
                return EditorGUIUtility.labelWidth;
#endif
            }
        }

        public static GUIContent[] TempContent(string[] texts) {
            GUIContent[] array = new GUIContent[texts.Length];
            for (int i = 0; i < texts.Length; i++) {
                array[i] = new GUIContent(texts[i]);
            }
            return array;
        }

    }

    #endregion

    #region EditorStyles internal

    /// <summary>
    ///     Reflection wrapper around EditorStyles internal members.
    /// </summary>
    public static class EditorStylesInternal {
        public static GUIStyle helpBox {
            get {
                Type type = typeof(EditorStyles);
                PropertyInfo info = type.GetProperty("helpBox", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (info != null) {
                    object value = info.GetValue(type, null);
                    return (GUIStyle) value;
                }

                return EditorStyles.label;
            }
        }
    }

    #endregion
    /// <summary>
    ///     Reflection wrapper around EditorGUILayout internal members.
    /// </summary>
    public static class EditorGUILayoutInternal {
        public const float kLabelFloatMinW = 80f + EditorGUIInternal.kNumberW + 5f;
        public const float kLabelFloatMaxW = 80f + EditorGUIInternal.kNumberW + 5f;
        public const float kPlatformTabWidth = 30f;

        public static Rect GetControlRect(bool hasLabel, float height, GUIStyle style, params GUILayoutOption[] options) {
            Rect rect = GUILayoutUtility.GetRect(!hasLabel ? EditorGUIInternal.kNumberW : kLabelFloatMinW,
                kLabelFloatMaxW, height, height, style, options);

#if PRE_UNITY_4_3
            rect.yMin -= 2f;
#endif

            return rect;
        }
    }

    #region PropertyEditor

    public abstract class PropertyEditor<TObject> : UnityEditor.Editor where TObject : MonoBehaviour {
        protected List<TObject> _objectList = new List<TObject>();
        private static readonly Dictionary<string, PropertyInfo> _serializedPropertyTypeProperties = new Dictionary<string, PropertyInfo>();
        private static readonly Type _type;
        private static readonly Dictionary<string, PropertyInfo> _typeProperties = new Dictionary<string, PropertyInfo>();
        private static Type _serializedPropertyType;

        static PropertyEditor() {
            _type = typeof(TObject);

            _serializedPropertyType = typeof(SerializedProperty);
            PropertyInfo[] serializedPropertyTypePropertyInfos = _serializedPropertyType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo propertyInfo in serializedPropertyTypePropertyInfos) {
                _serializedPropertyTypeProperties.Add(propertyInfo.Name, propertyInfo);
            }
        }

        protected virtual void OnEnable() {
            OnSelectionChange();
        }

        protected void OnSelectionChange() {
            _objectList.Clear();
            if (serializedObject.isEditingMultipleObjects) {
                foreach (Object targetObject in serializedObject.targetObjects) {
                    TObject obj = targetObject as TObject;
                    if (obj != null)
                        _objectList.Add(obj);
                }
            } else {
                TObject obj = serializedObject.targetObject as TObject;
                if (obj != null)
                    _objectList.Add(obj);
            }
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            Undo.RecordObjects(serializedObject.targetObjects, "Undo" + _type.Name);
        }

        protected Rect BeginProperty(SerializedProperty property, GUIContent label, float height) {
            Rect position =
                EditorGUILayoutInternal.GetControlRect(
                    true,
                    height,
                    EditorStyles.layerMaskField, 
                    null);
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            return position;
        }

        protected Rect BeginProperty(SerializedProperty property, GUIContent label) {
            return BeginProperty(property, label, EditorGUIInternal.GetPropertyHeight(property, label, false));
        }

        protected bool EndProperty() {
            bool result = EditorGUI.EndChangeCheck();
            EditorGUI.EndProperty();
            return result;
        }

        protected T DoFieldProperty<T>(SerializedProperty property, GUIContent label, string propertyName, Func<Rect, GUIContent, SerializedProperty, T> guiFunc) {
            if (property == null)
                throw new ArgumentNullException("property", "Property name: " + propertyName);

            if (guiFunc == null)
                throw new ArgumentNullException("guiFunc");

            Rect fieldRect = BeginProperty(property, label);

            object returnValue = null;
            object tempValue = guiFunc(fieldRect, label, property);
            if (property.propertyType == SerializedPropertyType.LayerMask)
                return default(T);

            PropertyInfo serializedPropertyInfo;
            _serializedPropertyTypeProperties.TryGetValue(GetSerializedPropertyValuePropertyName(property), out serializedPropertyInfo);
            if (EndProperty()) {
                PropertyInfo propertyInfo;
                if (!_typeProperties.TryGetValue(propertyName, out propertyInfo)) {
                    propertyInfo = _type.GetProperty(propertyName);
                    if (propertyInfo == null) 
                        throw new MissingMemberException(string.Format("Property '{0}' not found in type {1}", propertyName, _type));

                    _typeProperties.Add(propertyName, propertyInfo);
                }

                foreach (Object targetObject in serializedObject.targetObjects) {
                    propertyInfo.SetValue(targetObject, tempValue, null);
                    returnValue = ConvertSerializedPropertyValue(property, propertyInfo.GetValue(targetObject, null));
                    serializedPropertyInfo.SetValue(property, returnValue, null);
                    returnValue = serializedPropertyInfo.GetValue(property, null);
                }
            }

            if (returnValue == null)
                return (T) ConvertOutValue<T>(property, serializedPropertyInfo.GetValue(property, null));

            return (T) ConvertOutValue<T>(property, returnValue);
        }

        protected object ConvertOutValue<T>(SerializedProperty property, object obj) {
            if (property.propertyType == SerializedPropertyType.Color) {
                if (typeof(T) == typeof(Color32)) {
                    if (obj is Color) {
                        Color32 color = (Color) obj;
                        return color;
                    }

                    return (Color32) obj;
                }

                return (Color) obj;
            }

            return obj;
        }

        protected object ConvertSerializedPropertyValue(SerializedProperty property, object obj) {
            if (property.propertyType == SerializedPropertyType.Enum) {
                return (int) obj;
            }

            if (property.propertyType == SerializedPropertyType.Color) {
                if (obj is Color32) {
                    Color color = (Color32) obj;
                    return color;
                }

                return (Color) obj;
            }

            return obj;
        }

        protected string GetSerializedPropertyValuePropertyName(SerializedProperty property) {
            switch (property.propertyType) {
                case SerializedPropertyType.Boolean:
                    return "boolValue";
                case SerializedPropertyType.Integer:
                    return "intValue";
                case SerializedPropertyType.Float:
                    return "floatValue";
                case SerializedPropertyType.String:
                    return "stringValue";
                case SerializedPropertyType.Rect:
                    return "rectValue";
                case SerializedPropertyType.Color:
                    return "colorValue";
                case SerializedPropertyType.Vector2:
                    return "vector2Value";
                case SerializedPropertyType.Vector3:
                    return "vector3Value";
                case SerializedPropertyType.Enum:
                    return "enumValueIndex";
                case SerializedPropertyType.ObjectReference:
                    return "objectReferenceValue";
                default:
                    throw new ArgumentException("Unknown property type " + property.propertyType);
            }
        }
    }

    #endregion
}