using System;
using System.Collections;
using Meowtosis.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Thanh sao: phần vàng tụt dần theo số nước, ba ngôi sao trên thanh tắt dần khi vượt mốc (StarMeter).
    // Vị trí ngôi sao đặt sẵn trong scene ở 0, 1/3 và 2/3 thanh.
    // Mất sao (không tính lúc mở level): ngôi sao phồng lên rồi rung, vỡ thành mảnh vàng bắn ra và rơi xuống mờ dần,
    // đổi sang sao tắt; thanh rung nhẹ và nháy đỏ. Tiếng do presenter phát khi nhận sự kiện StarLost.
    public sealed class StarMeterView : MonoBehaviour
    {
        [Tooltip("Phần vàng của thanh: neo trái, độ dài theo anchorMax.x.")]
        [SerializeField] private RectTransform fill;
        [Tooltip("Ngôi sao thứ 1, 2, 3 theo thứ tự trái sang phải.")]
        [SerializeField] private Image[] stars;
        [SerializeField] private Sprite litStar;
        [SerializeField] private Sprite emptyStar;
        [SerializeField] private float drainSpeed = 1.2f;

        [Header("Vỡ sao")]
        [Tooltip("Các mảnh vỡ dùng chung (con của thanh sao, tắt sẵn), bắn ra từ ngôi sao vừa mất.")]
        [SerializeField] private Image[] shards;
        [SerializeField] private Color shardColor = new Color(1f, 0.82f, 0.25f, 1f);
        [SerializeField] private float swellSeconds = 0.14f;
        [SerializeField] private float swellScale = 1.5f;
        [SerializeField] private float shakeSeconds = 0.2f;
        [SerializeField] private float shardSeconds = 0.75f;
        [SerializeField] private float shardSpeed = 520f;
        [SerializeField] private float shardGravity = 1500f;
        [Tooltip("Thanh rung ngang bao nhiêu pixel canvas khi mất sao.")]
        [SerializeField] private float barShake = 7f;
        [SerializeField] private Color barFlash = new Color(1f, 0.35f, 0.3f, 1f);

        private float shownFill = 1f;
        private float targetFill = 1f;
        private int shownStars = -1;
        private Image fillImage;
        private Color fillColor;
        private RectTransform barRoot;
        private Vector2 barHome;
        private Coroutine barRoutine;

        // Báo số sao vừa mất (để phát tiếng).
        public event Action<int> StarLost;

        public void Show(StarMeter meter, bool instantly)
        {
            targetFill = meter.Fill;
            if (instantly) shownFill = targetFill;
            var lost = !instantly && shownStars >= 0 && meter.Stars < shownStars;
            for (var index = 0; index < stars.Length; index++)
            {
                // Ngôi sao đang vỡ giữ nguyên hình sáng tới lúc vỡ, hoạt cảnh tự đổi sang sao tắt.
                var breaking = lost && index >= meter.Stars && index < shownStars;
                if (!breaking) stars[index].sprite = index < meter.Stars ? litStar : emptyStar;
            }
            if (lost)
            {
                for (var index = meter.Stars; index < shownStars; index++) StartCoroutine(Shatter(index, (index - meter.Stars) * 0.12f));
                PlayBar();
                StarLost?.Invoke(shownStars - meter.Stars);
            }
            shownStars = meter.Stars;
            ApplyFill();
        }

        private void Awake()
        {
            fillImage = fill.GetComponent<Image>();
            if (fillImage != null) fillColor = fillImage.color;
            barRoot = (RectTransform)transform;
            barHome = barRoot.anchoredPosition;
            foreach (var shard in shards) shard.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Mathf.Approximately(shownFill, targetFill)) return;
            shownFill = Mathf.MoveTowards(shownFill, targetFill, drainSpeed * Time.deltaTime);
            ApplyFill();
        }

        private void ApplyFill() => fill.anchorMax = new Vector2(Mathf.Max(0.001f, shownFill), fill.anchorMax.y);

        private void PlayBar()
        {
            if (barRoutine != null) StopCoroutine(barRoutine);
            barRoutine = StartCoroutine(ShakeBar());
        }

        // Thanh rung ngang tắt dần và nháy đỏ rồi về màu cũ.
        private IEnumerator ShakeBar()
        {
            for (var time = 0f; time < shakeSeconds; time += Time.deltaTime)
            {
                var t = time / shakeSeconds;
                var decay = 1f - t;
                barRoot.anchoredPosition = barHome + new Vector2(Mathf.Sin(t * Mathf.PI * 7f) * barShake * decay, 0f);
                if (fillImage != null) fillImage.color = Color.Lerp(barFlash, fillColor, t);
                yield return null;
            }
            barRoot.anchoredPosition = barHome;
            if (fillImage != null) fillImage.color = fillColor;
            barRoutine = null;
        }

        // Ngôi sao phồng lên, rung, rồi vỡ: đổi sang sao tắt và bắn các mảnh vàng ra.
        private IEnumerator Shatter(int index, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            var star = stars[index].rectTransform;
            var home = star.localScale;
            for (var time = 0f; time < swellSeconds; time += Time.deltaTime)
            {
                star.localScale = home * Mathf.Lerp(1f, swellScale, time / swellSeconds);
                yield return null;
            }
            stars[index].sprite = emptyStar;
            StartCoroutine(FlyShards(star.position, index));
            for (var time = 0f; time < shakeSeconds; time += Time.deltaTime)
            {
                var t = time / shakeSeconds;
                star.localScale = home * Mathf.Lerp(swellScale, 1f, t * t);
                star.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 5f) * 16f * (1f - t));
                yield return null;
            }
            star.localScale = home;
            star.localRotation = Quaternion.identity;
        }

        // Mảnh vỡ bay ra theo các hướng đều quanh ngôi sao, rơi xuống vì trọng lực, nhỏ dần và mờ đi.
        private IEnumerator FlyShards(Vector3 origin, int starIndex)
        {
            var count = shards.Length;
            var velocities = new Vector2[count];
            var spins = new float[count];
            var parent = (RectTransform)shards[0].transform.parent;
            var scale = parent.lossyScale.x;
            for (var index = 0; index < count; index++)
            {
                var angle = (index / (float)count + UnityEngine.Random.Range(-0.04f, 0.04f)) * Mathf.PI * 2f;
                velocities[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.5f) * shardSpeed * UnityEngine.Random.Range(0.6f, 1f);
                spins[index] = UnityEngine.Random.Range(-540f, 540f);
                var shard = shards[index];
                shard.color = shardColor;
                shard.rectTransform.position = origin;
                shard.rectTransform.localScale = Vector3.one * UnityEngine.Random.Range(0.7f, 1.2f);
                shard.gameObject.SetActive(true);
            }
            for (var time = 0f; time < shardSeconds; time += Time.deltaTime)
            {
                var t = time / shardSeconds;
                for (var index = 0; index < count; index++)
                {
                    var shard = shards[index];
                    velocities[index].y -= shardGravity * Time.deltaTime;
                    shard.rectTransform.position += (Vector3)(velocities[index] * Time.deltaTime * scale);
                    shard.rectTransform.Rotate(0f, 0f, spins[index] * Time.deltaTime);
                    var color = shardColor;
                    color.a = 1f - t * t;
                    shard.color = color;
                }
                yield return null;
            }
            foreach (var shard in shards) shard.gameObject.SetActive(false);
        }
    }
}
