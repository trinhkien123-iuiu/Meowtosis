using System;
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

        void Show(bool soundOn, bool musicOn);

        void Hide();
    }

    // Popup cài đặt: bốn nút tròn bật/tắt âm thanh, bật/tắt nhạc, bật/tắt rung lắc (giảm chuyển động) và về Home, dùng cùng bộ hình (Bounce) với trang Setting ở Home.
    // Chơi lại màn có nút riêng trên thanh trên; các nút để test nằm ở bảng cheat (CheatPopup).
    public sealed class SettingsPopup : PopupView, ISettingsPopup
    {
        [SerializeField] private Button soundButton;
        [SerializeField] private Image soundBackground;
        [SerializeField] private Image soundIcon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private Button musicButton;
        [SerializeField] private Image musicBackground;
        [SerializeField] private Image musicIcon;
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;
        [SerializeField] private Sprite toggleOnSprite;
        [SerializeField] private Sprite toggleOffSprite;
        [Tooltip("Nút giảm chuyển động (rung bàn, dừng hình): lưu ở MotionSettings, tự xử lý không qua presenter.")]
        [SerializeField] private Button motionButton;
        [SerializeField] private Image motionBackground;
        [SerializeField] private Image motionIcon;
        [SerializeField] private Sprite motionOnSprite;
        [SerializeField] private Sprite motionOffSprite;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button closeButton;

        public event Action HomeClicked;

        public event Action SoundToggled;

        public event Action MusicToggled;

        public void Show(bool soundOn, bool musicOn)
        {
            soundIcon.sprite = soundOn ? soundOnSprite : soundOffSprite;
            soundBackground.sprite = soundOn ? toggleOnSprite : toggleOffSprite;
            musicIcon.sprite = musicOn ? musicOnSprite : musicOffSprite;
            musicBackground.sprite = musicOn ? toggleOnSprite : toggleOffSprite;
            ShowMotion();
            if (!IsOpen) Open();
        }

        // Sáng khi còn chuyển động mạnh (rung, dừng hình); tắt (gạch chéo) khi đã bật chế độ giảm chuyển động.
        private void ShowMotion()
        {
            var on = !MotionSettings.Reduced;
            motionIcon.sprite = on ? motionOnSprite : motionOffSprite;
            motionBackground.sprite = on ? toggleOnSprite : toggleOffSprite;
        }

        private void Awake()
        {
            soundButton.onClick.AddListener(() => SoundToggled?.Invoke());
            musicButton.onClick.AddListener(() => MusicToggled?.Invoke());
            motionButton.onClick.AddListener(() =>
            {
                MotionSettings.Reduced = !MotionSettings.Reduced;
                ShowMotion();
            });
            closeButton.onClick.AddListener(Hide);
            homeButton.onClick.AddListener(() => HomeClicked?.Invoke());
        }
    }
}
