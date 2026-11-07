using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Gắn trên Canvas "Hud" của scene Game. Danh sách phần HUD bị ẩn và nút dưới bàn đều kéo thả trong Inspector.
    public sealed class OnboardingView : MonoBehaviour, IOnboardingView
    {
        [Tooltip("Các phần HUD ẩn đi trong lúc hướng dẫn; xong thì trả về đúng trạng thái trước đó.")]
        [SerializeField] private GameObject[] hiddenWhileOnboarding;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionLabel;

        private bool[] wasActive = Array.Empty<bool>();

        public event Action ButtonClicked;

        private void Awake()
        {
            actionButton.onClick.AddListener(() => ButtonClicked?.Invoke());
            HideButton();
        }

        public void Enter()
        {
            wasActive = new bool[hiddenWhileOnboarding.Length];
            for (var index = 0; index < hiddenWhileOnboarding.Length; index++)
            {
                // Mục trống (đối tượng đã bị xoá khỏi scene) thì bỏ qua, để không chặn cả màn hướng dẫn.
                if (hiddenWhileOnboarding[index] == null) continue;
                wasActive[index] = hiddenWhileOnboarding[index].activeSelf;
                hiddenWhileOnboarding[index].SetActive(false);
            }
            HideButton();
        }

        public void Exit()
        {
            for (var index = 0; index < Math.Min(wasActive.Length, hiddenWhileOnboarding.Length); index++)
            {
                if (hiddenWhileOnboarding[index] != null) hiddenWhileOnboarding[index].SetActive(wasActive[index]);
            }
            HideButton();
        }

        public void ShowButton(string label)
        {
            actionLabel.text = label;
            actionButton.gameObject.SetActive(true);
        }

        public void HideButton() => actionButton.gameObject.SetActive(false);
    }
}
