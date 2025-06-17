using UnityEngine;
using UnityEngine.Events;

namespace OpenUp.Environment.Features
{
    public class SpawnPoint : MonoBehaviour
    {
        public UnityEvent<Pose> OnReposition = new UnityEvent<Pose>();
        public Pose pose => new Pose(transform.position, transform.rotation);

        public void Reposition(Pose pose)
        {
            transform.position = pose.position;
            transform.rotation = pose.rotation;
            OnReposition.Invoke(pose);
        }
    }
}