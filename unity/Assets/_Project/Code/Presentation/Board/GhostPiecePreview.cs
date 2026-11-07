using UnityEngine;

namespace Meowtosis.Presentation
{
    // Hiển thị bóng mờ (ghost preview) lướt từ ô bắt đầu tới ô đích để mô phỏng thao tác vuốt thật
    // một cách trực quan, có co dãn đàn hồi (squash & stretch) và lặp lại tuần hoàn.
    public sealed class GhostPiecePreview : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float baseScale = 1f;
        private Vector3 startPos;
        private Vector3 targetPos;
        private Vector3 moveDirection;
        private float timer;

        private const float MoveDuration = 0.55f;
        private const float SettleDuration = 0.45f;
        private const float FadeDuration = 0.25f;
        private const float PauseDuration = 0.35f;
        private const float TotalCycle = MoveDuration + SettleDuration + FadeDuration + PauseDuration; // 1.6s

        public void Init(Vector3 start, Vector3 target, Sprite sprite, float width, float lift, int sortingOrder)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = sortingOrder;

            startPos = start + new Vector3(0f, lift, 0f);
            targetPos = target + new Vector3(0f, lift, 0f);
            moveDirection = (targetPos - startPos).normalized;

            if (sprite != null && sprite.bounds.size.x > 0.001f)
            {
                baseScale = width / sprite.bounds.size.x;
            }
            transform.localPosition = startPos;
            transform.localScale = Vector3.one * baseScale;
            timer = 0f;
        }

        private void Update()
        {
            if (spriteRenderer == null) return;

            timer = Mathf.Repeat(timer + Time.deltaTime, TotalCycle);

            if (timer < MoveDuration)
            {
                // Giai đoạn 1: Lướt và dãn người theo hướng vuốt từ điểm xuất phát sang đích
                var progress = timer / MoveDuration;
                var ease = Mathf.SmoothStep(0f, 1f, progress);
                transform.localPosition = Vector3.Lerp(startPos, targetPos, ease);

                // Độ trong suốt tăng dần từ 0 lên 0.45f khi lướt
                var alpha = Mathf.Lerp(0.05f, 0.45f, Mathf.Clamp01(progress * 2.5f));
                spriteRenderer.color = new Color(1f, 1f, 1f, alpha);

                // Co dãn đàn hồi (squash & stretch) lúc đang lướt
                var stretch = Mathf.Sin(progress * Mathf.PI);
                var scaleAlong = 1f + stretch * 0.22f;
                var scaleAcross = 1f - stretch * 0.12f;

                var isHorizontal = Mathf.Abs(moveDirection.x) >= Mathf.Abs(moveDirection.y);
                var scaleX = baseScale * (isHorizontal ? scaleAlong : scaleAcross);
                var scaleY = baseScale * (isHorizontal ? scaleAcross : scaleAlong);
                transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
            else if (timer < MoveDuration + SettleDuration)
            {
                // Giai đoạn 2: Tiếp đất tại ô đích, nảy nhẹ và thở
                var progress = (timer - MoveDuration) / SettleDuration;
                transform.localPosition = targetPos;

                // Nảy đàn hồi khi chạm đích
                var bounce = Mathf.Sin(progress * Mathf.PI * 2f) * Mathf.Exp(-progress * 3.5f) * 0.12f;
                var scaleX = baseScale * (1f + bounce);
                var scaleY = baseScale * (1f - bounce);
                transform.localScale = new Vector3(scaleX, scaleY, 1f);

                spriteRenderer.color = new Color(1f, 1f, 1f, 0.42f);
            }
            else if (timer < MoveDuration + SettleDuration + FadeDuration)
            {
                // Giai đoạn 3: Mờ dần biến mất trước khi lặp lại
                var progress = (timer - MoveDuration - SettleDuration) / FadeDuration;
                transform.localPosition = targetPos;
                transform.localScale = Vector3.one * baseScale;
                var alpha = Mathf.Lerp(0.42f, 0f, progress);
                spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
            }
            else
            {
                // Giai đoạn 4: Nghỉ một nhịp ngắn ở trạng thái ẩn rồi lặp lại từ đầu
                transform.localPosition = startPos;
                spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }
}
