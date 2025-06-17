using UnityEngine;

namespace OpenUp.Environment.Pathing
{
    public interface ITempLayer
    {
        void SetLayer(int layer);
        void ResetLayer();
    }

    public interface IPathing
    {
        public void AddWall(Wall wall);
        public void RemoveWall(Wall wall);
        public void AddFloor(Floor floor);
        public void RemoveFloor(Floor floor);
    }
}