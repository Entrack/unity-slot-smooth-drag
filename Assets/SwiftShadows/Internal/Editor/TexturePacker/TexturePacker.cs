using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LostPolygon.SwiftShadows.Internal.Editor {
    public class TexturePacker : EditorWindow {
        [SerializeField]
        private int _atlasSize = 1024;

        [SerializeField]
        private int _atlasPadding = 0;

        [SerializeField]
        private static readonly Vector2 _windowSize = new Vector2(380f, 101f);

        private bool _isFirstDrawn;

        [MenuItem("Tools/Lost Polygon/Swift Shadows/Sprite Packer")]
        private static void ShowWindow() {
            TexturePacker window = GetWindow<TexturePacker>(true, "Swift Shadows Sprite Packer");
            window.minSize = _windowSize;
            window.maxSize = _windowSize;
        }

        private void OnGUI() {
            if (!_isFirstDrawn && Event.current.type == EventType.Layout) {
                Rect rect = position;
                rect.width = _windowSize.x;
                rect.height = _windowSize.y;
                position = rect;

                _isFirstDrawn = true;
            }

            Texture2D[] inputTextures = 
                Selection.objects
                .OfType<Texture2D>()
                .Where(tex => tex.GetTextureImporter().spriteImportMode == SpriteImportMode.None)
                .ToArray();

            GUILayout.Space(1f);
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label("Max atlas size: ", GUILayout.Width(90f));
                _atlasSize =
                    EditorGUILayout.IntPopup(
                        _atlasSize,
                        new[] { "256", "512", "1024", "2048", "4096" },
                        new[] { 256, 512, 1024, 2048, 4096 },
                        GUILayout.Width(50)
                        );

                GUILayout.Label("Padding (px): ", GUILayout.Width(80f));
                _atlasPadding = EditorGUILayout.IntSlider(_atlasPadding, 0, 10);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(GUILayout.Width(200f));
            {
                EditorGUILayout.HelpBox(
                    inputTextures.Length > 0 ? 
                    string.Format("Selected {0} texture(s)", inputTextures.Length) :
                    "Select textures in the Project view first.", 
                    MessageType.Info,
                    true);
            }
            EditorGUILayout.EndHorizontal();

            GUI.enabled = inputTextures.Length > 0;
            if (GUILayout.Button("Pack into atlas")) {
                string atlasPath = EditorUtility.SaveFilePanelInProject("Save atlas image", "Atlas.png", "png", "Please enter a file name to save the atlas to");
                if (!string.IsNullOrEmpty(atlasPath)) {
                    Texture2D atlasTexture;
                    TextureImporter atlasTextureImported;

                    NaturalStringComparer naturalStringComparer = new NaturalStringComparer();
                    List<Texture2D> sortedInputTextures = inputTextures.ToList();
                    sortedInputTextures.Sort((tex1, tex2) => {
                        string path1 = AssetDatabase.GetAssetPath(tex1);
                        string path2 = AssetDatabase.GetAssetPath(tex2);

                        return naturalStringComparer.Compare(path1, path2);
                    });
                    inputTextures = sortedInputTextures.ToArray();

                    PackTextures(inputTextures, atlasPath, _atlasSize, _atlasPadding, out atlasTexture, out atlasTextureImported);
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath(atlasPath, typeof(Texture2D)));
                    Debug.Log(string.Format("Atlas image saved to \"{0}\"", atlasPath));
                }
            }
        }

        private void OnSelectionChange() {
            Repaint();
        }

        public static void PackTextures(
            Texture2D[] inputTextures, 
            string atlasPath, 
            int atlasSize, 
            int atlasPadding, 
            out Texture2D atlasTexture, 
            out TextureImporter atlasTextureImporter) {
            List<PackedTextureInfo> inputTexturesList = inputTextures.Select(texture2D => new PackedTextureInfo(texture2D)).ToList();
            
            // Force each input texture to be readable
            foreach (PackedTextureInfo textureInfo in inputTexturesList) {
                textureInfo.SetIsReadable(true);
            }

            // Try to pack textures
            try {
                // Allocate new texture for atlas and pack
                atlasTexture = new Texture2D(atlasSize, atlasSize, TextureFormat.ARGB32, false, true);
                Rect[] packedRects = atlasTexture.PackTextures(inputTextures, atlasPadding, atlasSize, false);

                // Preserve original dimensions for later use
                int atlasTextureWidth = atlasTexture.width;
                int atlasTextureHeight = atlasTexture.height;

                if (atlasTexture.format != TextureFormat.ARGB32) {
                    // Retrieve data from atlas and destroy it.
                    // We have to do this because we want ARGB32 texture format
                    // so it'd be possible to save it as PNG, not DXT
                    // or whatever Texture2D.PackTextures may pack them into
                    Color32[] atlasPixels = atlasTexture.GetPixels32();
                    DestroyImmediate(atlasTexture);

                    // Allocate new atlas texture and copy data as ARGB32
                    atlasTexture = new Texture2D(atlasTextureWidth, atlasTextureHeight, TextureFormat.ARGB32, false, true);
                    atlasTexture.SetPixels32(atlasPixels);
                }

                // Encode atlas to PNG and write 
                byte[] atlasPngData = atlasTexture.EncodeToPNG();
                File.WriteAllBytes(atlasPath, atlasPngData);

                // Free the in-memory texture and re-import it as an asset
                DestroyImmediate(atlasTexture);
                AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);

                atlasTexture = AssetDatabase.LoadAssetAtPath(atlasPath, typeof(Texture2D)) as Texture2D;

                if (atlasTexture == null)
                    throw new FileLoadException(string.Format("Could not load atlas texture '{0}'", atlasPath));

                // Get TextureImporter and setup it for sprites
                atlasTextureImporter = atlasTexture.GetTextureImporter();
                atlasTextureImporter.spriteImportMode = SpriteImportMode.Multiple;
                atlasTextureImporter.textureType = TextureImporterType.Default;
                atlasTextureImporter.maxTextureSize = atlasSize;
                atlasTextureImporter.mipmapEnabled = true;
                atlasTextureImporter.alphaIsTransparency = true;
                TextureImporterSettings atlasTextureImporterSettings = new TextureImporterSettings();
                atlasTextureImporter.ReadTextureSettings(atlasTextureImporterSettings);
                atlasTextureImporterSettings.spriteMeshType = SpriteMeshType.FullRect;
                atlasTextureImporter.SetTextureSettings(atlasTextureImporterSettings);

                // Fill sprite metadata for each texture
                SpriteMetaData[] atlasSpriteMetaData = new SpriteMetaData[packedRects.Length];
                for (int i = 0; i < packedRects.Length; i++) {
                    Rect rect = packedRects[i];
                    rect.x *= atlasTextureWidth;
                    rect.y *= atlasTextureHeight;
                    rect.width *= atlasTextureWidth;
                    rect.height *= atlasTextureHeight;

                    SpriteMetaData spriteMetaData = new SpriteMetaData();
                    spriteMetaData.name = inputTexturesList[i].Name;
                    spriteMetaData.alignment = 0;
                    spriteMetaData.rect = rect;

                    // Center pivot
                    spriteMetaData.pivot = 
                        new Vector2(
                            (spriteMetaData.rect.xMax - spriteMetaData.rect.xMin) / 2f,
                            (spriteMetaData.rect.yMax - spriteMetaData.rect.yMin) / 2f
                            );

                    atlasSpriteMetaData[i] = spriteMetaData;
                }

                // Apply spritesheet
                atlasTextureImporter.spritesheet = atlasSpriteMetaData;
                atlasTextureImporter.ImportNow();
            } finally {
                // Restore IsReadable on input textures
                foreach (PackedTextureInfo textureInfo in inputTexturesList) {
                    textureInfo.SetIsReadable(textureInfo.IsReadable);
                }
            }
        }

        private class PackedTextureInfo {
            public readonly Texture2D Texture2D;
            public readonly bool IsReadable;
            public readonly string Name;
            private readonly TextureImporter _textureImporter;

            public PackedTextureInfo(Texture2D texture2D) {
                Texture2D = texture2D;
                _textureImporter = Texture2D.GetTextureImporter();
                IsReadable = _textureImporter.isReadable;
                Name = texture2D.name;
            }

            public void SetIsReadable(bool isReadable) {
                if (_textureImporter.isReadable == isReadable)
                    return;

                _textureImporter.isReadable = isReadable;
                _textureImporter.ImportNow();
            }
        }

        private class NaturalStringComparer : IComparer<string> {
            private static readonly Regex _regexp = new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);
        
            public int Compare(string x, string y) {
                x = x.ToLower();
                y = y.ToLower();
                if (string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0) {
                    if (x.Length == y.Length) return 0;
                    return x.Length < y.Length ? -1 : 1;
                }

                var a = _regexp.Split(x);
                var b = _regexp.Split(y);
                int i = 0;
                while (true) {
                    int r = PartCompare(a[i], b[i]);
                    if (r != 0) return r;
                    ++i;
                }
            }
        
            private static int PartCompare(string x, string y) {
                int a, b;
                if (int.TryParse(x, out a) && int.TryParse(y, out b))
                    return a.CompareTo(b);

                return string.Compare(x, y, StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }
}