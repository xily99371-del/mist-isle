using UnityEngine;

namespace MistIsle.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class TopDownPlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.2f;
        private Rigidbody2D body;
        private Vector2 input;
        public bool InputEnabled { get; set; } = true;
        public Vector2 Facing { get; private set; } = Vector2.down;
        public bool IsMoving => input.sqrMagnitude > 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.bodyType = RigidbodyType2D.Dynamic;
        }

        private void Update()
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (!InputEnabled) input = Vector2.zero;
            else if (Mathf.Abs(input.x) > 0f) input.y = 0f;
            if (input.sqrMagnitude > 0f) Facing = input;
        }

        private void FixedUpdate()
        {
            body.linearVelocity = input * moveSpeed;
        }
    }
}
