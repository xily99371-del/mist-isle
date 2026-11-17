using UnityEngine;

namespace MistIsle.Player
{
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

        private void LateUpdate()
        {
            if (visual == null || motor == null) return;
            if (frameAnimator == null && Down != null && Left != null && Right != null && Up != null)
            {
                var frames = new System.Collections.Generic.List<Sprite>();
                frames.AddRange(Down); frames.AddRange(Left); frames.AddRange(Right); frames.AddRange(Up);
                frameAnimator = gameObject.AddComponent<SpriteFrameAnimator>();
                frameAnimator.Configure(visual, frames.ToArray(), Down.Length, 8f, false);
            }
            if (frameAnimator != null) frameAnimator.SetMotion(motor.Facing, motor.IsMoving);
            visual.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
    }
}
