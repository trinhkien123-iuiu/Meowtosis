using UnityEngine;

namespace Meowtosis.Presentation
{
    // Hồn mèo trắng mờ có vầng hào quang và mắt @@ bay lên khỏi ô độc (Prefabs/Effects/Soul).
    public sealed class SoulEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] fadedParts;
        [SerializeField] private float seconds = 1.4f;
        [SerializeField] private AnimationCurve rise = PieceMotion.Keys(0f, -0.089f, 0.45f, 0.422f, 0.75f, 0.711f, 1f, 0.978f);
        [SerializeField] private AnimationCurve sway = PieceMotion.Keys(0f, 0f, 0.45f, -0.078f, 0.75f, 0.067f, 1f, -0.022f);
        [SerializeField] private AnimationCurve size = PieceMotion.Keys(0f, 0.8f, 0.45f, 0.82f, 0.75f, 0.78f, 1f, 0.72f);
        [SerializeField] private AnimationCurve opacity = PieceMotion.Keys(0f, 0f, 0.15f, 1f, 0.75f, 0.8f, 1f, 0f);

        private Vector3 start;
        private float time;

        private void Start()
        {
            start = transform.localPosition;
            Apply(0f);
            Destroy(gameObject, seconds);
        }

        private void Update()
        {
            time += Time.deltaTime;
            Apply(Mathf.Clamp01(time / seconds));
        }

        private void Apply(float progress)
        {
            transform.localPosition = start + new Vector3(sway.Evaluate(progress), rise.Evaluate(progress), 0f);
            transform.localScale = Vector3.one * size.Evaluate(progress);
            var alpha = opacity.Evaluate(progress);
            foreach (var part in fadedParts) part.color = new Color(part.color.r, part.color.g, part.color.b, alpha);
        }
    }
}
