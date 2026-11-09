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
        [Tooltip("Chữ Z bay lên khi mọi con màu này đang ngủ (chỉ hàng đếm dưới bàn có).")]
        [SerializeField] private ChipSleepEffect sleepEffect;

        private Species species;

        // Báo màu của ô vừa được bấm.
        public event Action<Species> Clicked;

        public void Show(Species shownSpecies, Sprite sprite, string text)
        {
            species = shownSpecies;
            icon.sprite = sprite;
            icon.color = Color.white;
            label.text = text;
        }

        // `allAsleep`: mọi con màu này đang ngủ, chip mờ đi và thêm chữ z để người chơi biết màu này không cần lo.
        public void ShowCount(Species shownSpecies, Sprite sprite, int count, bool allAsleep = false)
        {
            species = shownSpecies;
            icon.sprite = sprite;
            icon.color = allAsleep ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            label.text = count.ToString();
            label.alpha = allAsleep ? 0.6f : 1f;
            if (sleepEffect != null) sleepEffect.SetAsleep(allAsleep);
        }

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(species));
        }
    }
}
