using System;
using System.Collections.Generic;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // HUD màn chơi dựng sẵn trong scene Game (Canvas "Hud"): thanh trên (level, tim, cài đặt), thanh sao,
    // nhiệm vụ, bong bóng lời thoại, hàng đếm mèo, hàng booster, thông báo ngắn, bảng chọn màu (chỉ hiện khi dùng Hoán đổi).
    public sealed class HudView : MonoBehaviour, IHudView
    {
        [Header("Thanh trên")]
        [SerializeField] private TMP_Text levelText;
        [Tooltip("Nút trong suốt phủ ô Lv; chạm nhanh 3 lần mở bảng cheat.")]
        [SerializeField] private Button levelBadgeButton;
        [SerializeField] private float tripleTapSeconds = 0.6f;
        [SerializeField] private Image[] heartIcons;
        [SerializeField] private Color emptyHeartColor = new Color(0.25f, 0.28f, 0.5f, 0.85f);
        [SerializeField] private TMP_Text heartTimerText;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button restartButton;

        [Header("Giữa màn")]
        [SerializeField] private StarMeterView starMeter;
        [SerializeField] private GoalPillView goalPill;
        [SerializeField] private GameObject tipBubble;
        [SerializeField] private TMP_Text tipText;
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private float toastSeconds = 2.4f;

        [Header("Level có hướng dẫn")]
        [Tooltip("Các phần HUD ẩn khi level có bước hướng dẫn: thanh trên, tim, nút, thanh sao, viên mục tiêu.")]
        [SerializeField] private GameObject[] hiddenWhileGuided;
        [Tooltip("Độ cao của bong bóng lời thoại (neo trên) khi level có hướng dẫn; bình thường thì giữ vị trí đặt trong scene.")]
        [SerializeField] private float guidedTipY = -250f;

        [Header("Mèo")]
        [SerializeField] private BoardArt art;
        [SerializeField] private SpeciesChip speciesChipPrefab;
        [SerializeField] private Transform speciesRow;
        [SerializeField] private GameObject colorPicker;
        [SerializeField] private Transform colorChoiceRow;
        [SerializeField] private SpeciesChip colorChoicePrefab;

        [Header("Booster")]
        [SerializeField] private BoosterButton hintButton;
        [SerializeField] private BoosterButton mutateButton;
        [SerializeField] private BoosterButton transplantButton;

        private readonly List<SpeciesChip> speciesChips = new List<SpeciesChip>();
        private readonly List<SpeciesChip> colorChoices = new List<SpeciesChip>();
        private float toastTimeLeft;
        private float normalTipY;
        private bool hasNormalTipY;

        public event Action SettingsClicked;

        public event Action RestartClicked;

        public event Action HintClicked;

        public event Action<BoosterKind> BoosterClicked;

        public event Action<Species> ColorChosen;

        public event Action LevelBadgeTripleTapped;

        private int badgeTaps;
        private float lastBadgeTap = -10f;

        public void ShowLevel(int levelNumber) => levelText.text = GameTexts.LevelShort(levelNumber);

        public void ShowHearts(int hearts, string timer)
        {
            for (var index = 0; index < heartIcons.Length; index++) heartIcons[index].color = index < hearts ? Color.white : emptyHeartColor;
            heartTimerText.text = timer;
        }

        public void ShowStars(StarMeter meter, bool instantly) => starMeter.Show(meter, instantly);

        public void ShowGoal(GoalProgress progress, bool instantly) => goalPill.Show(progress, instantly);

        public void ShowTip(string text)
        {
            // Cỡ chữ, tự co, màu, viền chỉnh trên TipText trong scene Game.
            if (tipText != null) tipText.text = text;
            if (tipBubble != null) tipBubble.SetActive(!string.IsNullOrEmpty(text));
        }

        public void SetGuidedLayout(bool guided)
        {
            foreach (var part in hiddenWhileGuided)
            {
                if (part != null) part.SetActive(!guided);
            }
            if (tipBubble == null) return;
            var bubble = (RectTransform)tipBubble.transform;
            if (!hasNormalTipY)
            {
                normalTipY = bubble.anchoredPosition.y;
                hasNormalTipY = true;
            }
            bubble.anchoredPosition = new Vector2(bubble.anchoredPosition.x, guided ? guidedTipY : normalTipY);
        }

        public void ShowSpecies(IReadOnlyList<SpeciesCount> counts)
        {
            EnsureChips(speciesChips, speciesChipPrefab, speciesRow, counts.Count);
            for (var index = 0; index < counts.Count; index++)
            {
                var species = counts[index].Species;
                speciesChips[index].ShowCount(species, art.CatSprite(species, false), counts[index].Count);
            }
        }

        public void ShowBooster(BoosterKind kind, int count, bool selected)
        {
            var button = kind switch
            {
                BoosterKind.Mutate => mutateButton,
                BoosterKind.Transplant => transplantButton,
                _ => hintButton,
            };
            button.ShowCount(count);
            button.SetSelected(selected);
        }

        public void SetHintEnabled(bool enabled) => hintButton.SetInteractable(enabled);

        public void ShowToast(string text)
        {
            toastText.text = text;
            toastPanel.SetActive(text.Length > 0);
            toastTimeLeft = toastSeconds;
        }

        public void ShowColorChoices(IReadOnlyList<Species> colors)
        {
            colorPicker.SetActive(colors.Count > 0);
            EnsureChips(colorChoices, colorChoicePrefab, colorChoiceRow, colors.Count);
            for (var index = 0; index < colors.Count; index++)
                colorChoices[index].Show(colors[index], art.CatSprite(colors[index], false), GameTexts.ColorName(colors[index]));
        }

        private void Awake()
        {
            settingsButton.onClick.AddListener(() => SettingsClicked?.Invoke());
            if (restartButton != null) restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            if (levelBadgeButton != null) levelBadgeButton.onClick.AddListener(OnLevelBadgeTapped);
            hintButton.Clicked += () => HintClicked?.Invoke();
            mutateButton.Clicked += () => BoosterClicked?.Invoke(BoosterKind.Mutate);
            transplantButton.Clicked += () => BoosterClicked?.Invoke(BoosterKind.Transplant);
            toastPanel.SetActive(false);
            colorPicker.SetActive(false);
        }

        // Đếm số lần chạm liên tiếp, mỗi lần cách nhau không quá tripleTapSeconds.
        private void OnLevelBadgeTapped()
        {
            badgeTaps = Time.unscaledTime - lastBadgeTap <= tripleTapSeconds ? badgeTaps + 1 : 1;
            lastBadgeTap = Time.unscaledTime;
            if (badgeTaps < 3) return;
            badgeTaps = 0;
            LevelBadgeTripleTapped?.Invoke();
        }

        private void Update()
        {
            if (toastTimeLeft <= 0f) return;
            toastTimeLeft -= Time.deltaTime;
            if (toastTimeLeft <= 0f) toastPanel.SetActive(false);
        }

        // Giữ đúng `count` ô trong hàng: thêm từ prefab khi thiếu, ẩn bớt khi thừa (số màu đổi theo level).
        private void EnsureChips(List<SpeciesChip> chips, SpeciesChip prefab, Transform row, int count)
        {
            while (chips.Count < count)
            {
                var chip = Instantiate(prefab, row);
                chip.Clicked += color => ColorChosen?.Invoke(color);
                chips.Add(chip);
            }
            for (var index = 0; index < chips.Count; index++) chips[index].gameObject.SetActive(index < count);
        }
    }
}
