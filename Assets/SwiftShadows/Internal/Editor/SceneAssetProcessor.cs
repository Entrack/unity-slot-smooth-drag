using System.IO;
using UnityEngine;

namespace LostPolygon.SwiftShadows.Internal.Editor {
    /// <summary>
    /// Clears resources on scene save to avoid annoying messages.
    /// </summary>
    public class ShadowManagerSceneAssetModificationProcessor : UnityEditor.AssetModificationProcessor {
        private static string[] OnWillSaveAssets(string[] paths) {
            // Get the name of the scene to save.
            bool isMustClear = false;
            foreach (string path in paths) {
                string fileName = Path.GetFileName(path);

                // "ProjectSettings/ProjectSettings.asset" is saved on build.
                // Scene is saved by Ctrl+S.
                // In both situations we have to clear the Editor camera ShadowsCameraEvents
                if (path.StartsWith("ProjectSettings") ||
                    (fileName != null && fileName.EndsWith(".unity"))) {
                    isMustClear = true;
                    break;
                }
            }

            if (!isMustClear) {
                return paths;
            }
            
            if (!ShadowManager.IsDestroyed) {
                ShadowManager.Instance.Clear(false, true);
                ShadowManagerEditorHelper.Instance.SetIsMustUpdate();
            }

            return paths;
        }
    }
}