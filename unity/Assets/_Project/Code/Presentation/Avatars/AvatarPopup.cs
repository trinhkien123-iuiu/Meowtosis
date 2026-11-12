using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Bảng chọn avatar kiểu Profile của Bounce Blast (Home/HomeCanvas/AvatarPopup):
    // trên cùng là ảnh avatar đang chọn, giữa là lưới 3 cột cuộn được (ô AvatarChoice dựng từ AvatarCatalog, ô đầu là Mặc định),
    // chạm một ô thì ô đó có viền và dấu tích, ảnh trên cập nhật; bấm Lưu mới báo Chosen. Nút X hoặc nền tối thì đóng không lưu.
    public sealed class AvatarPopup : PopupView
    {
        [SerializeField] private AvatarCatalog catalog;
        [SerializeField] private AvatarChoiceView choicePrefab;
        [SerializeField] private RectTransform grid;
        [SerializeField] private ScrollRect scroll;
        [Tooltip("Ảnh avatar đang chọn ở đầu bảng.")]
        [SerializeField] private Image preview;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button closeButton;
        [Tooltip("Lớp tối phía sau thẻ: chạm vào cũng đóng.")]
        [SerializeField] private Button shadeButton;

        private readonly List<AvatarChoiceView> choices = new List<AvatarChoiceView>();
        private string pickedId = AvatarCatalog.DefaultId;

        // Bấm Lưu với avatar `id` (rỗng là mặc định).
        public event Action<string> Chosen;

        public void Show(string selectedId)
        {
            if (choices.Count == 0) Build();
            Pick(catalog.PictureOf(selectedId) != null ? selectedId : AvatarCatalog.DefaultId);
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            if (!IsOpen) Open();
        }

        private void Build()
        {
            Add(AvatarCatalog.DefaultId, catalog.DefaultPicture, GameTexts.DefaultAvatar);
            foreach (var option in catalog.Options) Add(option.Id, option.Picture, option.Title);
        }

        private void Add(string id, Sprite sprite, string title)
        {
            var choice = Instantiate(choicePrefab, grid);
            choice.Show(id, sprite, title, false);
            choice.SetFrame(catalog.Frame, catalog.PortraitMask);
            choice.Clicked += Pick;
            choices.Add(choice);
        }

        private void Pick(string id)
        {
            pickedId = id;
            foreach (var choice in choices) choice.SetSelected(choice.AvatarId == id);
            AvatarPortraitFrame.Show(preview, catalog.PictureOf(id) ?? catalog.DefaultPicture, catalog.Frame, catalog.PortraitMask);
        }

        private void Save()
        {
            Chosen?.Invoke(pickedId);
            Hide();
        }

        private void Awake()
        {
            saveButton.onClick.AddListener(Save);
            closeButton.onClick.AddListener(Hide);
            if (shadeButton != null) shadeButton.onClick.AddListener(Hide);
        }
    }
}
