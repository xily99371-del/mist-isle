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

        private void Awake()
        {
            motor = GetComponent<TopDownPlayerMotor>();
            visual = GetComponentInChildren<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (visual == null || motor == null) return;
            Sprite[] frames = motor.Facing.y > 0f ? Up : motor.Facing.x < 0f ? Left : motor.Facing.x > 0f ? Right : Down;
            if (frames != null && frames.Length > 0)
                visual.sprite = frames[motor.IsMoving ? (int)(Time.time * 8f) % frames.Length : 0];
            visual.sortingOrder = 100 - Mathf.RoundToInt(transform.position.y * 10f);
        }
    }
}
