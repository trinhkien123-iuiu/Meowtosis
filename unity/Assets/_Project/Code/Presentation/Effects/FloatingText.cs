using TMPro;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Chữ bay lên rồi mờ dần ("Khử 3!") (Prefabs/Effects/FloatingText).
    // Luôn nằm gọn trong khung nhìn của camera: chữ dài đứng gần mép thì được dịch vào trong thay vì tràn ra ngoài.
    public sealed class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float seconds = 1.1f;
        [SerializeField] private float riseSpeed = 0.78f;
        [Tooltip("Khoảng chừa lại giữa chữ và mép khung nhìn, tính bằng đơn vị thế giới.")]
        [SerializeField] private float edgeMargin = 0.15f;

        private float time;
        private float topLimit = float.PositiveInfinity;

        public void Show(string text, Camera viewCamera)
        {
            label.text = text;
            transform.localPosition += Vector3.up * 0.27f;
            if (viewCamera != null) KeepInside(viewCamera);
            Destroy(gameObject, seconds);
        }

        // Dịch cả chữ sang ngang cho vừa khung nhìn; nếu chữ rộng hơn khung thì thu nhỏ cho vừa.
        private void KeepInside(Camera viewCamera)
        {
            label.ForceMeshUpdate();
            var bounds = label.textBounds;
            var lower = label.transform.TransformPoint(bounds.min);
            var upper = label.transform.TransformPoint(bounds.max);
            var halfHeight = viewCamera.orthographicSize;
            var halfWidth = halfHeight * viewCamera.aspect;
            var center = viewCamera.transform.position;
            var minX = center.x - halfWidth + edgeMargin;
            var maxX = center.x + halfWidth - edgeMargin;

            var width = upper.x - lower.x;
            var room = maxX - minX;
            if (width > room)
            {
                transform.localScale *= room / width;
                label.ForceMeshUpdate();
                lower = label.transform.TransformPoint(label.textBounds.min);
                upper = label.transform.TransformPoint(label.textBounds.max);
            }
            var shift = 0f;
            if (lower.x < minX) shift = minX - lower.x;
            else if (upper.x > maxX) shift = maxX - upper.x;
            transform.position += Vector3.right * shift;
            topLimit = center.y + halfHeight - edgeMargin - (upper.y - transform.position.y);
        }

        private void Update()
        {
            time += Time.deltaTime;
            var position = transform.localPosition + Vector3.up * (riseSpeed * Time.deltaTime);
            // Bay lên tới sát mép trên thì dừng lại mờ dần, không bay ra khỏi màn hình.
            if (position.y > topLimit) position.y = Mathf.Max(transform.localPosition.y, topLimit);
            transform.localPosition = position;
            label.alpha = Mathf.Clamp01(1f - time / seconds);
        }
    }
}
