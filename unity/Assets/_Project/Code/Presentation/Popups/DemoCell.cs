using Meowtosis.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Một ô của bàn minh hoạ trong thẻ cơ chế (Prefabs/Hud/DemoCell): ô gạch hoặc ô độc, hộp, quân.
    public sealed class DemoCell : MonoBehaviour
    {
        [SerializeField] private GameObject tile;
        [SerializeField] private GameObject toxicTile;
        [SerializeField] private Image box;
        [SerializeField] private Image piece;
        [SerializeField] private GameObject sleepMarks;
        [Tooltip("Bề rộng ảnh mèo so với ô.")]
        [SerializeField] private float catWidth = 0.778f;
        [Tooltip("Bề rộng hình virus so với ô (vẽ trên khung 1,5 ô).")]
        [SerializeField] private float placeholderWidth = 1.5f;

        public void ShowCell(Level level, int cell, BoardArt art)
        {
            var isSlot = level.IsSlot(cell);
            tile.SetActive(isSlot && !level.IsToxic(cell));
            toxicTile.SetActive(isSlot && level.IsToxic(cell));
            var target = level.TargetAt(cell);
            box.gameObject.SetActive(target.HasValue);
            if (target.HasValue) box.sprite = art.BoxSprite(target.Value);
        }

        public void ShowPiece(Piece? shown, BoardArt art, float cellSize, bool isHappy)
        {
            piece.gameObject.SetActive(shown.HasValue);
            sleepMarks.SetActive(shown.HasValue && shown.Value.IsAsleep);
            if (!shown.HasValue) return;

            var value = shown.Value;
            if (value.IsVirus) piece.sprite = art.VirusSprite(value.Facing);
            else piece.sprite = art.CatSprite(value.Species, isHappy);
            var width = cellSize * (value.IsCat ? catWidth : placeholderWidth);
            piece.rectTransform.sizeDelta = new Vector2(width, width * piece.sprite.rect.height / piece.sprite.rect.width);
        }
    }
}
