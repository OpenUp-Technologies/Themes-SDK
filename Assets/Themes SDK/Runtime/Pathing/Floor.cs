using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace OpenUp.Environment.Pathing
{
    public class Floor : MonoBehaviour, ITempLayer
    {
        private int floorLayer;
        private int existingLayer;
        private bool isTempLayer;
        
        private IPathing pathing;

        private void OnDestroy()
        {
            pathing.RemoveFloor(this);
        }

        public void SetPathing(IPathing pathing)
        {
            this.pathing = pathing;
            pathing.AddFloor(this);
        }

        public void SetLayer(int layer)
        {
            if (!enabled) return;

            if (isTempLayer)
                throw new NotSupportedException($"Floor {name} cannot be in more than one temporary layer");

            existingLayer = gameObject.layer;
            gameObject.layer = layer;
            isTempLayer = true;
        }

        public void ResetLayer()
        {
            gameObject.layer = existingLayer;
            isTempLayer = false;
        }
    }
}