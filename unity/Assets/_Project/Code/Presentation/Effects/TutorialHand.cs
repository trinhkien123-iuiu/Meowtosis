using UnityEngine;

namespace Meowtosis.Presentation
{
    // Bàn tay hướng dẫn lướt từ ô mèo sang ô đích rồi lặp lại (Prefabs/Board/TutorialHand).
    public sealed class TutorialHand : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private float loopSeconds = 1.5f;
        [SerializeField] private AnimationCurve travel = PieceMotion.Keys(0f, 0f, 0.15f, 0f, 0.7f, 1f, 1f, 1f);
        [SerializeField] private AnimationCurve opacity = PieceMotion.Keys(0f, 0f, 0.15f, 1f, 0.8f, 1f, 1f, 0f);

        private Vector3 from;
        private Vector3 to;
        private float time;

        public void Swipe(Vector3 fromPosition, Vector3 toPosition)
        {
            from = fromPosition;
            to = toPosition;
            time = 0f;
        }

        private void Update()
        {
            time = Mathf.Repeat(time + Time.deltaTime, loopSeconds);
            var progress = time / loopSeconds;
            transform.localPosition = Vector3.Lerp(from, to, travel.Evaluate(progress));
            sprite.color = new Color(1f, 1f, 1f, opacity.Evaluate(progress));
        }
    }
}
