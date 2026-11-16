using UnityEngine;

namespace MistIsle.Player
{
    public sealed class CoastalCamera : MonoBehaviour
    {
        public Transform Target;
        public Rect Bounds;
        public void Snap()
        {
            if (Target != null) transform.position = Desired();
        }
        private Vector3 Desired()
        {
            var camera = GetComponent<Camera>();
            float h = camera.orthographicSize;
            float w = h * camera.aspect;
            var p = Target.position;
            float x = Bounds.width > w * 2f ? Mathf.Clamp(p.x, Bounds.xMin + w, Bounds.xMax - w) : Bounds.center.x;
            float y = Bounds.height > h * 2f ? Mathf.Clamp(p.y, Bounds.yMin + h, Bounds.yMax - h) : Bounds.center.y;
            return new Vector3(x, y, -10f);
        }
        private void LateUpdate()
        {
            if (Target == null) return;
            transform.position = Vector3.Lerp(transform.position, Desired(), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }
}
