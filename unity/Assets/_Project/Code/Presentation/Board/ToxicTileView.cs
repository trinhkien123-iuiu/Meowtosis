using UnityEngine;

namespace Meowtosis.Presentation
{
    // Ô độc "sống" ở bên trong: vũng độc thở nhẹ, xương cá nổi bập bềnh và lắc lư, bong bóng nổi lên từ vũng, phình ra rồi vỡ.
    // Khung khối bên ngoài đứng yên. Mỗi ô lệch pha ngẫu nhiên nên các ô không nhúc nhích cùng nhịp.
    // Chỉ đổi vị trí, góc và cỡ, không đổi màu, để hoạt cảnh thắng làm mờ khối mà không bị tranh chấp.
    public sealed class ToxicTileView : MonoBehaviour
    {
        [Header("Các lớp")]
        [Tooltip("Vũng độc bên trong khối (không phải khung ngoài).")]
        [SerializeField] private Transform pool;
        [SerializeField] private Transform bones;
        [Tooltip("Mỗi phần tử là một bong bóng (hình tròn trắng viền sáng).")]
        [SerializeField] private Transform[] bubbles;

        [Header("Thở")]
        [SerializeField] private float breathSpeed = 2.2f;
        [SerializeField] private float breathAmount = 0.03f;

        [Header("Xương cá")]
        [SerializeField] private float bobHeight = 0.03f;
        [SerializeField] private float bobSpeed = 1.5f;
        [SerializeField] private float swayDegrees = 6f;
        [SerializeField] private float swaySpeed = 1.1f;

        [Header("Bong bóng")]
        [Tooltip("Vùng bong bóng mọc ra, so với tâm ô, tính bằng ô.")]
        [SerializeField] private Vector2 bubbleArea = new Vector2(0.56f, 0.5f);
        [SerializeField] private Vector2 bubbleCenter = new Vector2(0f, -0.04f);
        [SerializeField] private Vector2 bubbleSecondsRange = new Vector2(1.4f, 2.6f);
        [SerializeField] private Vector2 bubbleSizeRange = new Vector2(0.1f, 0.2f);
        [SerializeField] private float bubbleRise = 0.14f;
        [Tooltip("Phần cuối đời bong bóng dành cho cú phình và vỡ.")]
        [SerializeField, Range(0.05f, 0.5f)] private float popPortion = 0.18f;

        private Vector3 poolScale;
        private Vector3 bonesPosition;
        private float phase;
        private float[] bubbleAge;
        private float[] bubbleLife;
        private float[] bubbleSize;
        private Vector2[] bubbleOrigin;

        private void Awake()
        {
            phase = Random.value * 100f;
            if (pool != null) poolScale = pool.localScale;
            if (bones != null) bonesPosition = bones.localPosition;
            var count = bubbles == null ? 0 : bubbles.Length;
            bubbleAge = new float[count];
            bubbleLife = new float[count];
            bubbleSize = new float[count];
            bubbleOrigin = new Vector2[count];
            for (var index = 0; index < count; index++)
            {
                Respawn(index);
                bubbleAge[index] = Random.value * bubbleLife[index];   // lệch pha để không vỡ cùng lúc
            }
        }

        private void Update()
        {
            var time = Time.time + phase;
            if (pool != null)
            {
                var wave = Mathf.Sin(time * breathSpeed);
                pool.localScale = new Vector3(poolScale.x * (1f + breathAmount * 0.6f * wave),
                    poolScale.y * (1f + breathAmount * Mathf.Sin(time * breathSpeed + 1.3f)), poolScale.z);
            }
            if (bones != null)
            {
                bones.localPosition = bonesPosition + Vector3.up * (bobHeight * Mathf.Sin(time * bobSpeed));
                bones.localRotation = Quaternion.Euler(0f, 0f, swayDegrees * Mathf.Sin(time * swaySpeed));
            }
            for (var index = 0; index < bubbleAge.Length; index++) UpdateBubble(index, Time.deltaTime);
        }

        private void UpdateBubble(int index, float deltaTime)
        {
            bubbleAge[index] += deltaTime;
            if (bubbleAge[index] >= bubbleLife[index]) Respawn(index);

            var progress = bubbleAge[index] / bubbleLife[index];
            var popStart = 1f - popPortion;
            float scale;
            if (progress < popStart)
            {
                var grow = progress / popStart;
                scale = bubbleSize[index] * grow * grow * (3f - 2f * grow);
            }
            else
            {
                // Phình nhanh một nhịp rồi vỡ (thu về 0).
                var pop = (progress - popStart) / popPortion;
                scale = bubbleSize[index] * (1f + 0.45f * Mathf.Sin(pop * Mathf.PI * 0.5f)) * (1f - pop * pop);
            }
            var bubble = bubbles[index];
            bubble.localPosition = (Vector3)(bubbleOrigin[index] + Vector2.up * (bubbleRise * progress));
            bubble.localScale = Vector3.one * Mathf.Max(scale, 0f);
        }

        private void Respawn(int index)
        {
            bubbleAge[index] = 0f;
            bubbleLife[index] = Random.Range(bubbleSecondsRange.x, bubbleSecondsRange.y);
            bubbleSize[index] = Random.Range(bubbleSizeRange.x, bubbleSizeRange.y);
            bubbleOrigin[index] = bubbleCenter + new Vector2(Random.Range(-0.5f, 0.5f) * bubbleArea.x, Random.Range(-0.5f, 0.5f) * bubbleArea.y);
        }
    }
}
