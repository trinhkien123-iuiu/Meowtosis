using System;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Hình đổi theo loài: mèo từng màu (thường, vui, các vẻ mặt), hộp từng màu, vi khuẩn, virus bốn hướng, màu hạt hiệu ứng.
    // Hình cố định của bàn (ô, khung, màng...) nằm trong các prefab ở Prefabs/Board.
    [CreateAssetMenu(fileName = "BoardArt", menuName = "Meowtosis/Board Art")]
    public sealed class BoardArt : ScriptableObject
    {
        [SerializeField] private Sprite germ;
        [SerializeField] private Color germColor = new Color32(0x84, 0xdb, 0x4c, 0xff);
        [SerializeField] private Sprite virusUp;
        [SerializeField] private Sprite virusDown;
        [SerializeField] private Sprite virusLeft;
        [SerializeField] private Sprite virusRight;
        [SerializeField] private Color virusColor = new Color32(0xb5, 0x6c, 0xff, 0xff);
        [SerializeField] private CatArt[] cats = Array.Empty<CatArt>();

        public Sprite Germ => germ;

        public Sprite VirusSprite(Direction facing) => facing switch
        {
            Direction.Up => virusUp,
            Direction.Down => virusDown,
            Direction.Left => virusLeft,
            _ => virusRight,
        };

        // Mèo thường, hoặc mèo vui (mắt sao) khi đứng đúng hộp.
        public Sprite CatSprite(Species species, bool isHappy)
        {
            var art = Find(species);
            if (art == null) return null;
            return isHappy && art.HappyCat != null ? art.HappyCat : art.Cat;
        }

        public Sprite BoxSprite(Species species) => Find(species)?.Box;

        public Sprite FaceSprite(Species species, FaceKind kind)
        {
            var art = Find(species);
            if (art == null) return null;
            return kind switch
            {
                FaceKind.Blink => art.BlinkFace,
                FaceKind.Sad => art.SadFace,
                FaceKind.Yawn => art.YawnFace,
                _ => null,
            };
        }

        // Màu hạt hiệu ứng khi quân này vỡ.
        public Color ColorOf(Species species)
        {
            if (species == Species.Germ) return germColor;
            if (species == Species.Virus) return virusColor;
            var art = Find(species);
            return art != null ? art.Color : Color.white;
        }

        private CatArt Find(Species species) => Array.Find(cats, art => art.Species == species);

        [Serializable]
        public sealed class CatArt
        {
            [SerializeField] private Species species;
            [SerializeField] private Sprite cat;
            [SerializeField] private Sprite happyCat;
            [SerializeField] private Sprite box;
            [SerializeField] private Sprite blinkFace;
            [SerializeField] private Sprite sadFace;
            [SerializeField] private Sprite yawnFace;
            [SerializeField] private Color color = Color.white;

            public Species Species => species;

            public Sprite Cat => cat;

            public Sprite HappyCat => happyCat;

            public Sprite Box => box;

            public Sprite BlinkFace => blinkFace;

            public Sprite SadFace => sadFace;

            public Sprite YawnFace => yawnFace;

            public Color Color => color;
        }
    }
}
