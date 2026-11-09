using UnityEngine;

namespace Meowtosis.Presentation
{
    // Vòng ma thuật nằm dưới chân mèo (prefab Prefabs/Board/Highlight): đánh dấu ô chọn được khi dùng booster hoặc bộ 3 sắp nổ.
    // Vòng ngoài và lõi xoay ngược chiều nhau, quầng sáng thở bằng độ trong (không phóng to thu nhỏ để không lấn ô bên cạnh).
    // Lúc hiện ra thì nở từ nhỏ lên một lần. Flare(): bản dùng một lần, loé to rồi tắt (khi booster tác động lên ô).
    public sealed class MagicCircle : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer outer;
        [SerializeField] private SpriteRenderer inner;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private Color color = new Color(1f, 0.86f, 0.35f, 1f);
        [SerializeField] private float outerDegreesPerSecond = 40f;
        [SerializeField] private float innerDegreesPerSecond = -70f;
        [SerializeField] private float glowPulseSeconds = 1.2f;
        [SerializeField, Range(0f, 1f)] private float glowMinAlpha = 0.35f;
        [SerializeField] private float appearSeconds = 0.22f;
        [Header("Loé (Flare)")]
        [SerializeField] private float flareSeconds = 0.55f;
        [SerializeField] private float flareGrowTo = 1.7f;

        private float age;
        private float phase;
        private bool flaring;
        private Vector3 baseScale;

        public void SetColor(Color tint)
        {
            color = tint;
            ApplyAlpha(1f, 1f);
        }

        // Loé một lần rồi tự huỷ.
        public void Flare(Color tint)
        {
            flaring = true;
            age = 0f;
            SetColor(tint);
        }

        private void Awake()
        {
            baseScale = transform.localScale;
            phase = Random.value * glowPulseSeconds;
            transform.localScale = baseScale * 0.3f;
            ApplyAlpha(1f, 1f);
        }

        private void Update()
        {
            age += Time.deltaTime;
            outer.transform.Rotate(0f, 0f, outerDegreesPerSecond * Time.deltaTime);
            inner.transform.Rotate(0f, 0f, innerDegreesPerSecond * Time.deltaTime);
            if (flaring)
            {
                var t = Mathf.Clamp01(age / flareSeconds);
                transform.localScale = baseScale * Mathf.Lerp(0.6f, flareGrowTo, 1f - (1f - t) * (1f - t));
                ApplyAlpha(1f - t, 1f - t);
                if (t >= 1f) Destroy(gameObject);
                return;
            }
            // Nở ra một lần lúc hiện, sau đó giữ nguyên cỡ.
            var grow = Mathf.Clamp01(age / appearSeconds);
            transform.localScale = baseScale * Mathf.Lerp(0.3f, 1f, 1f - (1f - grow) * (1f - grow) * (1f - grow));
            var wave = 0.5f + 0.5f * Mathf.Sin((age + phase) / glowPulseSeconds * Mathf.PI * 2f);
            ApplyAlpha(Mathf.Lerp(glowMinAlpha, 1f, wave), Mathf.Lerp(0.75f, 1f, wave));
        }

        private void ApplyAlpha(float glowAlpha, float ringAlpha)
        {
            outer.color = new Color(color.r, color.g, color.b, color.a * ringAlpha);
            inner.color = new Color(color.r, color.g, color.b, color.a * ringAlpha * 0.85f);
            glow.color = new Color(color.r, color.g, color.b, color.a * glowAlpha * 0.85f);
        }
    }
}
