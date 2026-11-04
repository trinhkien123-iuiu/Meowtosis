using System.Collections;
using TMPro;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Ô tổng xu của panel thắng: bật ra rồi nhận xu bay về, mỗi đồng về thì số tăng và icon nảy một cái.
    public sealed class CoinCounterView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text amountText;
        [Tooltip("Xu bay về đúng chỗ này.")]
        [SerializeField] private RectTransform icon;
        [SerializeField] private float appearSeconds = 0.3f;
        [SerializeField] private AnimationCurve appearScale = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.7f, 1.1f), new Keyframe(1f, 1f));
        [SerializeField] private float popSeconds = 0.14f;
        [SerializeField] private float popAmount = 0.25f;

        private Coroutine popping;

        public RectTransform Icon => icon;

        public void Hide() => gameObject.SetActive(false);

        public void Appear(int amount)
        {
            gameObject.SetActive(true);
            amountText.text = amount.ToString();
            icon.localScale = Vector3.one;
            StartCoroutine(AnimateAppear());
        }

        public void Receive(int amount)
        {
            amountText.text = amount.ToString();
            if (popping != null) StopCoroutine(popping);
            popping = StartCoroutine(PopIcon());
        }

        private IEnumerator AnimateAppear()
        {
            for (var time = 0f; time < appearSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / appearSeconds;
                group.alpha = progress;
                transform.localScale = Vector3.one * appearScale.Evaluate(progress);
                yield return null;
            }
            group.alpha = 1f;
            transform.localScale = Vector3.one;
        }

        private IEnumerator PopIcon()
        {
            for (var time = 0f; time < popSeconds; time += Time.unscaledDeltaTime)
            {
                icon.localScale = Vector3.one * (1f + popAmount * Mathf.Sin(time / popSeconds * Mathf.PI));
                yield return null;
            }
            icon.localScale = Vector3.one;
        }
    }
}
