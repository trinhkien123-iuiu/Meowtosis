using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Một avatar chọn được: id lưu trong tiến độ người chơi, tên hiện dưới ô, hình 256x256 có sẵn khung (Art/Avatars).
    [Serializable]
    public sealed class AvatarOption
    {
        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private Sprite picture;

        public string Id => id;

        public string Title => title;

        public Sprite Picture => picture;
    }

    // Danh sách avatar (Data/AvatarCatalog): mỗi chủ đề một chú mèo một màu, ảnh tạo bằng tools/make-avatars.py.
    // Id rỗng là avatar mặc định của game (mèo đỏ khung vàng ở Home).
    [CreateAssetMenu(fileName = "AvatarCatalog", menuName = "Meowtosis/Avatar Catalog")]
    public sealed class AvatarCatalog : ScriptableObject
    {
        public const string DefaultId = "";

        [Tooltip("Hình đại diện cho ô Mặc định trong bảng chọn.")]
        [SerializeField] private Sprite defaultPicture;
        [SerializeField] private List<AvatarOption> options = new List<AvatarOption>();

        public Sprite DefaultPicture => defaultPicture;

        public IReadOnlyList<AvatarOption> Options => options;

        // Hình của avatar `id`; null nếu là mặc định hoặc id không còn trong danh sách (khi đó hiện avatar mặc định).
        public Sprite PictureOf(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var option = options.Find(candidate => candidate.Id == id);
            return option?.Picture;
        }
    }
}
