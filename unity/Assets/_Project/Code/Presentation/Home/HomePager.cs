using System;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Các trang của màn Home xếp thành một dải ngang, cả dải chạy theo một vị trí duy nhất (0 là trang đầu, 1 là trang kế…).
    // Vuốt thì vị trí bám ngón tay, thả hoặc bấm tab thì trượt mượt tới trang đích; đang trượt vẫn nắm lại được.
    // Thanh dưới nghe PositionChanged nên luôn khớp với trang, kể cả lúc đang kéo dở.
    [DisallowMultipleComponent]
    public sealed class HomePager : MonoBehaviour
    {
        [Serializable]
        public sealed class Page
        {
            [Tooltip("Gốc trang, luôn bật khi chạy để vuốt không bị khựng.")]
            public GameObject root;
            [Tooltip("Phần phủ kín màn hình được trượt.")]
            public RectTransform content;
            [Tooltip("Chặn bấm nút trong trang khi đang vuốt.")]
            public CanvasGroup group;
        }

        [Tooltip("Cùng thứ tự với tab ở thanh dưới.")]
        [SerializeField] private Page[] pages = Array.Empty<Page>();
        [Tooltip("Chặn bấm tab khi đang vuốt.")]
        [SerializeField] private CanvasGroup barGroup;
        [Tooltip("Ngón tay đi bao nhiêu điểm ảnh màn hình mới tính là vuốt ngang (ít hơn thì vẫn là bấm).")]
        [SerializeField] private float dragThreshold = 25f;
        [Tooltip("Thả quá phần này của bề ngang thì sang trang.")]
        [SerializeField, Range(0.1f, 0.9f)] private float switchFraction = 0.25f;
        [Tooltip("Vuốt nhanh hơn (bề ngang mỗi giây) thì sang trang dù chưa quá ngưỡng.")]
        [SerializeField] private float flickSpeed = 1.2f;
        [Tooltip("Số giây trượt nốt sau khi thả hoặc bấm tab.")]
        [SerializeField] private float settleSeconds = 0.28f;
        [Tooltip("Vuốt về phía không còn trang thì chỉ kéo được phần này rồi bật lại.")]
        [SerializeField, Range(0f, 0.5f)] private float edgeResistance = 0.15f;

        private float position;
        private int restingPage;
        private int dragStartPage;
        private float dragStartPosition;
        private bool isPressed;
        private bool isDragging;
        private bool isVerticalGesture;
        private bool isSettling;
        private Vector2 pressPoint;
        private float lastPointerX;
        private float velocity;
        private float settleFrom;
        private float settleTo;
        private float settleProgress;
        private int restoreInteractionFrame = -1;

        // Vị trí của dải trang, có phần lẻ khi đang trượt.
        public event Action<float> PositionChanged;

        // Có màn khác phủ lên thì không nhận vuốt.
        public bool IsLocked { get; set; }

        public void Show(int index, bool animate)
        {
            if (index < 0 || index >= pages.Length) return;
            restingPage = index;
            if (!animate)
            {
                isSettling = false;
                MoveTo(index);
                return;
            }
            StartSettling(index);
        }

        private void Awake()
        {
            foreach (var page in pages) page.root.SetActive(true);
        }

        private void StartSettling(int index)
        {
            restingPage = index;
            settleFrom = position;
            settleTo = index;
            settleProgress = 0f;
            isSettling = true;
        }

        private void MoveTo(float newPosition)
        {
            position = newPosition;
            // Đặt theo neo chứ không theo điểm ảnh, nên đổi cỡ màn hình các trang vẫn nằm đúng chỗ.
            for (var page = 0; page < pages.Length; page++)
            {
                var content = pages[page].content;
                content.anchorMin = new Vector2(page - position, 0f);
                content.anchorMax = new Vector2(page - position + 1f, 1f);
            }
            PositionChanged?.Invoke(position);
        }

        private void Update()
        {
            if (restoreInteractionFrame >= 0 && Time.frameCount > restoreInteractionFrame)
            {
                SetInteractable(true);
                restoreInteractionFrame = -1;
            }

            if (IsLocked)
            {
                isPressed = false;
                isDragging = false;
                return;
            }

            ReadPointer(out var isDown, out var isHeld, out var isUp, out var point);
            if (isDown)
            {
                isPressed = true;
                isDragging = false;
                isVerticalGesture = false;
                pressPoint = point;
                velocity = 0f;
            }

            if (isPressed && isHeld && !isDragging && !isVerticalGesture)
            {
                var moved = point - pressPoint;
                if (Mathf.Abs(moved.y) > dragThreshold && Mathf.Abs(moved.y) > Mathf.Abs(moved.x)) isVerticalGesture = true;
                else if (Mathf.Abs(moved.x) > dragThreshold) BeginDrag(point);
            }
            if (isDragging && isHeld) Drag(point);
            if (isUp)
            {
                isPressed = false;
                if (isDragging) EndDrag();
            }
            if (isSettling) Settle();
        }

        private void Settle()
        {
            settleProgress += Time.unscaledDeltaTime / Mathf.Max(0.01f, settleSeconds);
            var progress = Mathf.Clamp01(settleProgress);
            var remaining = 1f - progress;
            MoveTo(Mathf.Lerp(settleFrom, settleTo, 1f - remaining * remaining * remaining));   // chậm dần về cuối
            if (progress >= 1f) isSettling = false;
        }

        // Tính từ điểm bắt đầu kéo chứ không từ điểm bấm, để trang không nhảy một đoạn bằng ngưỡng vuốt.
        // Đang trượt mà nắm lại thì tiếp tục từ vị trí hiện tại.
        private void BeginDrag(Vector2 point)
        {
            isDragging = true;
            isSettling = false;
            pressPoint = point;
            lastPointerX = point.x;
            dragStartPosition = position;
            dragStartPage = Mathf.RoundToInt(position);
            SetInteractable(false);
        }

        private void Drag(Vector2 point)
        {
            var screenWidth = Mathf.Max(1f, Screen.width);
            var deltaTime = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            velocity = Mathf.Lerp(velocity, (point.x - lastPointerX) / screenWidth / deltaTime, 0.5f);
            lastPointerX = point.x;
            var wanted = dragStartPosition - (point.x - pressPoint.x) / screenWidth;
            var last = pages.Length - 1;
            if (wanted < 0f) wanted *= edgeResistance;
            else if (wanted > last) wanted = last + (wanted - last) * edgeResistance;
            MoveTo(wanted);
        }

        private void EndDrag()
        {
            isDragging = false;
            restoreInteractionFrame = Time.frameCount;
            var moved = position - dragStartPage;
            var page = dragStartPage;
            if (Mathf.Abs(moved) > switchFraction) page += moved > 0f ? 1 : -1;
            else if (Mathf.Abs(velocity) > flickSpeed) page += velocity < 0f ? 1 : -1;
            StartSettling(Mathf.Clamp(page, 0, pages.Length - 1));
        }

        private void SetInteractable(bool isInteractable)
        {
            foreach (var page in pages) page.group.interactable = isInteractable;
            barGroup.interactable = isInteractable;
        }

        private static void ReadPointer(out bool isDown, out bool isHeld, out bool isUp, out Vector2 point)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                point = touch.position;
                isDown = touch.phase == TouchPhase.Began;
                isUp = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                isHeld = !isUp;
                return;
            }
            point = Input.mousePosition;
            isDown = Input.GetMouseButtonDown(0);
            isHeld = Input.GetMouseButton(0);
            isUp = Input.GetMouseButtonUp(0);
        }
    }
}
