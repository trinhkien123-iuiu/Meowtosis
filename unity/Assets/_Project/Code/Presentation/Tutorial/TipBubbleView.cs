using System.Collections;
using TMPro;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Bong bóng lời hướng dẫn phía trên bàn (TipBubble):
    // - Xuất hiện lần đầu: cả bong bóng và mèo cùng nảy ra.
    // - Đổi câu khi các câu liền nhau: con mèo giữ nguyên vị trí và kích thước, chữ cũ thu nhỏ, nghỉ một nhịp, rồi nảy chữ mới ra kèm rung nhẹ khung chat.
    // - Ẩn: thu nhỏ rồi tắt. Chạy theo thời gian thật (unscaledDeltaTime).
    public sealed class TipBubbleView : MonoBehaviour
    {
        [SerializeField] private RectTransform body;
        [SerializeField] private TMP_Text text;
        [SerializeField] private float popSeconds = 0.24f;
        [SerializeField] private float shrinkSeconds = 0.1f;
        [SerializeField] private float restSeconds = 0.22f;
        [SerializeField] private float hideSeconds = 0.12f;

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
            Restart(Appear(richText, wasVisible));
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
            text.text = Pictograms.Format(":" + string.Join("::", Pictograms.Names) + ":");
            text.ForceMeshUpdate();
        }

        private void Restart(IEnumerator next)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(next);
        }

        private IEnumerator Appear(string richText, bool wasVisible)
        {
            if (wasVisible)
            {
                // Khi đổi câu liền nhau: giữ nguyên vị trí và kích thước con mèo, chỉ thu nhỏ phần chữ
                var textTransform = text.rectTransform;
                for (var time = 0f; time < shrinkSeconds; time += Time.unscaledDeltaTime)
                {
                    var t = time / shrinkSeconds;
                    textTransform.localScale = Vector3.one * (1f - t);
                    yield return null;
                }
                textTransform.localScale = Vector3.zero;

                // Nghỉ một nhịp ngắn cho người chơi kịp nhận thấy câu cũ kết thúc
                if (restSeconds > 0f)
                    yield return new WaitForSecondsRealtime(restSeconds);

                // Đổi nội dung chữ mới
                text.text = richText;
                text.maxVisibleCharacters = int.MaxValue;
                text.ForceMeshUpdate();

                // Nảy chữ mới bật ra kèm độ rung nhẹ của khung chat (mèo vẫn ở yên)
                for (var time = 0f; time < popSeconds; time += Time.unscaledDeltaTime)
                {
                    var t = time / popSeconds;
                    var textScale = t < 0.65f
                        ? Mathf.Lerp(0.2f, 1.12f, t / 0.65f)
                        : Mathf.Lerp(1.12f, 1f, (t - 0.65f) / 0.35f);
                    textTransform.localScale = Vector3.one * textScale;

                    var bump = 1f + 0.04f * Mathf.Sin(t * Mathf.PI);
                    body.localScale = Vector3.one * bump;
                    yield return null;
                }
                textTransform.localScale = Vector3.one;
                body.localScale = Vector3.one;
            }
            else
            {
                // Xuất hiện lần đầu: cả khung chat và mèo cùng nảy ra
                text.text = richText;
                text.maxVisibleCharacters = int.MaxValue;
                text.rectTransform.localScale = Vector3.one;
                text.ForceMeshUpdate();

                for (var time = 0f; time < popSeconds; time += Time.unscaledDeltaTime)
                {
                    var t = time / popSeconds;
                    var scale = t < 0.65f
                        ? Mathf.Lerp(0.35f, 1.08f, t / 0.65f)
                        : Mathf.Lerp(1.08f, 1f, (t - 0.65f) / 0.35f);
                    body.localScale = Vector3.one * scale;
                    yield return null;
                }
                body.localScale = Vector3.one;
            }

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
            routine = null;
        }

        private void OnDisable()
        {
            body.localScale = Vector3.one;
            text.rectTransform.localScale = Vector3.one;
            text.maxVisibleCharacters = int.MaxValue;
            routine = null;
        }
    }
}
