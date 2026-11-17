using System;
using UnityEngine;

namespace MistIsle.Player
{
    /// <summary>Plays authored South/West/East/North frames; a stopped actor holds its authored idle frame.</summary>
    [DisallowMultipleComponent]
    public sealed class SpriteFrameAnimator : MonoBehaviour
    {
        public enum FacingDirection { South, West, East, North }

        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1)] private int framesPerDirection = 3;
        [SerializeField, Min(.1f)] private float walkFramesPerSecond = 7f;
        [SerializeField] private int idleFrame;
        [SerializeField] private bool inferMotionFromTransform = true;
        [SerializeField] private FacingDirection facing = FacingDirection.South;
        private Vector3 previousPosition;
        private bool moving;
        private float elapsed;
        private bool hasPosition;

        public FacingDirection Facing => facing;
        public bool IsMoving => moving;
        public int FrameCount => frames == null ? 0 : frames.Length;
        public int CurrentFrameIndex { get; private set; }

        private void Awake()
        {
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            previousPosition = transform.position;
            hasPosition = true;
        }

        public void Configure(SpriteRenderer renderer, Sprite[] authoredFrames, int directionFrameCount = 3,
            float framesPerSecond = 7f, bool automaticMovement = true)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (authoredFrames == null || authoredFrames.Length != directionFrameCount * 4)
                throw new ArgumentException("Four direction rows of authored frames are required.", nameof(authoredFrames));
            if (!ReferenceEquals(target, renderer)) ActorVisualSizing.RemoveHuman(target);
            target = renderer;
            frames = (Sprite[])authoredFrames.Clone();
            framesPerDirection = directionFrameCount;
            walkFramesPerSecond = Mathf.Max(.1f, framesPerSecond);
            inferMotionFromTransform = automaticMovement;
            previousPosition = transform.position;
            hasPosition = true;
            elapsed = 0f;
            ApplyFrame();
        }

        private void OnDestroy()
        {
            ActorVisualSizing.RemoveHuman(target);
        }

        public void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude <= .00001f) return;
            FacingDirection next = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? direction.x < 0 ? FacingDirection.West : FacingDirection.East
                : direction.y > 0 ? FacingDirection.North : FacingDirection.South;
            facing = next;
            ApplyFrame();
        }

        public void Face(FacingDirection direction)
        {
            facing = direction;
            ApplyFrame();
        }

        public void SetMotion(Vector2 direction, bool isMoving)
        {
            inferMotionFromTransform = false;
            Face(direction);
            if (moving != isMoving) elapsed = 0f;
            moving = isMoving;
            ApplyFrame();
        }

        public void UseAutomaticMotion(bool enabled)
        {
            inferMotionFromTransform = enabled;
            previousPosition = transform.position;
            hasPosition = true;
        }

        public void ResetAfterTeleport()
        {
            previousPosition = transform.position;
            hasPosition = true;
            moving = false;
            elapsed = 0f;
            ApplyFrame();
        }

        private void LateUpdate()
        {
            if (inferMotionFromTransform)
            {
                Vector3 current = transform.position;
                Vector2 delta = hasPosition ? (Vector2)(current - previousPosition) : Vector2.zero;
                previousPosition = current;
                hasPosition = true;
                bool nextMoving = delta.sqrMagnitude > .000001f && delta.sqrMagnitude < 9f;
                if (moving != nextMoving) elapsed = 0f;
                moving = nextMoving;
                if (moving) Face(delta);
            }
            elapsed = moving ? elapsed + Time.deltaTime : 0f;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (target == null || frames == null || framesPerDirection <= 0) return;
            int localFrame = Mathf.Clamp(idleFrame, 0, framesPerDirection - 1);
            if (moving)
            {
                int step = Mathf.FloorToInt(elapsed * walkFramesPerSecond);
                // Authored three-column sheets: neutral, first support step, opposite support step.
                // Passing through neutral between supports avoids a 2 -> 0 -> 1 asymmetric hop.
                if (framesPerDirection == 3)
                {
                    int phase = step % 4;
                    localFrame = phase == 1 ? 1 : phase == 3 ? 2 : 0;
                }
                else localFrame = step % framesPerDirection;
            }
            int index = (int)facing * framesPerDirection + localFrame;
            CurrentFrameIndex = index;
            ActorVisualSizing.ApplyFacingReference(target, frames[(int)facing * framesPerDirection]);
            if (index >= 0 && index < frames.Length && frames[index] != null) target.sprite = frames[index];
        }
    }
}
