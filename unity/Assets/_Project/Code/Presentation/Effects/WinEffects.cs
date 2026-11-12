using System;
using System.Collections;
using UnityEngine;
using Spine.Unity;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Hiệu ứng của popup thắng, vẽ bằng Image trên canvas vì hạt của Particle System nằm dưới lớp UI phủ màn hình:
    // sao toé sáng, pháo giấy rơi, pháo hoa, xu bay nối đuôi về ô tổng xu.
    // Hình dáng chỉnh trong các prefab Prefabs/Hud/Win*.
    public sealed class WinEffects : MonoBehaviour
    {
        [SerializeField] private RectTransform layer;
        [SerializeField] private Image sparklePrefab;
        [SerializeField] private Image confettiPrefab;
        [SerializeField] private Image coinPrefab;
        [SerializeField] private SkeletonDataAsset spinningCoin;
        [SerializeField] private Material spinningCoinMaterial;

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

        [Header("Pháo hoa")]
        [Tooltip("Hình tia lửa (sao 4 cánh), vệt đuôi và đầu pháo (chấm mờ), chớp sáng và quầng sáng lúc nổ.")]
        [SerializeField] private Sprite sparkSprite;
        [SerializeField] private Sprite trailSprite;
        [SerializeField] private Sprite flareSprite;
        [SerializeField] private Sprite glowSprite;
        [SerializeField, Min(0)] private int fireworkCount = 7;
        [Tooltip("Quả sau bắn sau quả trước khoảng này (giây).")]
        [SerializeField] private Vector2 fireworkGap = new Vector2(0.28f, 0.55f);
        [SerializeField] private float rocketSeconds = 0.55f;
        [SerializeField] private float rocketSize = 34f;
        [SerializeField, Min(1)] private int sparksPerFirework = 34;
        [SerializeField] private Vector2 sparkSpeed = new Vector2(380f, 560f);
        [Tooltip("Sức cản không khí: phần tốc độ còn lại sau mỗi giây.")]
        [SerializeField, Range(0.01f, 1f)] private float sparkDrag = 0.12f;
        [SerializeField] private float sparkGravity = 260f;
        [SerializeField] private Vector2 sparkSeconds = new Vector2(1.1f, 1.6f);
        [SerializeField] private float sparkSize = 46f;
        [Tooltip("Vệt đuôi dài bằng quãng tia lửa bay trong khoảng này (giây).")]
        [SerializeField] private float trailSeconds = 0.09f;
        [SerializeField] private float trailWidth = 16f;
        [SerializeField] private float flashSize = 520f;
        [SerializeField] private float flashSeconds = 0.45f;
        [SerializeField] private Color[] fireworkColors =
        {
            new Color(1f, 0.78f, 0.3f), new Color(1f, 0.42f, 0.6f), new Color(0.42f, 0.85f, 1f),
            new Color(0.6f, 1f, 0.5f), new Color(0.85f, 0.6f, 1f),
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

        public void PrewarmAssets()
        {
            if (spinningCoin != null) spinningCoin.GetSkeletonData(true);
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

        public Image CreateStarGlint(RectTransform star)
        {
            var glint = Instantiate(sparklePrefab, star);
            glint.gameObject.name = "StarGlint";
            glint.sprite = sparkSprite;
            glint.raycastTarget = false;
            glint.color = new Color(1f, 0.97f, 0.8f, 0f);
            var rect = glint.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(star.rect.width * 0.22f, star.rect.height * 0.25f);
            rect.sizeDelta = Vector2.one * (star.rect.width * 0.22f);
            rect.localScale = Vector3.zero;
            return glint;
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

        // Pháo hoa: đầu pháo bay lên kéo vệt sáng, nổ thành quầng sáng và vòng tia lửa. Tia lửa có vệt đuôi theo hướng bay,
        // chậm dần vì sức cản, rơi theo trọng lực, lõi trắng ngả dần sang màu quả pháo, cuối đời nhấp nháy rồi tắt.
        public void Fireworks() => StartCoroutine(LaunchFireworks());

        private IEnumerator LaunchFireworks()
        {
            var area = layer.rect;
            for (var index = 0; index < fireworkCount; index++)
            {
                var color = fireworkColors[index % fireworkColors.Length];
                var from = new Vector2(UnityEngine.Random.Range(area.xMin * 0.7f, area.xMax * 0.7f), area.yMin);
                var to = new Vector2(from.x + UnityEngine.Random.Range(-60f, 60f),
                    UnityEngine.Random.Range(area.center.y + area.height * 0.05f, area.yMax - area.height * 0.12f));
                StartCoroutine(Firework(from, to, color));
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(fireworkGap.x, fireworkGap.y));
            }
        }

        private Image CreateFx(Sprite sprite, Vector2 size)
        {
            var image = Instantiate(sparklePrefab, layer);
            image.sprite = sprite;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            image.rectTransform.localScale = Vector3.one;
            image.rectTransform.localRotation = Quaternion.identity;
            return image;
        }

        private IEnumerator Firework(Vector2 from, Vector2 to, Color color)
        {
            var head = CreateFx(trailSprite, Vector2.one * rocketSize);
            var tail = CreateFx(trailSprite, new Vector2(rocketSize * 0.45f, rocketSize));
            tail.rectTransform.pivot = new Vector2(0.5f, 1f);
            var warm = Color.Lerp(color, Color.white, 0.6f);
            var previous = from;
            for (var time = 0f; time < rocketSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / rocketSeconds;
                var position = Vector2.Lerp(from, to, 1f - (1f - progress) * (1f - progress));
                head.rectTransform.anchoredPosition = position;
                head.color = new Color(warm.r, warm.g, warm.b, 0.6f + 0.4f * Mathf.Sin(time * 50f));
                var speed = (position - previous).magnitude / Mathf.Max(Time.unscaledDeltaTime, 0.001f);
                tail.rectTransform.anchoredPosition = position;
                tail.rectTransform.sizeDelta = new Vector2(rocketSize * 0.45f, Mathf.Clamp(speed * 0.12f, rocketSize * 0.5f, rocketSize * 4f));
                tail.color = new Color(warm.r, warm.g, warm.b, 0.55f);
                previous = position;
                yield return null;
            }
            Destroy(head.gameObject);
            Destroy(tail.gameObject);

            StartCoroutine(Flash(glowSprite, to, flashSize, flashSeconds, color));
            StartCoroutine(Flash(flareSprite, to, flashSize * 0.55f, flashSeconds * 0.6f, Color.white));
            var secondary = fireworkColors[UnityEngine.Random.Range(0, fireworkColors.Length)];
            for (var index = 0; index < sparksPerFirework; index++)
            {
                var angle = (index + UnityEngine.Random.value * 0.6f) / sparksPerFirework * Mathf.PI * 2f;
                var speed = UnityEngine.Random.Range(sparkSpeed.x, sparkSpeed.y);
                var tint = index % 4 == 0 ? secondary : color;
                StartCoroutine(Spark(to, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed, tint,
                    UnityEngine.Random.Range(sparkSeconds.x, sparkSeconds.y)));
            }
        }

        private IEnumerator Flash(Sprite sprite, Vector2 at, float size, float seconds, Color color)
        {
            var flash = CreateFx(sprite, Vector2.one * size);
            flash.rectTransform.anchoredPosition = at;
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / seconds;
                flash.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, 1f - (1f - progress) * (1f - progress));
                flash.color = new Color(color.r, color.g, color.b, (1f - progress) * (1f - progress) * 0.8f);
                yield return null;
            }
            Destroy(flash.gameObject);
        }

        private IEnumerator Spark(Vector2 position, Vector2 velocity, Color color, float seconds)
        {
            var spark = CreateFx(sparkSprite, Vector2.one * sparkSize);
            var trail = CreateFx(trailSprite, new Vector2(trailWidth, trailWidth));
            trail.rectTransform.pivot = new Vector2(0.5f, 0f);
            var spin = UnityEngine.Random.Range(-240f, 240f);
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                var delta = Time.unscaledDeltaTime;
                var progress = time / seconds;
                velocity *= Mathf.Pow(sparkDrag, delta);
                velocity += Vector2.down * (sparkGravity * delta);
                position += velocity * delta;

                var shade = Color.Lerp(Color.white, color, Mathf.Clamp01(progress * 2.2f));
                var alpha = 1f - progress * progress;
                if (progress > 0.6f && UnityEngine.Random.value < 0.35f) alpha *= 0.25f;   // nhấp nháy trước khi tắt
                spark.color = new Color(shade.r, shade.g, shade.b, alpha);
                spark.rectTransform.anchoredPosition = position;
                spark.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, progress);
                spark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, spin * time);

                // Vệt đuôi: chấm mờ kéo dài ngược hướng bay, dài theo tốc độ
                trail.rectTransform.anchoredPosition = position;
                trail.rectTransform.sizeDelta = new Vector2(trailWidth * Mathf.Lerp(1f, 0.4f, progress), Mathf.Max(trailWidth, velocity.magnitude * trailSeconds));
                trail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-velocity.y, -velocity.x) * Mathf.Rad2Deg - 90f);
                trail.color = new Color(shade.r, shade.g, shade.b, alpha * 0.55f);
                yield return null;
            }
            Destroy(spark.gameObject);
            Destroy(trail.gameObject);
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
            // All coins emerge immediately; stagger only their departure to the counter.
            RectTransform rect;
            var animated = spinningCoin != null && spinningCoinMaterial != null;
            if (animated)
            {
                var coin = SkeletonGraphic.NewSkeletonGraphicGameObject(spinningCoin, layer, spinningCoinMaterial);
                coin.raycastTarget = false;
                coin.UnscaledTime = true;
                rect = coin.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                var entry = coin.AnimationState.SetAnimation(0, "rotation", true);
                entry.TrackTime = index * 0.047f;
            }
            else rect = Instantiate(coinPrefab, layer).rectTransform;
            // Original animation is 225 pixels across; match the existing coin prefab.
            var baseScale = animated ? coinPrefab.rectTransform.rect.width / 225f : 1f;
            var scatter = from + UnityEngine.Random.insideUnitCircle * coinStartSpread;
            var tilt = UnityEngine.Random.Range(-coinWobbleDegrees, coinWobbleDegrees);
            const float burstSeconds = 0.18f;
            for (var time = 0f; time < burstSeconds + delay; time += Time.unscaledDeltaTime)
            {
                var progress = Mathf.Clamp01(time / burstSeconds);
                rect.anchoredPosition = Vector2.Lerp(from, scatter, 1f - Mathf.Pow(1f - progress, 3f));
                rect.localScale = Vector3.one * (baseScale * coinStartScale * Mathf.Lerp(0.4f, 1f, progress));
                rect.localRotation = Quaternion.Euler(0f, 0f, tilt);
                yield return null;
            }
            var bend = (scatter + to) * 0.5f + new Vector2(UnityEngine.Random.Range(-coinSway, coinSway), UnityEngine.Random.Range(coinLift.x, coinLift.y));
            var seconds = Mathf.Max(0.01f, coinSeconds);
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / seconds;
                var eased = progress * progress;
                var fromBend = Vector2.Lerp(scatter, bend, eased);
                var toTarget = Vector2.Lerp(bend, to, eased);
                rect.anchoredPosition = Vector2.Lerp(fromBend, toTarget, eased);
                var scale = baseScale * Mathf.Lerp(coinStartScale, coinEndScale, eased);
                rect.localScale = animated ? Vector3.one * scale : new Vector3(scale * Mathf.Cos((time + index * 0.047f) * 18f), scale, scale);
                rect.localRotation = Quaternion.Euler(0f, 0f, tilt * (1f - progress));
                yield return null;
            }
            rect.anchoredPosition = to;
            Destroy(rect.gameObject);
            arrived();
        }
    }
}
