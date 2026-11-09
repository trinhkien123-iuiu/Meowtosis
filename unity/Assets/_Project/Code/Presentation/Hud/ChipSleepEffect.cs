using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Chữ Z nhỏ bay lên từ hình mèo trên chip đếm, giống chữ Z của con mèo ngủ trên bàn (SleepyEffect). Chỉ chạy khi
    // mọi con màu đó đang ngủ. Chữ là các Image dựng lúc chạy, nằm trong chip, không chặn bấm.
    public sealed class ChipSleepEffect : MonoBehaviour
    {
        [SerializeField] private Sprite letter;
        [Tooltip("Hình mèo của chip: chữ Z mọc ra từ góc trên bên phải của nó.")]
        [SerializeField] private RectTransform anchor;
        [SerializeField] private float letterSeconds = 1.6f;
        [SerializeField] private float letterEvery = 0.7f;
        [SerializeField] private float startSize = 14f;
        [SerializeField] private float endSize = 26f;
        [SerializeField] private Vector2 travel = new Vector2(14f, 38f);
        [SerializeField] private float swayWidth = 5f;

        private readonly List<Image> letters = new List<Image>();
        private readonly List<float> ages = new List<float>();
        private bool running;
        private float untilNext;

        public void SetAsleep(bool asleep)
        {
            if (running == asleep) return;
            running = asleep;
            untilNext = 0f;
            if (asleep) return;
            for (var index = 0; index < letters.Count; index++) Hide(index);
        }

        private void OnDisable()
        {
            running = false;
            for (var index = 0; index < letters.Count; index++) Hide(index);
        }

        private void Update()
        {
            if (letter == null) return;
            if (running && (untilNext -= Time.unscaledDeltaTime) <= 0f)
            {
                untilNext = letterEvery;
                Spawn();
            }
            for (var index = 0; index < letters.Count; index++)
            {
                if (ages[index] < 0f) continue;
                ages[index] += Time.unscaledDeltaTime;
                var t = ages[index] / letterSeconds;
                if (t >= 1f)
                {
                    Hide(index);
                    continue;
                }
                var rect = letters[index].rectTransform;
                var start = StartPoint();
                var sway = Mathf.Sin(t * Mathf.PI * 2.2f) * swayWidth;
                rect.anchoredPosition = start + new Vector2(travel.x * t + sway, travel.y * t);
                rect.sizeDelta = Vector2.one * Mathf.Lerp(startSize, endSize, t);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 3f) * 14f);
                var alpha = t < 0.15f ? t / 0.15f : 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
                letters[index].color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private Vector2 StartPoint()
        {
            if (anchor == null) return Vector2.zero;
            var rect = anchor.rect;
            return anchor.anchoredPosition + new Vector2(rect.width * 0.25f, rect.height * 0.3f);
        }

        private void Spawn()
        {
            var index = ages.FindIndex(age => age < 0f);
            if (index < 0)
            {
                var go = new GameObject("Z", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var image = go.GetComponent<Image>();
                image.sprite = letter;
                image.raycastTarget = false;
                image.preserveAspect = true;
                letters.Add(image);
                ages.Add(-1f);
                index = letters.Count - 1;
            }
            letters[index].gameObject.SetActive(true);
            letters[index].transform.SetAsLastSibling();
            ages[index] = 0f;
        }

        private void Hide(int index)
        {
            ages[index] = -1f;
            letters[index].gameObject.SetActive(false);
        }
    }
}
