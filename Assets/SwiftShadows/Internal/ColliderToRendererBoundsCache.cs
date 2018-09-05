using System.Collections.Generic;
using UnityEngine;
using LostPolygon.SwiftShadows.Internal;

namespace LostPolygon.SwiftShadows {
    public class ColliderToRendererBoundsCache {
        /// <summary>
        /// Maps a Collider to the Bounds of a Renderer attached to the same GameObject as Renderer.
        /// Used to avoid expensive GetComponent calls in SwiftShadow when culling.
        /// </summary>
        private readonly ExposedDictionary<Collider, Bounds> _map = new ExposedDictionary<Collider, Bounds>(new ColliderEqualityComparer());

        public ColliderToRendererBoundsCache() {
        }

        public bool GetRendererBoundsFromCollider(Collider collider, out Bounds bounds) {
            if (!_map.TryGetValue(collider, out bounds)) {
                Renderer renderer = collider.GetComponent<Renderer>();
                if (renderer == null)
                    return false;

                bounds = renderer.bounds;
                _map.Add(collider, bounds);
            }

            return true;
        }

        public void Clear() {
            _map.Clear();
        }

        private sealed class ColliderEqualityComparer : IEqualityComparer<Collider> {
            public bool Equals(Collider x, Collider y) {
                // because two colliders can't have the same instance IDs
                return true;
            }

            public int GetHashCode(Collider obj) {
                return obj.GetInstanceID();
            }
        }
    }
}