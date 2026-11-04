using UnityEngine;

namespace Meowtosis.Presentation
{
    // Mèo bị đẩy khỏi hộp đúng màu: mắt @@ và ba ngôi sao bay vòng elip trên đầu (Prefabs/Effects/Dizzy, gắn vào Visual của quân).
    public sealed class DizzyEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] fadedParts;
        [SerializeField] private SpriteRenderer[] stars;
        [SerializeField] private float seconds = 1.65f;
        [SerializeField] private float fadeSeconds = 0.35f;
        [SerializeField] private float orbitSeconds = 1f;
        [SerializeField] private float orbitWidth = 0.333f;
        [SerializeField] private float orbitHeight = 0.1f;
        [SerializeField] private float headHeight = 0.489f;
        [SerializeField] private float starSize = 0.0412f;

        private float time;

        private void Start() => Destroy(gameObject, seconds);

        private void Update()
        {
            time += Time.deltaTime;
            for (var index = 0; index < stars.Length; index++)
            {
                var angle = (time / orbitSeconds + (float)index / stars.Length) * Mathf.PI * 2f;
                var depth = -Mathf.Sin(angle);   // vòng ra trước thì to, ra sau thì nhỏ
                stars[index].transform.localPosition = new Vector3(Mathf.Cos(angle) * orbitWidth, headHeight + depth * orbitHeight, 0f);
                stars[index].transform.localScale = Vector3.one * (starSize * (0.96f + depth * 0.24f));
            }

            var alpha = Mathf.Clamp01((seconds - time) / fadeSeconds);
            foreach (var part in fadedParts) part.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
