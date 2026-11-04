using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Một hạt sao bay ra rồi mờ dần trên canvas, dùng chung cho panel thắng và bản đồ level.
    internal static class UiSparkle
    {
        public static IEnumerator Fly(Image prefab, RectTransform layer, Vector2 center, Vector2 velocity, float seconds)
        {
            var sparkle = Object.Instantiate(prefab, layer);
            var rect = sparkle.rectTransform;
            var color = sparkle.color;
            var spin = Random.Range(-360f, 360f);
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / seconds;
                var eased = 1f - (1f - progress) * (1f - progress);
                rect.anchoredPosition = center + velocity * (eased * seconds);
                rect.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.2f, progress);
                rect.localRotation = Quaternion.Euler(0f, 0f, spin * time);
                color.a = 1f - progress * progress;
                sparkle.color = color;
                yield return null;
            }
            Object.Destroy(sparkle.gameObject);
        }
    }
}
