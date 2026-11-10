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
        [Tooltip("Nhịp xuất hiện của bong bóng (nảy, hiện dần từng chữ). Bỏ trống thì bật tắt bong bóng như cũ.")]
        [SerializeField] private TipBubbleView tipBubbleView;
        [Tooltip("Mèo cam dẫn chuyện cạnh bong bóng.")]
        [SerializeField] private NarratorCat narrator;
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
        [Tooltip("Bong bóng tròn chọn màu của Hoán đổi, bật ra trên con mèo vừa chạm.")]
        [SerializeField] private ColorChoiceBubble colorBubble;

        [Header("Booster")]
        [SerializeField] private BoosterButton hintButton;
        [SerializeField] private BoosterButton mutateButton;
        [SerializeField] private BoosterButton transplantButton;

        [Header("Đang dùng booster: viên lời nhắn dưới bàn (cùng kiểu thông báo ngắn); icon, tên, nút huỷ để trống cũng được")]
        [SerializeField] private GameObject boosterPanel;
        [SerializeField] private Image boosterPanelIcon;
        [SerializeField] private TMP_Text boosterPanelTitle;
        [SerializeField] private TMP_Text boosterPanelText;
        [SerializeField] private Button boosterPanelClose;
        [SerializeField] private Sprite mutateIcon;
        [SerializeField] private Sprite transplantIcon;
        [SerializeField] private Color promptColor = new Color(0.36f, 0.2f, 0.12f);
        [SerializeField] private Color warningColor = new Color(0.85f, 0.2f, 0.18f);
        [Tooltip("Ẩn đi khi đang chọn mục tiêu booster (hàng booster, hàng đếm mèo, nền hàng booster).")]
        [SerializeField] private GameObject[] hiddenWhileTargeting;

        private readonly List<SpeciesChip> speciesChips = new List<SpeciesChip>();
        private bool[] activeBeforeTargeting;
        private string boosterPrompt = "";
        private float warningTimeLeft;
        private float toastTimeLeft;
        private float normalTipY;
        private bool hasNormalTipY;

        public event Action SettingsClicked;

        public event Action RestartClicked;

        public event Action BoosterCancelClicked;

        public event Action<int> StarLost;

        public event Action<int> StarGained;

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

        public void ShowTip(string text, NarratorMood mood = NarratorMood.Explain)
        {
            // Cỡ chữ, tự co, màu, viền chỉnh trên TipText trong scene Game.
            var hasText = !string.IsNullOrEmpty(text);
            var rich = Pictograms.FormatWithBigPictures(text);
            if (tipBubbleView != null) tipBubbleView.Show(rich);
            else
            {
                if (tipText != null) tipText.text = rich;
                if (tipBubble != null) tipBubble.SetActive(hasText);
            }
            if (narrator == null) return;
            if (hasText) narrator.Say(text, mood);
            else narrator.Hide();
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

        public void ShowBoosterPrompt(BoosterKind kind, string prompt)
        {
            if (boosterPanel == null) return;
            var show = kind == BoosterKind.Mutate || kind == BoosterKind.Transplant;
            if (show && !boosterPanel.activeSelf)
            {
                activeBeforeTargeting = new bool[hiddenWhileTargeting.Length];
                for (var index = 0; index < hiddenWhileTargeting.Length; index++)
                {
                    if (hiddenWhileTargeting[index] == null) continue;
                    activeBeforeTargeting[index] = hiddenWhileTargeting[index].activeSelf;
                    hiddenWhileTargeting[index].SetActive(false);
                }
            }
            else if (!show && boosterPanel.activeSelf && activeBeforeTargeting != null)
            {
                for (var index = 0; index < hiddenWhileTargeting.Length && index < activeBeforeTargeting.Length; index++)
                {
                    if (hiddenWhileTargeting[index] != null) hiddenWhileTargeting[index].SetActive(activeBeforeTargeting[index]);
                }
            }
            boosterPanel.SetActive(show);
            if (!show) return;
            if (boosterPanelIcon != null) boosterPanelIcon.sprite = kind == BoosterKind.Mutate ? mutateIcon : transplantIcon;
            if (boosterPanelTitle != null) boosterPanelTitle.text = GameTexts.BoosterName(kind);
            boosterPrompt = prompt;
            if (warningTimeLeft <= 0f) ShowPromptText(prompt, false);
        }

        private void ShowPromptText(string text, bool warning)
        {
            boosterPanelText.text = text;
            boosterPanelText.color = warning ? warningColor : promptColor;
        }

        public void ShowToast(string text)
        {
            // Đang chọn mục tiêu booster: báo lỗi ngay trong thanh booster (đỏ một lúc) thay vì bật thông báo đè lên bàn.
            if (boosterPanel != null && boosterPanel.activeSelf && text.Length > 0)
            {
                ShowPromptText(text, true);
                warningTimeLeft = toastSeconds * 0.7f;
                return;
            }
            toastText.text = text;
            toastPanel.SetActive(text.Length > 0);
            toastTimeLeft = toastSeconds;
        }

        public void ShowColorChoices(IReadOnlyList<Species> colors, Vector2 screenPoint)
        {
            if (colors.Count == 0) colorBubble.Hide();
            else colorBubble.Show(colors, screenPoint);
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
            colorBubble.Chosen += color => ColorChosen?.Invoke(color);
            starMeter.StarLost += lost => StarLost?.Invoke(lost);
            starMeter.StarGained += gained => StarGained?.Invoke(gained);
            colorBubble.Hide();
            if (boosterPanelClose != null) boosterPanelClose.onClick.AddListener(() => BoosterCancelClicked?.Invoke());
            if (boosterPanel != null) boosterPanel.SetActive(false);
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
            if (warningTimeLeft > 0f && (warningTimeLeft -= Time.deltaTime) <= 0f) ShowPromptText(boosterPrompt, false);
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
