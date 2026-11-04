using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Hiệu ứng của popup thắng, vẽ bằng Image trên canvas vì hạt của Particle System nằm dưới lớp UI phủ màn hình:
    // sao toé sáng, pháo giấy rơi, xu bay nối đuôi về ô tổng xu.
    // Hình dáng chỉnh trong các prefab Prefabs/Hud/Win*.
    public sealed class WinEffects : MonoBehaviour
    {
        [SerializeField] private RectTransform layer;
        [SerializeField] private Image sparklePrefab;
        [SerializeField] private Image confettiPrefab;
        [SerializeField] private Image coinPrefab;

        [Header("Toé sáng")]
        [SerializeField, Min(0)] private int sparklesPerBurst = 12;
        [SerializeField] private float sparkleSpeed = 330f;
        [SerializeField] private float sparkleSeconds = 0.55f;

        [Header("Pháo giấy")]
        [SerializeField, Min(0)] private int confettiCount = 40;
        [SerializeField] private float confettiSeconds = 2.2f;
        [SerializeField] private Color[] confettiColors =
        {
            new Color(1f, 0.82f, 0.25f), new Color(1f, 0.45f, 0.62f), new Color(0.45f, 0.82f, 1f),
            new Color(0.62f, 0.93f, 0.42f), new Color(0.78f, 0.58f, 1f),
        };

        [Header("Xu bay")]
        [SerializeField] private float coinSeconds = 0.6f;
        [Tooltip("Xu xuất phát lệch khỏi tâm tối đa bao nhiêu.")]
        [SerializeField] private float coinStartSpread = 24f;
        [Tooltip("Đường bay cong sang ngang tối đa bao nhiêu.")]
        [SerializeField] private float coinSway = 80f;
        [Tooltip("Đường bay cong lên thêm trong khoảng này.")]
        [SerializeField] private Vector2 coinLift = new Vector2(40f, 120f);
        [SerializeField] private float coinStartScale = 1.1f;
        [SerializeField] private float coinEndScale = 0.6f;
        [SerializeField] private float coinWobbleDegrees = 25f;

        public void Clear()
        {
            StopAllCoroutines();
            foreach (Transform child in layer) Destroy(child.gameObject);
        }

        public void Sparkle(RectTransform around)
        {
            var center = LocalPoint(around);
            for (var index = 0; index < sparklesPerBurst; index++)
            {
                var angle = (index + UnityEngine.Random.value * 0.6f) / sparklesPerBurst * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                StartCoroutine(UiSparkle.Fly(sparklePrefab, layer, center, direction * (sparkleSpeed * UnityEngine.Random.Range(0.6f, 1.1f)), sparkleSeconds));
            }
        }

        public void Confetti()
        {
            var area = layer.rect;
            for (var index = 0; index < confettiCount; index++)
            {
                var color = confettiColors[index % confettiColors.Length];
                StartCoroutine(FlyConfetti(UnityEngine.Random.Range(area.xMin, area.xMax), area.yMax + 40f, area.height, color, UnityEngine.Random.value * 0.7f));
            }
        }

        // Xu bay nối đuôi từ `source` về `target`; mỗi đồng về tới nơi thì báo `arrived` với số đồng đã về.
        public void CoinFlight(RectTransform source, RectTransform target, int count, float stagger, Action<int> arrived)
        {
            var from = LocalPoint(source);
            var to = LocalPoint(target);
            var landed = 0;
            for (var index = 0; index < count; index++)
            {
                StartCoroutine(FlyCoin(from, to, index, index * stagger, () => arrived(++landed)));
            }
        }

        private Vector2 LocalPoint(RectTransform target)
        {
            return layer.InverseTransformPoint(target.TransformPoint(target.rect.center));
        }

        private IEnumerator FlyConfetti(float x, float top, float fall, Color color, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            var piece = Instantiate(confettiPrefab, layer);
            var rect = piece.rectTransform;
            piece.color = color;
            var spin = UnityEngine.Random.Range(-420f, 420f);
            var sway = UnityEngine.Random.Range(30f, 80f);
            var phase = UnityEngine.Random.value * Mathf.PI * 2f;
            var seconds = confettiSeconds * UnityEngine.Random.Range(0.8f, 1.2f);
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / seconds;
                rect.anchoredPosition = new Vector2(x + Mathf.Sin(phase + progress * 7f) * sway, top - fall * progress * (0.75f + 0.25f * progress));
                rect.localRotation = Quaternion.Euler(0f, 0f, spin * time);
                rect.localScale = new Vector3(Mathf.Cos(phase + time * 6f), 1f, 1f);   // lật như tờ giấy
                color.a = Mathf.Clamp01((1f - progress) * 4f);
                piece.color = color;
                yield return null;
            }
            Destroy(piece.gameObject);
        }

        private IEnumerator FlyCoin(Vector2 from, Vector2 to, int index, float delay, Action arrived)
        {
            yield return new WaitForSecondsRealtime(delay);
            var coin = Instantiate(coinPrefab, layer);
            var rect = coin.rectTransform;
            var start = from + UnityEngine.Random.insideUnitCircle * coinStartSpread;
            var bend = (start + to) * 0.5f + new Vector2(UnityEngine.Random.Range(-coinSway, coinSway), UnityEngine.Random.Range(coinLift.x, coinLift.y));
            for (var time = 0f; time < coinSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / coinSeconds;
                var eased = progress * progress * (3f - 2f * progress);
                var fromBend = Vector2.Lerp(start, bend, eased);
                var toTarget = Vector2.Lerp(bend, to, eased);
                rect.anchoredPosition = Vector2.Lerp(fromBend, toTarget, eased);
                rect.localScale = Vector3.one * Mathf.Lerp(coinStartScale, coinEndScale, eased);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(progress * 6f + index) * coinWobbleDegrees);
                yield return null;
            }
            Destroy(coin.gameObject);
            arrived();
        }
    }
}
