using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    public interface ISettingsPopup
    {
        bool IsOpen { get; }

        event Action HomeClicked;

        event Action SoundToggled;

        event Action MusicToggled;

        event Action RestartClicked;

        event Action PreviousLevelClicked;

        event Action NextLevelClicked;

        event Action RefillClicked;

        void Show(bool soundOn, bool musicOn, int levelNumber);

        void Hide();
    }

    // Popup cài đặt: bật/tắt âm thanh và nhạc, chơi lại màn. Phần "Bản thử" (chuyển level, nạp đầy tim và booster)
    // chỉ để test cho tới khi có bản đồ level.
    public sealed class SettingsPopup : PopupView, ISettingsPopup
    {
        [SerializeField] private Button soundButton;
        [SerializeField] private Image soundIcon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private Button musicButton;
        [SerializeField] private Image musicIcon;
        [SerializeField] private Color offColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Button restartButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button refillButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button homeButton;

        public event Action HomeClicked;

        public event Action SoundToggled;

        public event Action MusicToggled;

        public event Action RestartClicked;

        public event Action PreviousLevelClicked;

        public event Action NextLevelClicked;

        public event Action RefillClicked;

        public void Show(bool soundOn, bool musicOn, int levelNumber)
        {
            soundIcon.sprite = soundOn ? soundOnSprite : soundOffSprite;
            musicIcon.color = musicOn ? Color.white : offColor;
            levelText.text = GameTexts.LevelBadge(levelNumber);
            if (!IsOpen) Open();
        }

        private void Awake()
        {
            soundButton.onClick.AddListener(() => SoundToggled?.Invoke());
            musicButton.onClick.AddListener(() => MusicToggled?.Invoke());
            restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            previousButton.onClick.AddListener(() => PreviousLevelClicked?.Invoke());
            nextButton.onClick.AddListener(() => NextLevelClicked?.Invoke());
            refillButton.onClick.AddListener(() => RefillClicked?.Invoke());
            closeButton.onClick.AddListener(Hide);
            homeButton.onClick.AddListener(() => HomeClicked?.Invoke());
        }
    }
}
