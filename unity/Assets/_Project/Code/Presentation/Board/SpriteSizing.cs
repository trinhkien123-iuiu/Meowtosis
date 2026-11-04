using UnityEngine;

namespace Meowtosis.Presentation
{
    // Đặt cỡ hiển thị cho sprite theo đơn vị ô, không phụ thuộc cỡ ảnh: đổi sang ảnh khác cỡ vẫn vừa ô.
    internal static class SpriteSizing
    {
        public static void SetSprite(SpriteRenderer renderer, Sprite sprite, float width)
        {
            renderer.sprite = sprite;
            if (sprite == null) return;
            var scale = width / sprite.bounds.size.x;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
