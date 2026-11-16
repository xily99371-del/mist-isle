using UnityEngine;

namespace MistIsle.World
{
    public sealed class PlayerInteractionSensor : MonoBehaviour
    {
        [SerializeField] private Transform origin;
        [SerializeField, Min(0.1f)] private float radius = 1.15f;
        [SerializeField] private LayerMask interactableLayers;
        private readonly Collider2D[] hits = new Collider2D[16];
        private readonly RaycastHit2D[] obstructions = new RaycastHit2D[16];
        public IWorldInteractable Current { get; private set; }
        public bool InputEnabled { get; set; } = true;

        private void Update()
        {
            Current = null;
            if (!InputEnabled) return;
            Vector2 point = origin != null ? origin.position : transform.position;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = interactableLayers, useTriggers = true };
            int count = Physics2D.OverlapCircle(point, radius, filter, hits);
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = hits[i].GetComponentInParent<IWorldInteractable>();
                if (candidate == null || !candidate.CanInteract) continue;
                int wallCount = Physics2D.Linecast(point, hits[i].bounds.center,
                    new ContactFilter2D { useTriggers = false }, obstructions);
                bool blocked = false;
                for (int w = 0; w < wallCount; w++)
                {
                    var wall = obstructions[w].collider;
                    if (wall != null && !wall.transform.IsChildOf(transform)
                        && wall.GetComponentInParent<IWorldInteractable>() != candidate)
                    { blocked = true; break; }
                }
                if (blocked) continue;
                float distance = Vector2.SqrMagnitude((Vector2)hits[i].bounds.center - point);
                if (distance < bestDistance) { bestDistance = distance; Current = candidate; }
            }
            if (Current != null && Input.GetKeyDown(KeyCode.E)) Current.Interact();
        }

        public void Configure(LayerMask layers, float interactionRadius = 1.4f)
        {
            interactableLayers = layers;
            radius = interactionRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin != null ? origin.position : transform.position, radius);
        }
    }
}
