using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace OpenUp.Editor.EnvironmentsSdk
{
    [RequireComponent(typeof(Camera))]
    public class ThumbnailCam : MonoBehaviour
    {
        [SerializeField] private int resolution = 512;
            
        public RenderTexture GenerateThumbnail(GameObject target)
        {
            var cam = gameObject.GetComponent<Camera>();
            PositionCamera(cam, target);
            

            RenderTexture tex = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32) {
                name = "Render Tex",
                antiAliasing = 4,
            };
            
            cam.targetTexture = tex;
            cam.Render();
            cam.targetTexture = null;
            
            return tex;
        }
        
        private void PositionCamera(Camera cam, GameObject obj)
        {
            if (obj == null)
                return;
            
            var bounds = GetBounds(obj);

            Vector3 pos = bounds.center + obj.transform.forward * bounds.extents.magnitude;
            
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(-obj.transform.forward);
        }
        
        private Bounds GetBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            
            Bounds bounds = new Bounds();
            
            foreach (Renderer rend in renderers)
            {
                bounds.Encapsulate(rend.bounds);
            }
            
            return bounds;
        }
    }
}