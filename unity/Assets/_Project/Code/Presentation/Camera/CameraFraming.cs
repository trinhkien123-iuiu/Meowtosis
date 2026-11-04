using UnityEngine;

namespace Meowtosis.Presentation
{
    // Đặt camera để bàn chơi vừa màn hình, chừa phần trên và dưới cho HUD; ảnh nền luôn phủ kín màn hình.
    // Tự căn lại khi tỉ lệ màn hình đổi (xoay máy, đổi cỡ cửa sổ Game).
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFraming : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.4f)] private float topReserved = 0.22f;      // phần chiều cao màn hình dành cho HUD trên
        [SerializeField, Range(0f, 0.4f)] private float bottomReserved = 0.14f;   // và HUD dưới
        [SerializeField, Min(0f)] private float margin = 0.5f;                    // khoảng trống quanh bàn, tính bằng ô
        [SerializeField] private SpriteRenderer background;
        [Tooltip("Lớp hoạ tiết lặp trên nền (SpriteRenderer chế độ Tiled). Độ mờ chỉnh bằng Color của nó.")]
        [SerializeField] private SpriteRenderer pattern;
        [Tooltip("Màn hình cao bằng bao nhiêu tile hoạ tiết; số càng lớn hoạ tiết càng nhỏ.")]
        [SerializeField, Min(0.5f)] private float patternTilesHigh = 3f;

        private Camera targetCamera;
        private Bounds framedBounds;
        private bool hasBoard;
        private float framedAspect;

        public void Frame(Bounds board)
        {
            framedBounds = board;
            hasBoard = true;
            Apply();
        }

        private void Awake() => targetCamera = GetComponent<Camera>();

        private void LateUpdate()
        {
            if (hasBoard && !Mathf.Approximately(framedAspect, targetCamera.aspect)) Apply();
        }

        private void Apply()
        {
            framedAspect = targetCamera.aspect;
            var usableHeight = 1f - topReserved - bottomReserved;
            var halfHeightForRows = (framedBounds.size.y + 2f * margin) / 2f / usableHeight;
            var halfHeightForColumns = (framedBounds.size.x + 2f * margin) / 2f / targetCamera.aspect;
            var halfHeight = Mathf.Max(halfHeightForRows, halfHeightForColumns);
            targetCamera.orthographicSize = halfHeight;

            // Dời camera lên để bàn nằm giữa phần màn hình còn lại sau khi chừa chỗ cho HUD.
            var center = framedBounds.center + Vector3.up * ((topReserved - bottomReserved) * halfHeight);
            transform.position = new Vector3(center.x, center.y, transform.position.z);
            FitBackground(halfHeight);
            FitPattern(halfHeight);
        }

        // Tile tính theo chiều cao màn hình, nên level to hay nhỏ thì hoạ tiết vẫn cùng một cỡ trên màn hình.
        private void FitPattern(float halfHeight)
        {
            if (pattern == null || pattern.sprite == null) return;
            var viewHeight = halfHeight * 2f;
            var viewWidth = viewHeight * targetCamera.aspect;
            var scale = viewHeight / patternTilesHigh / (pattern.sprite.rect.height / pattern.sprite.pixelsPerUnit);
            pattern.transform.localScale = new Vector3(scale, scale, 1f);
            pattern.size = new Vector2(viewWidth, viewHeight) / scale;
        }

        private void FitBackground(float halfHeight)
        {
            if (background == null || background.sprite == null) return;
            var viewHeight = halfHeight * 2f;
            var viewWidth = viewHeight * targetCamera.aspect;
            var size = background.sprite.bounds.size;
            var scale = Mathf.Max(viewWidth / size.x, viewHeight / size.y);
            background.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
