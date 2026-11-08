using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Mèo hai màu (con của Visual trong prefab Piece): thân giữ nguyên một màu, góc trên phải ô có bong bóng nhỏ chứa mèo màu kế tiếp,
    // vài chấm dưới bong bóng là số nước còn lại trước khi đổi (còn 1 thì bong bóng và chấm nhấp nháy).
    // Lúc đổi màu, bong bóng phồng lên rồi co lại.
    public sealed class TwoToneCat : MonoBehaviour
    {
        [Tooltip("Nền tròn của bong bóng, tô màu kế tiếp.")]
        [SerializeField] private SpriteRenderer bubble;
        [Tooltip("Mèo màu kế tiếp, nằm trong bong bóng.")]
        [SerializeField] private SpriteRenderer head;
        [Tooltip("Các chấm đếm lượt, xếp từ trái sang phải; hiện đủ số lượt còn lại.")]
        [SerializeField] private SpriteRenderer[] pips;

        [Header("Bong bóng")]
        [SerializeField] private Vector2 bubbleOffset = new Vector2(0.34f, 0.35f);
        [SerializeField] private float bubbleSize = 0.46f;
        [Tooltip("Bề rộng mèo trong bong bóng so với bong bóng.")]
        [SerializeField] private float headWidth = 0.8f;
        [SerializeField] private float popScale = 1.6f;
        [SerializeField] private float popSeconds = 0.35f;
        [SerializeField] private float urgentPulseSeconds = 0.5f;

        [Header("Chấm đếm lượt")]
        [SerializeField] private float pipSize = 0.13f;
        [SerializeField] private float pipSpacing = 0.17f;
        [SerializeField] private float pipGap = 0.06f;

        private Species? shownSpecies;
        private int turnsLeft;
        private float pop = 1f;
        private float phase;

        public void Show(Piece piece, BoardArt art, float catWidth, float catLift)
        {
            gameObject.SetActive(piece.IsTwoColor);
            if (!piece.IsTwoColor)
            {
                shownSpecies = null;
                turnsLeft = 0;
                foreach (var pip in pips) pip.gameObject.SetActive(false);   // chấm nằm ngoài nhóm này nên phải tắt riêng
                return;
            }

            var other = piece.OtherColor.Value;
            bubble.color = Color.Lerp(art.ColorOf(other), Color.white, 0.82f);
            transform.localScale = Vector3.one;
            SpriteSizing.SetSprite(head, art.CatSprite(other, false), bubbleSize * headWidth);
            transform.localPosition = new Vector3(bubbleOffset.x, bubbleOffset.y + catLift * 0.5f, -0.02f);
            bubble.transform.localScale = Vector3.one * bubbleSize;
            head.transform.localPosition = new Vector3(0f, -0.01f, -0.01f);

            var pipColor = art.ColorOf(other);
            turnsLeft = piece.TurnsLeft;
            phase = piece.Id * 0.41f;
            var rowY = bubbleOffset.y + catLift * 0.5f - bubbleSize * 0.5f - pipGap;
            for (var index = 0; index < pips.Length; index++)
            {
                pips[index].gameObject.SetActive(index < turnsLeft);
                pips[index].color = pipColor;
                pips[index].transform.localScale = Vector3.one * pipSize;
                var offset = (index - (Mathf.Min(turnsLeft, pips.Length) - 1) * 0.5f) * pipSpacing;
                pips[index].transform.localPosition = new Vector3(bubbleOffset.x + offset, rowY, 0f);
            }

            // Màu hiện tại vừa đổi: bong bóng phồng lên cho người chơi để ý.
            if (shownSpecies.HasValue && shownSpecies.Value != piece.Species) pop = 0f;
            shownSpecies = piece.Species;
        }

        private void Update()
        {
            var scale = 1f;
            if (pop < 1f)
            {
                pop = Mathf.Min(1f, pop + Time.deltaTime / popSeconds);
                scale = Mathf.Lerp(popScale, 1f, 1f - (1f - pop) * (1f - pop));
            }
            if (turnsLeft == 1)
            {
                // Còn 1 nước: nhấp nháy để báo sắp đổi.
                var wave = 0.5f + 0.5f * Mathf.Sin((Time.time + phase) / urgentPulseSeconds * Mathf.PI * 2f);
                scale *= 1f + 0.18f * wave;
                foreach (var pip in pips) pip.transform.localScale = Vector3.one * (pipSize * (1f + 0.45f * wave));
            }
            transform.localScale = Vector3.one * scale;
        }
    }
}
