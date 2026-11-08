using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Các kiểu nền màn chơi (kéo thả trong Inspector). Nút trong bảng Cài đặt chuyển sang kiểu kế tiếp.
    // Lựa chọn chỉ là tuỳ chọn hiển thị trên máy này nên lưu thẳng vào PlayerPrefs, không đi qua tiến độ người chơi.
    public sealed class BackgroundSwitcher : MonoBehaviour
    {
        private const string PreferenceKey = "meowtosis-background";

        [Serializable]
        private sealed class Look
        {
            [SerializeField] private string displayName = "";
            [SerializeField] private Sprite image;
            [Tooltip("Hiện lớp hoạ tiết chân mèo phủ trên nền.")]
            [SerializeField] private bool showPattern = true;
            [Tooltip("Màu nhân lên ảnh nền; tối hơn thì ô gỗ nổi rõ hơn khỏi nền.")]
            [SerializeField] private Color tint = Color.white;

            public string DisplayName => displayName;

            public Sprite Image => image;

            public bool ShowPattern => showPattern;

            public Color Tint => tint;
        }

        [SerializeField] private SpriteRenderer background;
        [SerializeField] private SpriteRenderer pattern;
        [SerializeField] private CameraFraming framing;
        [SerializeField] private Look[] looks = Array.Empty<Look>();
        [SerializeField] private Button switchButton;
        [SerializeField] private TMP_Text switchLabel;

        private int current;

        private void Awake()
        {
            if (switchButton != null) switchButton.onClick.AddListener(ShowNext);
            current = ReadPreference();
            Show(current);
        }

        private void ShowNext()
        {
            if (looks.Length == 0) return;
            current = (current + 1) % looks.Length;
            Show(current);
            PlayerPrefs.SetInt(PreferenceKey, current);
            PlayerPrefs.Save();
        }

        private void Show(int index)
        {
            if (looks.Length == 0) return;
            var look = looks[index];
            background.sprite = look.Image;
            background.color = look.Tint;
            pattern.enabled = look.ShowPattern;
            framing.Refit();
            if (switchLabel != null) switchLabel.text = GameTexts.BackgroundButton(look.DisplayName);
        }

        private int ReadPreference()
        {
            var saved = PlayerPrefs.GetInt(PreferenceKey, 0);
            return saved >= 0 && saved < looks.Length ? saved : 0;
        }
    }
}
