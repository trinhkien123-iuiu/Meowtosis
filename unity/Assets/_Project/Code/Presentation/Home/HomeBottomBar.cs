using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Thanh dưới của màn Home: dải sáng, icon và tên tab đều tính từ vị trí của dải trang, nên chuyển động liền với trang
    // và bám ngón tay khi đang vuốt. Tab càng gần vị trí thì icon càng to, càng nhô lên và tên càng rõ.
    [DisallowMultipleComponent]
    public sealed class HomeBottomBar : MonoBehaviour
    {
        [Serializable]
        public sealed class Tab
        {
            public Button button;
            public RectTransform icon;
            public TMP_Text label;
        }

        [Tooltip("Cùng thứ tự với HomeTab: Shop, Home, Setting.")]
        [SerializeField] private Tab[] tabs = Array.Empty<Tab>();
        [Tooltip("Dải sáng nằm sau tab đang chọn.")]
        [SerializeField] private RectTransform highlight;
        [SerializeField] private float selectedIconScale = 1.2f;
        [Tooltip("Icon tab đang chọn nhô lên bao nhiêu, chừa chỗ cho tên tab bên dưới.")]
        [SerializeField] private float selectedIconLift = 54f;

        private Vector2[] iconRestPositions = Array.Empty<Vector2>();

        public event Action<int> TabClicked;

        public void ShowPosition(float position)
        {
            var clamped = Mathf.Clamp(position, 0f, tabs.Length - 1f);
            for (var tab = 0; tab < tabs.Length; tab++)
            {
                var closeness = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(clamped - tab));
                tabs[tab].icon.anchoredPosition = iconRestPositions[tab] + Vector2.up * (selectedIconLift * closeness);
                tabs[tab].icon.localScale = Vector3.one * Mathf.Lerp(1f, selectedIconScale, closeness);
                tabs[tab].label.alpha = closeness;
            }

            // Các tab rộng bằng nhau: tâm của tab thứ n nằm ở (n + 0,5) / số tab bề ngang thanh.
            var anchorX = (clamped + 0.5f) / tabs.Length;
            highlight.anchorMin = new Vector2(anchorX, highlight.anchorMin.y);
            highlight.anchorMax = new Vector2(anchorX, highlight.anchorMax.y);
        }

        private void Awake()
        {
            iconRestPositions = new Vector2[tabs.Length];
            for (var tab = 0; tab < tabs.Length; tab++)
            {
                var index = tab;
                iconRestPositions[tab] = tabs[tab].icon.anchoredPosition;
                tabs[tab].button.onClick.AddListener(() => TabClicked?.Invoke(index));
            }
        }
    }
}
