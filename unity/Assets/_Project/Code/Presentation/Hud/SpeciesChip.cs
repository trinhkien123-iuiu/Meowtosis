using System;
using Meowtosis.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Một ô nhỏ có hình mèo và một dòng chữ (Prefabs/Hud/SpeciesChip, ColorChoice): dùng cho hàng đếm mèo dưới bàn
    // và cho bảng chọn màu của Hoán đổi (prefab ColorChoice có thêm nút bấm).
    public sealed class SpeciesChip : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;

        private Species species;

        // Báo màu của ô vừa được bấm.
        public event Action<Species> Clicked;

        public void Show(Species shownSpecies, Sprite sprite, string text)
        {
            species = shownSpecies;
            icon.sprite = sprite;
            label.text = text;
        }

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(species));
        }
    }
}
