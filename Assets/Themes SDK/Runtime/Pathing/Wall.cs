using System;
using UnityEngine;
using UnityEngine.Events;

namespace OpenUp.Environment.Pathing
{
    public class Wall : MonoBehaviour, ITempLayer
    {
        [Serializable]
        public class OnWallHit : UnityEvent<RaycastHit> { }

        [SerializeField] public OnWallHit onWallHit = new OnWallHit();

        private int existingLayer;
        private IPathing pathing;

        private void OnDestroy()
        {
            if (pathing != null)
                pathing.RemoveWall(this);
        }

        public void SetPathing(IPathing pathing)
        {
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
    }
}