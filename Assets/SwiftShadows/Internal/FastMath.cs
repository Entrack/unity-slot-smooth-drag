#define HAS_STRUCTLAYOUT_SUPPORT

using System.Runtime.InteropServices;
using UnityEngine;

namespace LostPolygon.SwiftShadows.Internal {
    /// <summary>
    /// A collection of math utilities that provide better performance.
    /// </summary>
    public static class FastMath {
        public const float kDoublePi = Mathf.PI * 2f;
        public const float kInvDoublePi = 1f / kDoublePi;
        public const float kHalfPi = Mathf.PI / 2f;
        public const float kDeg2Rad = 1f / 180f * Mathf.PI;

#if HAS_STRUCTLAYOUT_SUPPORT
        /// <summary>
        /// The union of float and int sharing the same location in memory.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct FloatIntUnion {
            [FieldOffset(0)]
            public float f;

            [FieldOffset(0)]
            public int i;
        }
#endif

        /// <summary>
        /// Calculates the approximate square root of a given value.
        /// </summary>
        /// <remarks>
        /// This function is much faster than <c>Mathf.Sqrt()</c>, especially on mobile devices.
        /// </remarks>
        /// <param name="x">
        /// Input value.
        /// </param>
        /// <returns>
        /// The approximate value of square root of <paramref name="x"/>.
        /// </returns>
        public static float FastSqrt(float x) {
#if HAS_STRUCTLAYOUT_SUPPORT
            FloatIntUnion u;
            u.i = 0;
            u.f = x;
            float xhalf = 0.5f * x;
            u.i = 0x5f375a86 - (u.i >> 1);
            u.f = u.f * (1.5f - xhalf * u.f * u.f);
            return u.f * x;
#else
            return Mathf.Sqrt(x);
#endif
        }

        /// <summary>
        /// Calculates the approximate inverse magnitude of a vector.
        /// </summary>
        /// <remarks>
        /// This function is much faster than <c>Vector3.magnitude</c>, especially on mobile devices.
        /// </remarks>
        /// <param name="vector">
        /// Input vector.
        /// </param>
        /// <returns>
        /// The approximate value of inverse magnitude of <paramref name="vector"/>.
        /// </returns>
        public static float FastInvMagnitude(this Vector3 vector) {
#if HAS_STRUCTLAYOUT_SUPPORT
            float magnitude = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;
            FloatIntUnion u;
            u.i = 0;
            u.f = magnitude;
            float xhalf = 0.5f * magnitude;
            u.i = 0x5f375a86 - (u.i >> 1);
            u.f = u.f * (1.5f - xhalf * u.f * u.f);
            return u.f;
#else
            return 1f / vector.magnitude;
#endif
        }

        /// <summary>
        /// Calculates the approximate magnitude of a vector.
        /// </summary>
        /// <remarks>
        /// This function is much faster than <c>Vector3.magnitude</c>, especially on mobile devices.
        /// </remarks>
        /// <param name="vector">
        /// Input vector.
        /// </param>
        /// <returns>
        /// The approximate value of magnitude of <paramref name="vector"/>.
        /// </returns>
        public static float FastMagnitude(this Vector3 vector) {
#if HAS_STRUCTLAYOUT_SUPPORT
            float magnitude = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;

            FloatIntUnion u;
            u.i = 0;
            u.f = magnitude;
            float xhalf = 0.5f * magnitude;
            u.i = 0x5f375a86 - (u.i >> 1);
            u.f = u.f * (1.5f - xhalf * u.f * u.f);
            return u.f * magnitude;
#else
            return vector.magnitude;
#endif
        }

        /// <summary>
        /// Calculates the approximate normalized vector.
        /// </summary>
        /// <remarks>
        /// This function is much faster than <c>Vector3.normalized</c>, especially on mobile devices.
        /// </remarks>
        /// <param name="vector">
        /// Input vector.
        /// </param>
        /// <returns>
        /// The approximate value of magnitude of <paramref name="vector"/>.
        /// </returns>
        public static Vector3 FastNormalized(this Vector3 vector) {
#if HAS_STRUCTLAYOUT_SUPPORT
            float magnitude = vector.x * vector.x + vector.y * vector.y + vector.z * vector.z;

            FloatIntUnion u;
            u.i = 0;
            u.f = magnitude;
            float xhalf = 0.5f * magnitude;
            u.i = 0x5f375a86 - (u.i >> 1);
            u.f = u.f * (1.5f - xhalf * u.f * u.f);

            vector.x = vector.x * u.f;
            vector.y = vector.y * u.f;
            vector.z = vector.z * u.f;

            return vector;
#else
            return vector.normalized;
#endif
        }

        /// <summary>
        /// A VERY inaccurate linear approximation for acos.
        /// </summary>
        /// <remarks>
        /// This function is much faster than <c>Mathf.Acos</c>, especially on mobile devices.
        /// </remarks>
        /// <param name="x">
        /// Input value.
        /// </param>
        /// <returns>
        /// The approximate value of magnitude of <paramref name="x"/>.
        /// </returns>
        public static float FastPseudoAcos(float x) {
            return (-0.69813170079773212f * x * x - 0.87266462599716477f) * x + kHalfPi;
        }

        /// <summary>
        /// A simple substitute for Ray. Does not normalizes the direction vector.
        /// </summary>
        public struct Ray {
            public Vector3 Origin;
            public Vector3 Direction;

            public Ray(Vector3 origin, Vector3 direction) {
                Origin = origin;
                Direction = direction;
            }
        }

        /// <summary>
        /// A simple substitute for Plane.
        /// </summary>
        public struct Plane {
            public Vector3 Normal;
            public float Distance;

            public Plane(Vector3 normal, Vector3 point) {
                Normal = normal;
                Distance = -normal.x * point.x - normal.y * point.y - normal.z * point.z;
            }

            public bool Raycast(ref Ray ray, out float enter) {
                float coeff2 = ray.Direction.x * Normal.x + ray.Direction.y * Normal.y + ray.Direction.z * Normal.z;
                float coeff1 = -ray.Origin.x * Normal.x - ray.Origin.y * Normal.y - ray.Origin.z * Normal.z - Distance;
                if (coeff2 < Vector3.kEpsilon && coeff2 > -Vector3.kEpsilon) {
                    enter = 0f;
                    return false;
                }

                enter = coeff1 / coeff2;
                return enter > 0f;
            }
        }
    }
}