using UnityEngine;

namespace MistIsle.Player
{
    [DefaultExecutionOrder(100)]
    public sealed class TravelerPresentation : MonoBehaviour
    {
        public Sprite[] Down;
        public Sprite[] Left;
        public Sprite[] Right;
        public Sprite[] Up;
        private TopDownPlayerMotor motor;
        private SpriteRenderer visual;
        private SpriteFrameAnimator frameAnimator;

        private void Awake()
        {
            motor = GetComponent<TopDownPlayerMotor>();
            visual = GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (visual == null || motor == null) return;
            if (frameAnimator == null && Down != null && Left != null && Right != null && Up != null)
            {
                var frames = new System.Collections.Generic.List<Sprite>();
                frames.AddRange(Down); frames.AddRange(Left); frames.AddRange(Right); frames.AddRange(Up);
                frameAnimator = GetComponent<SpriteFrameAnimator>();
                if (frameAnimator == null) frameAnimator = gameObject.AddComponent<SpriteFrameAnimator>();
                frameAnimator.Configure(visual, frames.ToArray(), Down.Length, 8f, false);
            }
            if (frameAnimator != null) frameAnimator.SetMotion(motor.Facing, motor.IsMoving);
        }

        private void LateUpdate()
        {
            if (visual == null) return;
            visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
    }
}
