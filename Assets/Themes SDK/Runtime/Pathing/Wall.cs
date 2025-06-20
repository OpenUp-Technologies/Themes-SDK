using System;
using UnityEngine;
using UnityEngine.Events;

namespace OpenUp.Environment.Pathing
{
    [RequireComponent(typeof(Collider))]
    public class Wall : MonoBehaviour, ITempLayer
    {
        [Serializable]
        public class OnWallHit : UnityEvent<RaycastHit> { }

        [SerializeField] public OnWallHit onWallHit = new OnWallHit();

        private int existingLayer;
        private IPathing pathing;
        private Collider box;

        private void Awake()
        {
            box = GetComponent<Collider>();
            
            if (!box) Debug.LogError($"Wall {name} has no collider", gameObject);
        }
        
        private void OnDestroy()
        {
            if (pathing != null)
                pathing.RemoveWall(this);
        }

        public void SetPathing(IPathing pathing)
        {
            if (!box) return;
            
            this.pathing = pathing;
            pathing.AddWall(this);
        }

        public void SetLayer(int layer)
        {
            if (!enabled) return;
            
            existingLayer = gameObject.layer;
            gameObject.layer = layer;
        }

        public void ResetLayer() => gameObject.layer = existingLayer;

        /// <summary>
        /// Similar to DistanceToPoint but allows us to filter using the much more performant ClosestPointOnBounds.
        /// The collider is always contained within the bounds so the distance to the bounds is always less than the collider.
        /// Thus, if the distance to the collider is too great, then we don't need to check the actual collider. 
        /// This is used to make sure that mesh colliders don't eat up too much performance.
        /// </summary>
        /// <param name="point">Some arbitrary point in world space.</param>
        /// <param name="distance">A distance to compare with.</param>
        /// <returns>True if the point is closer than <paramref name="distance"/> to the collider.</returns>
        public bool IsWithinDistanceOfPoint(Vector3 point, float distance) =>
            gameObject.activeInHierarchy
         && Vector3.Distance(point, box.ClosestPointOnBounds(point)) < distance
         && Vector3.Distance(point, box.ClosestPoint(point)) < distance;
        
        public float DistanceToPoint(Vector3 point) => gameObject.activeInHierarchy 
            ? Vector3.Distance(point, box.ClosestPoint(point))
            : Single.PositiveInfinity;
    }
}