using System;
using Meowtosis.Domain;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Meowtosis.Presentation
{
    // Đổi thao tác chạm hoặc kéo chuột thành cú vuốt (điểm bắt đầu và hướng) hoặc cú chạm.
    // Trong lúc kéo chỉ báo `Dragged` để quân nghiêng theo; nhấc tay mới tính: kéo đủ xa là vuốt, chưa đủ là chạm.
    // Bấm lên nút giao diện thì không tính.
    public sealed class SwipeInput : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [Tooltip("Kéo xa bao nhiêu (tính bằng ô) thì tính là vuốt.")]
        [SerializeField, Min(0.05f)] private float minimumDistance = 0.3f;

        private Vector2 pressPoint;
        private bool isPressed;

        public event Action<Vector2, Direction> Swiped;

        public event Action<Vector2> Tapped;

        // Mỗi khung hình khi đang giữ: điểm bấm xuống và vector kéo (tính bằng ô).
        public event Action<Vector2, Vector2> Dragged;

        // Nhả tay hoặc huỷ: quân hết nghiêng.
        public event Action DragEnded;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && !IsPointerOverInterface())
            {
                pressPoint = PointerWorldPosition();
                isPressed = true;
            }
            if (!isPressed) return;

            var drag = PointerWorldPosition() - pressPoint;
            if (Input.GetMouseButton(0))
            {
                Dragged?.Invoke(pressPoint, drag);
                return;
            }

            isPressed = false;
            DragEnded?.Invoke();
            if (drag.magnitude >= minimumDistance) Swiped?.Invoke(pressPoint, DirectionOf(drag));
            else Tapped?.Invoke(pressPoint);
        }

        private void OnDisable() => CancelPress();

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) CancelPress();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) CancelPress();
        }

        private void CancelPress()
        {
            if (!isPressed) return;
            isPressed = false;
            DragEnded?.Invoke();
        }

        private Vector2 PointerWorldPosition() => targetCamera.ScreenToWorldPoint(Input.mousePosition);

        private static Direction DirectionOf(Vector2 drag)
        {
            if (Mathf.Abs(drag.x) > Mathf.Abs(drag.y)) return drag.x > 0f ? Direction.Right : Direction.Left;
            return drag.y > 0f ? Direction.Up : Direction.Down;
        }

        private static bool IsPointerOverInterface()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            return Input.touchCount > 0
                ? eventSystem.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                : eventSystem.IsPointerOverGameObject();
        }
    }
}
