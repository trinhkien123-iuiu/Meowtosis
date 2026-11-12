using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // UI-only framing: original portrait assets and saved avatar IDs remain unchanged.
    public sealed class AvatarPortraitFrame : MonoBehaviour
    {
        private Image portrait;
        private Image frame;

        public static void Show(Image target, Sprite artwork, Sprite border, Sprite maskSprite)
        {
            if (border == null || maskSprite == null)
            {
                target.sprite = artwork;
                return;
            }
            var view = target.GetComponent<AvatarPortraitFrame>();
            if (view == null) view = target.gameObject.AddComponent<AvatarPortraitFrame>();
            view.Apply(target, artwork, border, maskSprite);
        }

        private void Apply(Image target, Sprite artwork, Sprite border, Sprite maskSprite)
        {
            var rect = target.rectTransform;
            if (portrait == null)
            {
                var mask = target.GetComponent<Mask>();
                if (mask == null) mask = target.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;
                portrait = NewImage("OriginalAvatarPortrait", rect);
                portrait.rectTransform.anchorMin = Vector2.zero;
                portrait.rectTransform.anchorMax = Vector2.one;
                // Hide the old baked frame using a UI crop, without changing the image.
                var inset = Mathf.Min(rect.rect.width, rect.rect.height) * 0.14f;
                portrait.rectTransform.offsetMin = Vector2.one * -inset;
                portrait.rectTransform.offsetMax = Vector2.one * inset;
                frame = NewImage("CatdomAvatarFrame", rect.parent);
                frame.transform.SetSiblingIndex(rect.GetSiblingIndex() + 1);
            }
            target.sprite = maskSprite;
            target.color = Color.white;
            target.preserveAspect = false;
            target.raycastTarget = false;
            portrait.sprite = artwork;
            frame.sprite = border;
            var frameRect = frame.rectTransform;
            frameRect.anchorMin = rect.anchorMin;
            frameRect.anchorMax = rect.anchorMax;
            frameRect.pivot = rect.pivot;
            frameRect.anchoredPosition = rect.anchoredPosition;
            frameRect.sizeDelta = rect.sizeDelta + Vector2.one * (rect.rect.width * 0.08f);
            frameRect.localRotation = rect.localRotation;
            frameRect.localScale = rect.localScale;
        }

        private static Image NewImage(string name, Transform parent)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private void OnDestroy()
        {
            if (frame != null) Destroy(frame.gameObject);
        }
    }
}
