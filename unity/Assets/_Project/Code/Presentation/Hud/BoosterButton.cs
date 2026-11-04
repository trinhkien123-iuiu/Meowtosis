using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Nút tròn booster (Prefabs/Hud/BoosterButton): icon, tên, huy hiệu số lượt hoặc dấu cộng khi hết lượt.
    public sealed class BoosterButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject countBadge;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private GameObject plusBadge;
        [SerializeField] private GameObject selectedGlow;

        public event Action Clicked;

        // Booster có lượt: hiện số lượt, hết lượt hiện dấu cộng (bấm để mua bằng xu).
        public void ShowCount(int count)
        {
            countBadge.SetActive(count > 0);
            plusBadge.SetActive(count == 0);
            countText.text = count.ToString();
        }

        public void SetSelected(bool selected) => selectedGlow.SetActive(selected);

        public void SetInteractable(bool interactable) => button.interactable = interactable;

        private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke());
    }
}
