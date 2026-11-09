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

        public void Show(string avatarId)
        {
            var sprite = catalog.PictureOf(avatarId);
            var isDefault = sprite == null;
            foreach (var part in defaultParts) part.SetActive(isDefault);
            if (slotBackground != null) slotBackground.enabled = isDefault;
            picture.gameObject.SetActive(!isDefault);
            if (!isDefault) picture.sprite = sprite;
        }
    }
}
