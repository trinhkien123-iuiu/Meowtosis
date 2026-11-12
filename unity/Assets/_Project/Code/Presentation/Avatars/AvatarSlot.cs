using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Ô avatar của người chơi ở trang Home (thanh trên màn chơi luôn là mèo cam động, không đổi theo lựa chọn).
    // Avatar mặc định: bật các đối tượng có sẵn trong scene (mèo đỏ trong khung vàng).
    // Avatar đã chọn: tắt chúng và hiện hình avatar trong `picture`.
    public sealed class AvatarSlot : MonoBehaviour
    {
        [SerializeField] private AvatarCatalog catalog;
        [SerializeField] private Image picture;
        [Tooltip("Hiện khi dùng avatar mặc định, ẩn khi đã chọn avatar khác.")]
        [SerializeField] private GameObject[] defaultParts;
        [Tooltip("Ảnh nền của ô (nếu có) tắt đi khi hình avatar đã có khung riêng.")]
        [SerializeField] private Image slotBackground;
        [SerializeField] private Sprite clickGlowSprite;
        private Image clickGlow;
        private Vector3 feedbackRestScale;
        private bool feedbackPlaying;

        public void Show(string avatarId)
        {
            var sprite = catalog.PictureOf(avatarId);
            if (catalog.Frame != null)
            {
                foreach (var part in defaultParts) part.SetActive(false);
                if (slotBackground != null) slotBackground.enabled = false;
                picture.gameObject.SetActive(true);
                AvatarPortraitFrame.Show(picture, sprite ?? catalog.DefaultPicture, catalog.Frame, catalog.PortraitMask);
                return;
            }
            var isDefault = sprite == null;
            foreach (var part in defaultParts) part.SetActive(isDefault);
            if (slotBackground != null) slotBackground.enabled = isDefault;
            picture.gameObject.SetActive(!isDefault);
            if (!isDefault) picture.sprite = sprite;
        }

        public IEnumerator PlayClickFeedback()
        {
            feedbackRestScale = transform.localScale;
            feedbackPlaying = true;
            if (clickGlow == null && clickGlowSprite != null)
            {
                clickGlow = new GameObject("AvatarClickGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
                clickGlow.transform.SetParent(transform, false);
                clickGlow.transform.SetAsFirstSibling();
                clickGlow.sprite = clickGlowSprite;
                clickGlow.raycastTarget = false;
                clickGlow.rectTransform.sizeDelta = Vector2.one * 200f;
            }
            const float seconds = 0.18f;
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                var p = time / seconds;
                var scale = p < 0.25f ? Mathf.Lerp(1f, 0.9f, p / 0.25f)
                    : p < 0.65f ? Mathf.Lerp(0.9f, 1.08f, (p - 0.25f) / 0.4f)
                    : Mathf.Lerp(1.08f, 1f, (p - 0.65f) / 0.35f);
                transform.localScale = feedbackRestScale * scale;
                if (clickGlow != null)
                {
                    clickGlow.color = new Color(1f, 0.85f, 0.4f, Mathf.Sin(p * Mathf.PI) * 0.7f);
                    clickGlow.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.2f, p);
                }
                yield return null;
            }
            ResetClickFeedback();
        }

        public void ResetClickFeedback()
        {
            if (feedbackPlaying) transform.localScale = feedbackRestScale;
            feedbackPlaying = false;
            if (clickGlow != null) clickGlow.color = Color.clear;
        }

        private void OnDisable() => ResetClickFeedback();
    }
}
