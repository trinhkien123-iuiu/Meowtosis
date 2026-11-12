using System;
using Spine.Unity;
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
            public SkeletonDataAsset animation;
            public string animationPrefix;
        }

        [Tooltip("Cùng thứ tự với HomeTab: Shop, Home, Setting.")]
        [SerializeField] private Tab[] tabs = Array.Empty<Tab>();
        [Tooltip("Dải sáng nằm sau tab đang chọn.")]
        [SerializeField] private RectTransform highlight;
        [SerializeField] private RectTransform highlightRim;
        [SerializeField] private RectTransform[] sectionBackgrounds = Array.Empty<RectTransform>();
        [SerializeField] private float selectedIconScale = 1.2f;
        [Tooltip("Icon tab đang chọn nhô lên bao nhiêu, chừa chỗ cho tên tab bên dưới.")]
        [SerializeField] private float selectedIconLift = 54f;
        [SerializeField] private Material iconMaterial;

        private Vector2[] iconRestPositions = Array.Empty<Vector2>();
        private SkeletonGraphic[] animatedIcons = Array.Empty<SkeletonGraphic>();
        private int lastSelected = -1;

        public event Action<int> TabClicked;

        public void ShowPosition(float position)
        {
            var clamped = Mathf.Clamp(position, 0f, tabs.Length - 1f);
            var selected = Mathf.RoundToInt(clamped);
            for (var tab = 0; tab < tabs.Length; tab++)
            {
                var closeness = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(clamped - tab));
                tabs[tab].icon.anchoredPosition = iconRestPositions[tab] + Vector2.up * (selectedIconLift * closeness);
                tabs[tab].icon.localScale = Vector3.one * Mathf.Lerp(1f, selectedIconScale, closeness);
                tabs[tab].label.alpha = closeness;
                if (selected != lastSelected && animatedIcons[tab] != null &&
                    (lastSelected < 0 || tab == selected || tab == lastSelected))
                {
                    var state = animatedIcons[tab].AnimationState;
                    var animation = tabs[tab].animationPrefix + (tab == selected ? "_on" : "_off");
                    if (lastSelected < 0) state.SetAnimation(0, animation + "_idle", true);
                    else
                    {
                        state.SetAnimation(0, animation, false);
                        state.AddAnimation(0, animation + "_idle", true, 0f);
                    }
                }
            }
            lastSelected = selected;

            // Các tab rộng bằng nhau: tâm của tab thứ n nằm ở (n + 0,5) / số tab bề ngang thanh.
            var anchorX = (clamped + 0.5f) / tabs.Length;
            highlight.anchorMin = new Vector2(anchorX, highlight.anchorMin.y);
            highlight.anchorMax = new Vector2(anchorX, highlight.anchorMax.y);
            if (highlightRim != null)
            {
                highlightRim.anchorMin = new Vector2(anchorX, highlightRim.anchorMin.y);
                highlightRim.anchorMax = new Vector2(anchorX, highlightRim.anchorMax.y);
                var slotWidth = ((RectTransform)transform).rect.width / tabs.Length;
                if (slotWidth > 0f)
                {
                    highlight.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, slotWidth * 0.88f);
                    highlightRim.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, slotWidth);
                    AlignSectionEdges(clamped, slotWidth);
                }
            }
        }

        private void AlignSectionEdges(float position, float slotWidth)
        {
            if (sectionBackgrounds.Length != tabs.Length) return;
            var margin = (slotWidth - highlight.rect.width) / (2f * slotWidth * tabs.Length);
            for (var i = 0; i < sectionBackgrounds.Length; i++)
            {
                var section = sectionBackgrounds[i];
                if (section == null) continue;
                section.anchorMin = new Vector2(SectionEdge(i, position, margin), section.anchorMin.y);
                section.anchorMax = new Vector2(SectionEdge(i + 1, position, margin), section.anchorMax.y);
            }
        }

        private float SectionEdge(int boundary, float position, float margin)
        {
            if (boundary <= 0) return 0f;
            if (boundary >= tabs.Length) return 1f;
            var left = Mathf.Clamp01(1f - Mathf.Abs(position - (boundary - 1)));
            var right = Mathf.Clamp01(1f - Mathf.Abs(position - boundary));
            return boundary / (float)tabs.Length + margin * (right - left);
        }

        private void Awake()
        {
            iconRestPositions = new Vector2[tabs.Length];
            animatedIcons = new SkeletonGraphic[tabs.Length];
            for (var tab = 0; tab < tabs.Length; tab++)
            {
                var index = tab;
                iconRestPositions[tab] = tabs[tab].icon.anchoredPosition;
                tabs[tab].button.onClick.AddListener(() => TabClicked?.Invoke(index));
                if (tabs[tab].animation != null && iconMaterial != null)
                    animatedIcons[tab] = CreateAnimatedIcon(tabs[tab]);
            }
        }

        private SkeletonGraphic CreateAnimatedIcon(Tab tab)
        {
            var data = tab.animation.GetSkeletonData(true);
            if (data == null) return null;
            var graphic = SkeletonGraphic.NewSkeletonGraphicGameObject(tab.animation, tab.icon, iconMaterial);
            graphic.gameObject.name = "CatdomNavigationIcon";
            graphic.raycastTarget = false;
            graphic.UnscaledTime = true;
            var nativePixels = tab.animation.scale * (graphic.canvas != null ? graphic.canvas.referencePixelsPerUnit : 100f);
            var fit = Mathf.Min(tab.icon.rect.width / Mathf.Max(1f, data.Width * nativePixels),
                tab.icon.rect.height / Mathf.Max(1f, data.Height * nativePixels));
            var rect = graphic.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one * fit;
            rect.anchoredPosition = -new Vector2(data.X + data.Width * 0.5f, data.Y + data.Height * 0.5f) * (nativePixels * fit);
            graphic.AnimationState.SetAnimation(0, tab.animationPrefix + "_off_idle", true);
            var previousIcon = tab.icon.GetComponent<Image>();
            if (previousIcon != null) previousIcon.enabled = false;
            return graphic;
        }
    }
}
