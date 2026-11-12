using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Một ô trong bảng chọn avatar (Prefabs/Hud/AvatarChoice): hình, tên, viền sáng và dấu tích khi đang dùng.
    public sealed class AvatarChoiceView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image picture;
        [SerializeField] private TMP_Text title;
        [SerializeField] private GameObject selectedMark;

        private string avatarId;
        private Sprite portrait;

        public event Action<string> Clicked;

        public void Show(string id, Sprite sprite, string text, bool selected)
        {
            avatarId = id;
            picture.sprite = sprite;
            portrait = sprite;
            title.text = text;
            SetSelected(selected);
        }

        public void SetSelected(bool selected) => selectedMark.SetActive(selected);

        public void SetFrame(Sprite frame, Sprite mask) => AvatarPortraitFrame.Show(picture, portrait, frame, mask);

        public string AvatarId => avatarId;

        private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke(avatarId));
    }
}
