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
        [Tooltip("Mèo hai màu: bong bóng ở góc trên phải ô, tô màu kế tiếp; bên trong có mèo màu đó (twoToneHead).")]
        [SerializeField] private Image twoToneBubble;
        [SerializeField] private Image twoToneHead;
        [Tooltip("Các chấm đếm lượt, từ trái sang phải.")]
        [SerializeField] private Image[] pips;
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
            HideTwoTone();
            if (!shown.HasValue) return;

            var value = shown.Value;
            if (value.IsVirus) piece.sprite = art.VirusSprite(value.Facing);
            else piece.sprite = art.CatSprite(value.Species, isHappy);
            var width = cellSize * (value.IsCat ? catWidth : placeholderWidth);
            piece.rectTransform.sizeDelta = new Vector2(width, width * piece.sprite.rect.height / piece.sprite.rect.width);
            if (value.IsTwoColor) ShowTwoTone(value, art, cellSize);
        }

        // Nửa phải thân bằng màu kế tiếp, kèm chấm đếm lượt ở mép dưới ô.
        private void ShowTwoTone(Piece value, BoardArt art, float cellSize)
        {
            if (twoToneBubble == null) return;
            var other = value.OtherColor.Value;
            var bubbleSize = cellSize * 0.46f;
            var bubbleCenter = new Vector2(cellSize * 0.34f, cellSize * 0.35f);
            twoToneBubble.gameObject.SetActive(true);
            twoToneBubble.color = Color.Lerp(art.ColorOf(other), Color.white, 0.82f);
            twoToneBubble.rectTransform.anchoredPosition = bubbleCenter;
            twoToneBubble.rectTransform.sizeDelta = new Vector2(bubbleSize, bubbleSize);
            twoToneHead.sprite = art.CatSprite(other, false);
            var headWidth = bubbleSize * 0.8f;
            twoToneHead.rectTransform.sizeDelta = new Vector2(headWidth, headWidth * twoToneHead.sprite.rect.height / twoToneHead.sprite.rect.width);
            var pipColor = art.ColorOf(other);
            for (var index = 0; index < pips.Length; index++)
            {
                pips[index].gameObject.SetActive(index < value.TurnsLeft);
                pips[index].color = pipColor;
                pips[index].rectTransform.sizeDelta = new Vector2(cellSize * 0.13f, cellSize * 0.13f);
                var offset = (index - (Mathf.Min(value.TurnsLeft, pips.Length) - 1) * 0.5f) * cellSize * 0.17f;
                pips[index].rectTransform.anchoredPosition = bubbleCenter + new Vector2(offset, -bubbleSize * 0.5f - cellSize * 0.06f);
            }
        }

        private void HideTwoTone()
        {
            if (twoToneBubble != null) twoToneBubble.gameObject.SetActive(false);
            if (pips == null) return;
            foreach (var pip in pips) pip.gameObject.SetActive(false);
        }
    }
}
