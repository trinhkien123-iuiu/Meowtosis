using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Một đầu cổng dịch chuyển trong bàn minh hoạ của thẻ cơ chế (Prefabs/Hud/DemoPortalGate): vòng xoáy quay, quầng sáng thở.
    // Dựng sẵn theo hướng ra ngoài là +X, giống PortalGateView của bàn thật. Chạy bằng giờ thật vì thẻ mở lúc game tạm dừng.
    public sealed class DemoPortalGate : MonoBehaviour
    {
        [SerializeField] private RectTransform swirl;
        [SerializeField] private Image swirlImage;
        [SerializeField] private Image glow;
        [SerializeField] private float spinDegreesPerSecond = -140f;

        public void SetColor(Color color)
        {
            swirlImage.color = Color.Lerp(color, Color.white, 0.25f);
            var glowColor = color;
            glowColor.a = 0.6f;
            glow.color = glowColor;
        }

        private void Update()
        {
            swirl.Rotate(0f, 0f, spinDegreesPerSecond * Time.unscaledDeltaTime);
            var breathe = 1f + 0.1f * Mathf.Sin(Time.unscaledTime * 3f);
            glow.rectTransform.localScale = Vector3.one * breathe;
        }
    }
}
