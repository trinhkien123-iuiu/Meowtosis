using System.Collections;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Một đầu cổng dịch chuyển (Prefabs/Board/PortalGate), đặt ở cạnh ô nhìn ra ngoài bàn; dựng sẵn theo hướng ra ngoài là +X.
    // Vòng xoáy quay liên tục, quầng sáng thở nhẹ, tàn sáng bị hút vào tâm. Có quân đi qua thì Flash: phồng lên và sáng rực.
    public sealed class PortalGateView : MonoBehaviour
    {
        [Tooltip("Vòng xoáy quay; nằm trong một nhóm bị ép dẹt để thành hình bầu dục đứng.")]
        [SerializeField] private Transform swirl;
        [SerializeField] private SpriteRenderer swirlImage;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private SpriteRenderer rim;
        [SerializeField] private ParticleSystem wisps;
        [SerializeField] private float spinDegreesPerSecond = -140f;
        [SerializeField] private float glowBreathSeconds = 2f;
        [SerializeField] private float glowBreathAmount = 0.1f;
        [SerializeField] private float flashSeconds = 0.45f;
        [SerializeField] private float flashScale = 1.4f;
        [SerializeField] private float closeSeconds = 0.35f;

        private Vector3 baseScale;
        private Vector3 glowScale;
        private Color tint = Color.white;
        private float flashTime = 1f;
        private float phase;
        private bool isClosing;

        private void Awake()
        {
            baseScale = transform.localScale;
            glowScale = glow.transform.localScale;
            phase = Random.value * 6f;
        }

        public Color Tint => tint;

        public void SetColor(Color color)
        {
            tint = color;
            swirlImage.color = Color.Lerp(color, Color.white, 0.25f);
            rim.color = color;
            var glowColor = color;
            glowColor.a = 0.6f;
            glow.color = glowColor;
            var main = wisps.main;
            main.startColor = Color.Lerp(color, Color.white, 0.45f);
        }

        // Có quân vừa đi vào hoặc ra khỏi cổng này.
        public void Flash() => flashTime = 0f;

        // Cổng khép lại và biến mất (lúc bàn sụp khi thắng).
        public void Close()
        {
            if (!isClosing) StartCoroutine(Closing());
        }

        private void Update()
        {
            if (isClosing) return;
            swirl.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
            glow.transform.localScale = glowScale * (1f + glowBreathAmount * Mathf.Sin((Time.time + phase) / glowBreathSeconds * Mathf.PI * 2f));

            var scale = 1f;
            var boost = 0f;
            if (flashTime < 1f)
            {
                flashTime = Mathf.Min(1f, flashTime + Time.deltaTime / flashSeconds);
                var wave = Mathf.Sin(flashTime * Mathf.PI);
                scale = 1f + (flashScale - 1f) * wave;
                boost = wave;
            }
            transform.localScale = baseScale * scale;
            swirlImage.color = Color.Lerp(Color.Lerp(tint, Color.white, 0.25f), Color.white, boost);
        }

        private IEnumerator Closing()
        {
            isClosing = true;
            wisps.Stop();
            var start = transform.localScale;
            for (var time = 0f; time < closeSeconds; time += Time.deltaTime)
            {
                transform.localScale = Vector3.Lerp(start, Vector3.zero, time / closeSeconds);
                swirl.Rotate(0f, 0f, spinDegreesPerSecond * 3f * Time.deltaTime);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
