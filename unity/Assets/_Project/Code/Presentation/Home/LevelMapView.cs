using System;
using System.Collections;
using System.Collections.Generic;
using Meowtosis.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    public interface ILevelMapView
    {
        event Action<int> LevelClicked;

        event Action BackClicked;

        event Action ResetClicked;

        event Action UnlockAllClicked;

        void Show(IReadOnlyList<LevelMapEntry> entries);

        void Hide();

        // Báo level này chưa mở.
        void Refuse(int index);

        // Diễn hiệu ứng chọn level, khoá bấm tới khi xong rồi gọi `finished`.
        void PlaySelected(int index, Action finished);
    }

    // Bản đồ level phủ kín màn hình trong scene Home: một đường uốn lượn từ dưới lên, cuộn dọc, mở ra tại level hiện tại.
    // Khung và các lớp nằm sẵn trong scene; nút, chấm đường và nền được sinh từ prefab theo số level.
    public sealed class LevelMapView : MonoBehaviour, ILevelMapView
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button backButton;
        [Tooltip("Bản thử: xoá tiến độ để chơi lại từ đầu. Gỡ trước khi phát hành.")]
        [SerializeField] private Button resetButton;
        [Tooltip("Bản thử: mở khoá toàn bộ các level.")]
        [SerializeField] private Button unlockLevelButton;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;

        [Header("Các lớp trong nội dung cuộn")]
        [SerializeField] private RectTransform backgroundLayer;
        [SerializeField] private RectTransform pathLayer;
        [SerializeField] private RectTransform nodeLayer;
        [Tooltip("Mèo đứng trên nút level hiện tại.")]
        [SerializeField] private RectTransform marker;
        [SerializeField] private Image markerImage;
        [SerializeField] private Sprite normalCat;
        [Tooltip("Mèo đứng trên nút khi level hiện tại có cơ chế mới, để báo trước có điều mới.")]
        [SerializeField] private Sprite newMechanicCat;
        [SerializeField] private LevelMapEffects effects;

        [Header("Prefab")]
        [SerializeField] private RectTransform backgroundTilePrefab;
        [SerializeField] private Image pathDotPrefab;
        [SerializeField] private LevelMapNode nodePrefab;

        [Header("Đường đi")]
        [SerializeField] private float nodeSpacing = 240f;
        [Tooltip("Đường lượn sang hai bên, tính theo phần bề ngang màn hình.")]
        [SerializeField, Range(0f, 0.5f)] private float swingFraction = 0.27f;
        [SerializeField] private float swingRate = 0.8f;
        [SerializeField] private float bottomMargin = 300f;
        [SerializeField] private float topMargin = 460f;
        [SerializeField, Min(0)] private int dotsBetweenNodes = 3;
        [SerializeField] private Color reachedDotColor = new Color(1f, 0.86f, 0.3f, 1f);
        [SerializeField] private Color lockedDotColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private float markerHeight = 120f;
        [SerializeField] private float fadeSeconds = 0.2f;

        private readonly List<LevelMapNode> nodes = new List<LevelMapNode>();
        private readonly List<Image> dots = new List<Image>();

        public event Action<int> LevelClicked;

        public event Action BackClicked;

        public event Action ResetClicked;

        public event Action UnlockAllClicked;

        public void Show(IReadOnlyList<LevelMapEntry> entries)
        {
            gameObject.SetActive(true);
            canvasGroup.blocksRaycasts = true;
            Canvas.ForceUpdateCanvases();
            if (nodes.Count == 0) Build(entries.Count);

            var dot = 0;
            var current = 0;
            for (var index = 0; index < entries.Count; index++)
            {
                nodes[index].Show(entries[index]);
                if (entries[index].IsCurrent) current = index;
                if (index == 0) continue;
                for (var step = 0; step < dotsBetweenNodes; step++) dots[dot++].color = entries[index].IsUnlocked ? reachedDotColor : lockedDotColor;
            }

            marker.SetAsLastSibling();
            marker.anchoredPosition = NodePosition(current) + Vector2.up * markerHeight;
            markerImage.sprite = entries[current].IntroducesMechanic ? newMechanicCat : normalCat;
            CenterOn(current);
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }

        public void Hide() => gameObject.SetActive(false);

        public void Refuse(int index) => nodes[index].Shake();

        public void PlaySelected(int index, Action finished)
        {
            canvasGroup.blocksRaycasts = false;
            effects.Burst(nodes[index].Area);
            nodes[index].PlaySelected(finished);
        }

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(() => BackClicked?.Invoke());
            if (resetButton != null) resetButton.onClick.AddListener(() => ResetClicked?.Invoke());
            if (unlockLevelButton == null)
            {
                var found = transform.Find("MapSafeArea/UnlockLevelButton");
                if (found != null) unlockLevelButton = found.GetComponent<Button>();
            }
            if (unlockLevelButton != null)
            {
                unlockLevelButton.onClick.AddListener(UnlockAll);
            }
        }

        // Gọi từ button Unlock trong Inspector (UnityEvent OnClick) hoặc code
        public void UnlockAll() => UnlockAllClicked?.Invoke();

        private void Build(int count)
        {
            var height = bottomMargin + (count - 1) * nodeSpacing + topMargin;
            content.sizeDelta = new Vector2(content.sizeDelta.x, height);
            Canvas.ForceUpdateCanvases();
            BuildBackground(height);

            for (var index = 0; index < count; index++)
            {
                if (index > 0) BuildDots(NodePosition(index - 1), NodePosition(index));
                var node = Instantiate(nodePrefab, nodeLayer);
                ((RectTransform)node.transform).anchoredPosition = NodePosition(index);
                var level = index;
                node.Clicked += () => LevelClicked?.Invoke(level);
                nodes.Add(node);
            }
        }

        // Ảnh nền nối tiếp theo chiều dọc, tấm chẵn lật ngược để hai mép khớp nhau không lộ đường ghép.
        private void BuildBackground(float height)
        {
            var tileHeight = backgroundLayer.rect.width * backgroundTilePrefab.rect.height / backgroundTilePrefab.rect.width;
            var tileCount = Mathf.CeilToInt(height / tileHeight);
            for (var tile = 0; tile < tileCount; tile++)
            {
                var view = Instantiate(backgroundTilePrefab, backgroundLayer);
                view.sizeDelta = new Vector2(0f, tileHeight);
                view.anchoredPosition = new Vector2(0f, (tile + 0.5f) * tileHeight);
                view.localScale = new Vector3(1f, tile % 2 == 0 ? 1f : -1f, 1f);
            }
        }

        private void BuildDots(Vector2 from, Vector2 to)
        {
            for (var step = 0; step < dotsBetweenNodes; step++)
            {
                var dot = Instantiate(pathDotPrefab, pathLayer);
                dot.rectTransform.anchoredPosition = Vector2.Lerp(from, to, (step + 1f) / (dotsBetweenNodes + 1f));
                dots.Add(dot);
            }
        }

        private Vector2 NodePosition(int index) => new Vector2(
            Mathf.Sin(index * swingRate) * swingFraction * content.rect.width,
            bottomMargin + index * nodeSpacing);

        private void CenterOn(int index)
        {
            var scrollable = content.rect.height - scrollRect.viewport.rect.height;
            if (scrollable <= 0f) return;
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01((NodePosition(index).y - scrollRect.viewport.rect.height * 0.5f) / scrollable);
        }

        private IEnumerator FadeIn()
        {
            for (var time = 0f; time < fadeSeconds; time += Time.unscaledDeltaTime)
            {
                canvasGroup.alpha = time / fadeSeconds;
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
    }
}
