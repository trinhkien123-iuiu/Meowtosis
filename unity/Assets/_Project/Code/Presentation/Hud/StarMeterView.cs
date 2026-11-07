using Meowtosis.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Thanh sao: phần vàng tụt dần theo số nước, ba ngôi sao trên thanh tắt dần khi vượt mốc (StarMeter).
    // Vị trí ngôi sao đặt sẵn trong scene ở 0, 1/3 và 2/3 thanh.
    public sealed class StarMeterView : MonoBehaviour
    {
        [Tooltip("Phần vàng của thanh: neo trái, độ dài theo anchorMax.x.")]
        [SerializeField] private RectTransform fill;
        [Tooltip("Ngôi sao thứ 1, 2, 3 theo thứ tự trái sang phải.")]
        [SerializeField] private Image[] stars;
        [SerializeField] private Sprite litStar;
        [SerializeField] private Sprite emptyStar;
        [SerializeField] private float drainSpeed = 1.2f;

        private float shownFill = 1f;
        private float targetFill = 1f;

        public void Show(StarMeter meter, bool instantly)
        {
            targetFill = meter.Fill;
            if (instantly) shownFill = targetFill;
            for (var index = 0; index < stars.Length; index++) stars[index].sprite = index < meter.Stars ? litStar : emptyStar;
            ApplyFill();
        }

        private void Update()
        {
            if (Mathf.Approximately(shownFill, targetFill)) return;
            shownFill = Mathf.MoveTowards(shownFill, targetFill, drainSpeed * Time.deltaTime);
            ApplyFill();
        }

        private void ApplyFill() => fill.anchorMax = new Vector2(Mathf.Max(0.001f, shownFill), fill.anchorMax.y);
    }
}
