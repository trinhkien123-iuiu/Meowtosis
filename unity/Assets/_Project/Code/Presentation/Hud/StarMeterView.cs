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
        [Tooltip("Thời gian ngôi sao bật lên khi lấy lại được.")]
        [SerializeField] private float regainSeconds = 0.4f;
        [Tooltip("Hình quầng sáng và tia toả ra khi lấy lại sao (Art/UI/StarHalo).")]
        [SerializeField] private Sprite haloSprite;
        [Tooltip("Cỡ quầng sáng lúc to nhất, tính theo bề rộng ngôi sao.")]
        [SerializeField] private float haloScale = 3.2f;
        [SerializeField] private float haloSeconds = 0.7f;
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
        private Image[] halos;

        // Báo số sao vừa mất (để phát tiếng).
        public event Action<int> StarLost;

        // Báo số sao vừa lấy lại được (chơi lại màn: thanh sao đầy lại).
        public event Action<int> StarGained;

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
            var gained = shownStars >= 0 && meter.Stars > shownStars;   // cả khi chơi lại màn: sao đầy lại từng ngôi một
            if (gained)
            {
                for (var index = shownStars; index < meter.Stars; index++) StartCoroutine(Regain(index, (index - shownStars) * 0.1f));
                StarGained?.Invoke(meter.Stars - shownStars);
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
            halos = new Image[stars.Length];
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
            if (MotionSettings.Reduced) return;
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

        // Lấy lại sao: ngôi sao nhỏ xíu bật lên rồi lắng về đúng cỡ, cùng lúc quầng sáng vàng cùng các tia toả ra từ nó, xoay chậm,
        // to dần và tan. Không có mảnh vỡ (đó là cảnh mất sao).
        private IEnumerator Regain(int index, float delay)
        {
            var star = stars[index].rectTransform;
            var home = star.localScale;
            star.localScale = Vector3.zero;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            var halo = HaloOf(index);
            StartCoroutine(RadiateHalo(halo, star));
            for (var time = 0f; time < regainSeconds; time += Time.deltaTime)
            {
                var t = time / regainSeconds;
                var bounce = t < 0.55f ? Mathf.Lerp(0f, 1.35f, t / 0.55f) : Mathf.Lerp(1.35f, 1f, (t - 0.55f) / 0.45f);
                star.localScale = home * bounce;
                yield return null;
            }
            star.localScale = home;
        }

        private IEnumerator RadiateHalo(Image halo, RectTransform star)
        {
            halo.rectTransform.position = star.position;
            halo.gameObject.SetActive(true);
            var size = star.rect.width * haloScale;
            for (var time = 0f; time < haloSeconds; time += Time.deltaTime)
            {
                var t = time / haloSeconds;
                var grow = 1f - (1f - t) * (1f - t);   // nở nhanh rồi chậm dần
                halo.rectTransform.sizeDelta = Vector2.one * size * Mathf.Lerp(0.35f, 1f, grow);
                halo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -35f * t);
                var alpha = t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f;
                halo.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            halo.gameObject.SetActive(false);
        }

        // Quầng sáng của ngôi sao `index`, dựng lần đầu cần tới, nằm ngay sau ngôi sao trong thanh.
        private Image HaloOf(int index)
        {
            if (halos[index] != null) return halos[index];
            var go = new GameObject("StarHalo", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(stars[index].transform.parent, false);
            go.transform.SetSiblingIndex(stars[index].transform.GetSiblingIndex());
            var image = go.GetComponent<Image>();
            image.sprite = haloSprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            halos[index] = image;
            go.SetActive(false);
            return image;
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
        private IEnumerator FlyShards(Vector3 origin, int starIndex, float strength = 1f)
        {
            var count = shards.Length;
            var velocities = new Vector2[count];
            var spins = new float[count];
            var parent = (RectTransform)shards[0].transform.parent;
            var scale = parent.lossyScale.x;
            for (var index = 0; index < count; index++)
            {
                var angle = (index / (float)count + UnityEngine.Random.Range(-0.04f, 0.04f)) * Mathf.PI * 2f;
                velocities[index] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) + 0.5f) * shardSpeed * strength * UnityEngine.Random.Range(0.6f, 1f);
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
