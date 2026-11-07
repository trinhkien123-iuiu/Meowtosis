using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    public interface ICheatPopup
    {
        bool IsOpen { get; }

        event Action WinClicked;

        event Action SolveStepClicked;

        // Dời level đang chọn: -10, -1, +1, +10.
        event Action<int> LevelStepped;

        event Action OpenLevelClicked;

        event Action RefillClicked;

        event Action CoinsClicked;

        event Action UnlockAllClicked;

        event Action OnboardingClicked;

        // `chosenLevel` là level sẽ mở khi bấm "Mở level" (đếm từ 1).
        void Show(int chosenLevel, int levelCount);

        void Hide();
    }

    // Bảng cheat cho bản thử: mở bằng cách chạm nhanh 3 lần vào ô "Lv" trên HUD. Bố cục dựng sẵn trong scene Game
    // (Hud/CheatPopup), code chỉ nối nút với sự kiện. Gỡ khỏi scene trước khi phát hành.
    public sealed class CheatPopup : PopupView, ICheatPopup
    {
        [SerializeField] private Button winButton;
        [SerializeField] private Button solveStepButton;
        [SerializeField] private Button backTenButton;
        [SerializeField] private Button backOneButton;
        [SerializeField] private Button forwardOneButton;
        [SerializeField] private Button forwardTenButton;
        [SerializeField] private TMP_Text chosenLevelText;
        [SerializeField] private Button openLevelButton;
        [SerializeField] private Button refillButton;
        [SerializeField] private Button coinsButton;
        [SerializeField] private Button unlockAllButton;
        [SerializeField] private Button onboardingButton;
        [SerializeField] private Button closeButton;

        public event Action WinClicked;

        public event Action SolveStepClicked;

        public event Action<int> LevelStepped;

        public event Action OpenLevelClicked;

        public event Action RefillClicked;

        public event Action CoinsClicked;

        public event Action UnlockAllClicked;

        public event Action OnboardingClicked;

        public void Show(int chosenLevel, int levelCount)
        {
            chosenLevelText.text = GameTexts.CheatChosenLevel(chosenLevel, levelCount);
            if (!IsOpen) Open();
        }

        private void Awake()
        {
            winButton.onClick.AddListener(() => WinClicked?.Invoke());
            solveStepButton.onClick.AddListener(() => SolveStepClicked?.Invoke());
            backTenButton.onClick.AddListener(() => LevelStepped?.Invoke(-10));
            backOneButton.onClick.AddListener(() => LevelStepped?.Invoke(-1));
            forwardOneButton.onClick.AddListener(() => LevelStepped?.Invoke(1));
            forwardTenButton.onClick.AddListener(() => LevelStepped?.Invoke(10));
            openLevelButton.onClick.AddListener(() => OpenLevelClicked?.Invoke());
            refillButton.onClick.AddListener(() => RefillClicked?.Invoke());
            coinsButton.onClick.AddListener(() => CoinsClicked?.Invoke());
            unlockAllButton.onClick.AddListener(() => UnlockAllClicked?.Invoke());
            onboardingButton.onClick.AddListener(() => OnboardingClicked?.Invoke());
            closeButton.onClick.AddListener(Hide);
        }
    }
}
