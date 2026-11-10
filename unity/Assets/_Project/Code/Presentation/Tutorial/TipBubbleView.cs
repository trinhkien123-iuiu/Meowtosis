using System.Collections;
using TMPro;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Bong bóng lời hướng dẫn phía trên bàn (TipBubble): nảy ra khi xuất hiện, nảy nhẹ khi đổi câu,
    // hình minh hoạ và chữ hiện dần từng ký tự như đang nói; tắt thì thu nhỏ rồi ẩn. Chạy theo thời gian thật.
    public sealed class TipBubbleView : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private TMP_Text text;
        [SerializeField] private float popSeconds = 0.28f;
        [SerializeField] private float bumpSeconds = 0.2f;
        [SerializeField] private float hideSeconds = 0.14f;
        [Tooltip("Số ký tự hiện thêm mỗi giây.")]
        [SerializeField] private float charactersPerSecond = 40f;
        [Tooltip("Mỗi hình minh hoạ giữ thêm chừng này giây cho mắt kịp nhìn.")]
        [SerializeField] private float pictureHoldSeconds = 0.14f;

        private string shown = "";
        private Coroutine routine;

        public void Show(string richText)
        {
            if (string.IsNullOrEmpty(richText))
            {
                Hide();
                return;
            }
            if (gameObject.activeSelf && richText == shown) return;
            var wasVisible = gameObject.activeSelf && shown.Length > 0;
            shown = richText;
            gameObject.SetActive(true);
            Prewarm();
            text.maxVisibleCharacters = 0;
            text.text = richText;
            Restart(Appear(wasVisible));
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            shown = "";
            Restart(Disappear());
        }

        // Lần đầu dùng hình minh hoạ, TMP mới dựng lớp vẽ ảnh và khung đầu tiên vẽ sai (ô vuông xanh).
        // Vẽ trước một lần tất cả hình (ẩn) để lớp đó có sẵn trước khi câu thật hiện ra.
        private bool warmed;

        private void Prewarm()
        {
            if (warmed) return;
            warmed = true;
            text.maxVisibleCharacters = 0;
            text.text = Pictograms.Format(":" + string.Join("::", Pictograms.Names) + ":");
            text.ForceMeshUpdate();
        }

        private void Restart(IEnumerator next)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(next);
        }

        private IEnumerator Appear(bool wasVisible)
        {
            text.maxVisibleCharacters = 0;
            text.ForceMeshUpdate();
            var total = text.textInfo.characterCount;
            var seconds = wasVisible ? bumpSeconds : popSeconds;
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                var t = time / seconds;
                var scale = wasVisible
                    ? 1f + 0.06f * Mathf.Sin(t * Mathf.PI)
                    : t < 0.65f ? Mathf.Lerp(0.4f, 1.08f, t / 0.65f) : Mathf.Lerp(1.08f, 1f, (t - 0.65f) / 0.35f);
                body.localScale = Vector3.one * scale;
                yield return null;
            }
            body.localScale = Vector3.one;
            for (var visible = 1; visible <= total; visible++)
            {
                text.maxVisibleCharacters = visible;
                var isPicture = text.textInfo.characterInfo[visible - 1].elementType == TMP_TextElementType.Sprite;
                yield return new WaitForSecondsRealtime(1f / charactersPerSecond + (isPicture ? pictureHoldSeconds : 0f));
            }
            text.maxVisibleCharacters = 99999;
            routine = null;
        }

        private IEnumerator Disappear()
        {
            var from = body.localScale.x;
            for (var time = 0f; time < hideSeconds; time += Time.unscaledDeltaTime)
            {
                body.localScale = Vector3.one * Mathf.Lerp(from, 0.5f, time / hideSeconds);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            body.localScale = Vector3.one;
            text.maxVisibleCharacters = 99999;
            routine = null;
        }
    }
}
