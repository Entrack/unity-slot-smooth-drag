#if (UNITY_EDITOR || UNITY_ANDROID || UNITY_STANDALONE) && !ENABLE_IL2CPP
#  define SUPPORTS_REFLECTION
#endif

#if SUPPORTS_REFLECTION

using System;
using System.Reflection;
using UnityEngine;

namespace LostPolygon.SwiftShadows.Internal {
    /// <summary>
    /// A reflection wrapper around GeometryUtility internal methods. 
    /// </summary>
    public static class GeometryUtilityInternal {
        private static readonly Action<Plane[], Matrix4x4> Internal_ExtractPlanesDelegate;

        static GeometryUtilityInternal() {
            MethodInfo methodInfo =
                typeof(GeometryUtility)
                    .GetMethod(
                        "Internal_ExtractPlanes",
                        BindingFlags.NonPublic | BindingFlags.Static);

            if (methodInfo != null) {
                Internal_ExtractPlanesDelegate = (Action<Plane[], Matrix4x4>) Delegate.CreateDelegate(typeof(Action<Plane[], Matrix4x4>), methodInfo, false);
            }
        }

        /// <summary>
        /// Extract frustum planes from world-to-projection matrix.
        /// </summary>
        /// <param name="planes">
        /// The Plane[] to save planes to.
        /// </param>
        /// <param name="worldToProjectionMatrix">
        /// World to projection matrix.
        /// </param>
        public static void ExtractPlanes(Plane[] planes, Matrix4x4 worldToProjectionMatrix) {
            if (Internal_ExtractPlanesDelegate != null) {
                Internal_ExtractPlanesDelegate(planes, worldToProjectionMatrix);
            } else {
                Plane[] tempPlanes = GeometryUtility.CalculateFrustumPlanes(worldToProjectionMatrix);
                Array.Copy(tempPlanes, planes, 6);
            }
        }

        /// <summary>
        /// Extract frustum planes from camera transform.
        /// </summary>
        /// <param name="planes">
        /// The Plane[] to save planes to.
        /// </param>
        /// <param name="camera">
        /// Camera to calculate frustum from.
        /// </param>
        public static void CalculateFrustumPlanes(Plane[] planes, Camera camera) {
            ExtractPlanes(planes, camera.projectionMatrix * camera.worldToCameraMatrix);
        }
    }
}

#endif