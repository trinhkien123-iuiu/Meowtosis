using System.Collections;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Nền chung của mọi popup trong scene: lớp tối phủ màn hình (CanvasGroup) và tấm thẻ nảy lên khi mở.
    public abstract class PopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform card;
        [SerializeField] private float openSeconds = 0.32f;
        [Tooltip("Cỡ của thẻ theo thời gian mở (0 tới 1), nảy quá 1 một chút rồi về 1.")]
        [SerializeField] private AnimationCurve openScale = new AnimationCurve(
            new Keyframe(0f, 0.6f), new Keyframe(0.7f, 1.06f), new Keyframe(1f, 1f));

        public bool IsOpen => gameObject.activeSelf;

        public void Hide() => gameObject.SetActive(false);

        protected void Open()
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            for (var time = 0f; time < openSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / openSeconds;
                canvasGroup.alpha = Mathf.Clamp01(progress * 2f);
                card.localScale = Vector3.one * openScale.Evaluate(progress);
                yield return null;
            }
            canvasGroup.alpha = 1f;
            card.localScale = Vector3.one;
        }
    }
}
