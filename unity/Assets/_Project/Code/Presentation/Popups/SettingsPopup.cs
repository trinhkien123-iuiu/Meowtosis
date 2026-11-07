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

        void Show(bool soundOn, bool musicOn);

        void Hide();
    }

    // Popup cài đặt: bật/tắt âm thanh và nhạc, chơi lại màn, về Home. Các nút để test nằm ở bảng cheat (CheatPopup).
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
        [SerializeField] private Button closeButton;
        [SerializeField] private Button homeButton;

        public event Action HomeClicked;

        public event Action SoundToggled;

        public event Action MusicToggled;

        public event Action RestartClicked;

        public void Show(bool soundOn, bool musicOn)
        {
            soundIcon.sprite = soundOn ? soundOnSprite : soundOffSprite;
            musicIcon.color = musicOn ? Color.white : offColor;
            if (!IsOpen) Open();
        }

        private void Awake()
        {
            soundButton.onClick.AddListener(() => SoundToggled?.Invoke());
            musicButton.onClick.AddListener(() => MusicToggled?.Invoke());
            restartButton.onClick.AddListener(() => RestartClicked?.Invoke());
            closeButton.onClick.AddListener(Hide);
            homeButton.onClick.AddListener(() => HomeClicked?.Invoke());
        }
    }
}
