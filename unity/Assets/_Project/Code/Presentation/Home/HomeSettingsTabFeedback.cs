using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    [DisallowMultipleComponent]
    public sealed class HomeSettingsTabFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float SpinSeconds = 0.65f;
        private RectTransform icon;
        private Button button;
        private Quaternion restRotation;
        private Vector3 baseScale;
        private bool hovered;
        private bool pressed;
        private float feedbackScale = 1f;
        private float spinTime = SpinSeconds;
        private float spinStart;
        private float angle;

        public void Bind(RectTransform target)
        {
            icon = target;
            button = GetComponent<Button>();
            restRotation = icon.localRotation;
            baseScale = icon.localScale;
        }

        public void SetBaseScale(Vector3 value) => baseScale = value;

        public void PlayClick()
        {
            if (!CanInteract) return;
            spinStart = Mathf.Repeat(angle, 360f);
            spinTime = 0f;
        }

        private bool CanInteract => button != null && button.IsActive() && button.IsInteractable();

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Touch has press feedback; mouse hover must not remain after a finger lifts.
            if (eventData.pointerId < 0) hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && CanInteract) pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData) => pressed = false;

        private void LateUpdate()
        {
            if (icon == null) return;
            var targetScale = CanInteract ? (pressed ? 0.92f : hovered ? 1.09f : 1f) : 1f;
            feedbackScale = Mathf.Lerp(feedbackScale, targetScale, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
            var pop = 0f;
            if (spinTime < SpinSeconds)
            {
                spinTime = Mathf.Min(SpinSeconds, spinTime + Time.unscaledDeltaTime);
                var t = spinTime / SpinSeconds;
                var remaining = 1f - t;
                // A complete turn settles at the original orientation, even after repeated clicks.
                angle = Mathf.LerpUnclamped(spinStart, 360f, 1f - remaining * remaining * remaining);
                pop = Mathf.Sin(t * Mathf.PI) * 0.12f;
                if (spinTime >= SpinSeconds) angle = 0f;
            }
            icon.localRotation = restRotation * Quaternion.Euler(0f, 0f, angle);
            icon.localScale = baseScale * (feedbackScale + pop);
        }

        private void OnDisable()
        {
            hovered = pressed = false;
            feedbackScale = 1f;
            spinTime = SpinSeconds;
            spinStart = angle = 0f;
            if (icon == null) return;
            icon.localRotation = restRotation;
            icon.localScale = baseScale;
        }
    }
}
