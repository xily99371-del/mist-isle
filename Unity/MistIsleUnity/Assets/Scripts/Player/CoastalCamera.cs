using UnityEngine;

namespace MistIsle.Player
{
    public sealed class CoastalCamera : MonoBehaviour
    {
        public Transform Target;
        public Rect Bounds;
        public Vector2? FocusPoint;
        public void ClearFocus() { FocusPoint = null; }
        public void Snap()
        {
            UpdateViewport();
            if (Target != null) transform.position = Desired();
        }
        private void UpdateViewport()
        {
            if (Screen.width < 1 || Screen.height < 1) return;
            const float canvasAspect = 1280f / 720f;
            float windowAspect = Screen.width / (float)Screen.height;
            Rect viewport;
            if (windowAspect > canvasAspect)
            {
                float width = canvasAspect / windowAspect;
                viewport = new Rect((1f - width) * .5f, 0f, width, 1f);
            }
            else
            {
                float height = windowAspect / canvasAspect;
                viewport = new Rect(0f, (1f - height) * .5f, 1f, height);
            }
            var camera = GetComponent<Camera>();
            if (camera.rect == viewport) return;
            camera.rect = viewport;
            camera.ResetAspect();
        }
        private Vector3 Desired()
        {
            var camera = GetComponent<Camera>();
            float h = camera.orthographicSize;
            float w = h * camera.aspect;
            Vector2 p = FocusPoint ?? (Vector2)Target.position;
            float x = Bounds.width > w * 2f ? Mathf.Clamp(p.x, Bounds.xMin + w, Bounds.xMax - w) : Bounds.center.x;
            float y = Bounds.height > h * 2f ? Mathf.Clamp(p.y, Bounds.yMin + h, Bounds.yMax - h) : Bounds.center.y;
            return new Vector3(x, y, -10f);
        }
        private void LateUpdate()
        {
            if (Target == null) return;
            UpdateViewport();
            transform.position = Vector3.Lerp(transform.position, Desired(), 1f - Mathf.Exp(-8f * Time.deltaTime));
        }
    }
}
