using UnityEngine;

namespace MistIsle.World
{
    public sealed class PlayerInteractionSensor : MonoBehaviour
    {
        [SerializeField] private Transform origin;
        [SerializeField, Min(0.1f)] private float radius = 1.15f;
        [SerializeField] private LayerMask interactableLayers;
        private readonly Collider2D[] hits = new Collider2D[16];
        public IWorldInteractable Current { get; private set; }

        private void Update()
        {
            Current = null;
            Vector2 point = origin != null ? origin.position : transform.position;
            int count = Physics2D.OverlapCircleNonAlloc(point, radius, hits, interactableLayers);
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i].GetComponentInParent<IWorldInteractable>();
                if (candidate == null || !candidate.CanInteract) continue;
                float distance = Vector2.SqrMagnitude((Vector2)hits[i].bounds.center - point);
                if (distance < bestDistance) { bestDistance = distance; Current = candidate; }
            }
            if (Current != null && Input.GetKeyDown(KeyCode.E)) Current.Interact();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin != null ? origin.position : transform.position, radius);
        }
    }
}
