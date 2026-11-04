using UnityEngine;

namespace Meowtosis.Presentation
{
    // Co khung này vào vùng an toàn của màn hình (tai thỏ, góc bo, thanh điều hướng). Đặt lên một RectTransform kéo kín canvas;
    // mọi phần tử HUD cần tránh tai thỏ là con của nó. Popup phủ kín màn hình thì để ngoài.
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect appliedArea;
        private Vector2Int appliedScreen;

        private void OnEnable() => Apply();

        private void Update() => Apply();

        private void Apply()
        {
            var area = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (area == appliedArea && screen == appliedScreen) return;
            appliedArea = area;
            appliedScreen = screen;
            if (screen.x <= 0 || screen.y <= 0) return;
            // Vùng an toàn lớn hơn màn hình là số liệu lệch (đổi cỡ Game view giữa chừng): dùng cả màn hình.
            if (area.xMin < 0f || area.yMin < 0f || area.xMax > screen.x || area.yMax > screen.y) area = new Rect(0f, 0f, screen.x, screen.y);

            var rectTransform = (RectTransform)transform;
            rectTransform.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            rectTransform.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
