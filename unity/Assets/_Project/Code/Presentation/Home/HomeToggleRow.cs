using System;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Một hàng bật tắt trong trang Setting: nút có hai trạng thái dựng sẵn trong scene.
    public sealed class HomeToggleRow : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject onState;
        [SerializeField] private GameObject offState;

        public event Action Clicked;

        public void Show(bool isOn)
        {
            onState.SetActive(isOn);
            offState.SetActive(!isOn);
        }

        private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke());
    }
}
