/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Hit;
using UnityEngine;

namespace TouchScript.Layers
{
    /// <summary>
    /// Touch layer which represents a 3d camera looking into the world. Determines which objects may be hit in the view of a camera attached to parent GameObject.
    /// </summary>
    [AddComponentMenu("TouchScript/Layers/Camera Layer Alt")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Layers_CameraLayer.htm")]
    public class CameraLayerAlt : CameraLayerBase
    {
        #region Private variables

        private List<RaycastHit2D> sortedHits = new List<RaycastHit2D>(20);
        private Transform cachedTransform;
        private List<HitTest> tmpHitTestList = new List<HitTest>(10);

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            if (!Application.isPlaying) return;

            cachedTransform = GetComponent<Transform>();
        }

        #endregion

        #region Protected functions

        /// <inheritdoc />
        protected override LayerHitResult castRay(Ray ray, out TouchHit hit)
        {
            hit = default(TouchHit);
            var raycastHits = Physics2D.GetRayIntersectionAll(ray, float.PositiveInfinity, LayerMask);

            if (raycastHits.Length == 0) return LayerHitResult.Miss;
            if (raycastHits.Length > 1)
            {
                sortHits(raycastHits);

                RaycastHit2D raycastHit = default(RaycastHit2D);
                var i = 0;
                while (i < sortedHits.Count)
                {
                    raycastHit = sortedHits[i];
                    switch (doHit(raycastHit, out hit))
                    {
                        case HitTest.ObjectHitResult.Hit:
                            return LayerHitResult.Hit;
                        case HitTest.ObjectHitResult.Discard:
                            return LayerHitResult.Miss;
                    }
                    i++;
                }
            }
            else
            {
                switch (doHit(raycastHits[0], out hit))
                {
                    case HitTest.ObjectHitResult.Hit:
                        return LayerHitResult.Hit;
                    case HitTest.ObjectHitResult.Error:
                        return LayerHitResult.Error;
                    default:
                        return LayerHitResult.Miss;
                }
            }

            return LayerHitResult.Miss;
        }

        private HitTest.ObjectHitResult doHit(RaycastHit2D raycastHit, out TouchHit hit)
        {
            hit = new TouchHit(raycastHit);
            raycastHit.transform.GetComponents(tmpHitTestList);
            var count = tmpHitTestList.Count;
            if (count == 0) return HitTest.ObjectHitResult.Hit;

            var hitResult = HitTest.ObjectHitResult.Hit;
            for (var i = 0; i < count; i++)
            {
                var test = tmpHitTestList[i];
                if (!test.enabled) continue;
                hitResult = test.IsHit(hit);
                if (hitResult == HitTest.ObjectHitResult.Miss || hitResult == HitTest.ObjectHitResult.Discard) break;
            }

            return hitResult;
        }

        private void sortHits(RaycastHit2D[] hits)
        {
            Vector2 cameraPos = cachedTransform.position;
            sortedHits.Clear();
            sortedHits.AddRange(hits);
            sortedHits.Sort((a, b) =>
            {
                if (a.collider.transform == b.collider.transform) return 0;
                var distA = (a.point - cameraPos).sqrMagnitude;
                var distB = (b.point - cameraPos).sqrMagnitude;
                return distA < distB ? -1 : 1;
            });
        }

        #endregion
    }
}