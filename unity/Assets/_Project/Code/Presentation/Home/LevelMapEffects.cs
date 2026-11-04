using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Hiệu ứng khi chọn một nút level: vòng sáng loang ra và sao toé. Nằm ở lớp riêng trên các nút để không bị nút khác che.
    public sealed class LevelMapEffects : MonoBehaviour
    {
        [SerializeField] private RectTransform layer;
        [SerializeField] private Image ringPrefab;
        [SerializeField] private Image sparklePrefab;

        [Header("Vòng sáng")]
        [SerializeField] private float ringSeconds = 0.4f;
        [SerializeField] private float ringStartScale = 0.8f;
        [SerializeField] private float ringEndScale = 2.4f;

        [Header("Sao toé")]
        [SerializeField, Min(0)] private int sparkleCount = 12;
        [SerializeField] private float sparkleSpeed = 380f;
        [SerializeField] private float sparkleSeconds = 0.45f;

        public void Burst(RectTransform around)
        {
            var center = (Vector2)layer.InverseTransformPoint(around.TransformPoint(around.rect.center));
            StartCoroutine(GrowRing(center));
            for (var index = 0; index < sparkleCount; index++)
            {
                var angle = (index + Random.value * 0.6f) / sparkleCount * Mathf.PI * 2f;
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (sparkleSpeed * Random.Range(0.6f, 1.1f));
                StartCoroutine(UiSparkle.Fly(sparklePrefab, layer, center, velocity, sparkleSeconds));
            }
        }

        private IEnumerator GrowRing(Vector2 center)
        {
            var ring = Instantiate(ringPrefab, layer);
            var rect = ring.rectTransform;
            var color = ring.color;
            rect.anchoredPosition = center;
            for (var time = 0f; time < ringSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / ringSeconds;
                var eased = 1f - (1f - progress) * (1f - progress);
                rect.localScale = Vector3.one * Mathf.Lerp(ringStartScale, ringEndScale, eased);
                color.a = 1f - progress;
                ring.color = color;
                yield return null;
            }
            Destroy(ring.gameObject);
        }
    }
}
