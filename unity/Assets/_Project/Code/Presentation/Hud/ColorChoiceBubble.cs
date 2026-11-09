using System;
using System.Collections.Generic;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Bong bóng thoại chọn màu của Hoán đổi (SafeArea/Layout/ColorBubble): oval có đuôi cong (hình Art/UI/ChoiceBubble),
    // đầu đuôi chỉ ngay vào con mèo vừa chạm, trong bong bóng là các chú mèo màu khác trên đĩa to cho dễ bấm.
    // Không bao giờ tràn ra ngoài khung màn hình: hình gốc có đuôi ở góc dưới trái, bên nào còn nhiều chỗ hơn thì bong bóng
    // lật sang bên đó (phải/trái, trên/dưới mèo), và nếu bên tốt nhất vẫn chưa đủ chỗ thì thu nhỏ vừa khít.
    // Gốc (pivot) của hình đặt ở đầu đuôi nên bong bóng nở ra từ chính con mèo.
    public sealed class ColorChoiceBubble : MonoBehaviour
    {
        [Tooltip("Hình bong bóng, pivot đặt đúng đầu đuôi.")]
        [SerializeField] private RectTransform shape;
        [SerializeField] private RectTransform choiceRoot;
        [SerializeField] private SpeciesChip choicePrefab;
        [SerializeField] private BoardArt art;
        [Tooltip("Từ đầu đuôi tới tâm phần oval, khi chưa lật và chưa thu nhỏ (đơn vị canvas).")]
        [SerializeField] private Vector2 ovalCenter = new Vector2(285f, 290f);
        [Tooltip("Đầu đuôi đặt cao hơn tâm con mèo chừng này (ngang đỉnh đầu mèo).")]
        [SerializeField] private float tipAboveCat = 70f;
        [Tooltip("Khoảng cách ngang giữa hai lựa chọn.")]
        [SerializeField] private float pairSpacing = 175f;
        [Tooltip("Khoảng cách dọc giữa hai hàng (khi có từ 4 màu).")]
        [SerializeField] private float rowSpacing = 200f;
        [Tooltip("Khoảng chừa lại giữa bong bóng và mép màn hình.")]
        [SerializeField] private float edgeMargin = 24f;
        [Tooltip("Thu nhỏ tối đa khi chật chỗ; nhỏ hơn nữa thì chấp nhận tràn một chút còn hơn khó bấm.")]
        [SerializeField, Range(0.4f, 1f)] private float minimumScale = 0.6f;
        [SerializeField] private float popSeconds = 0.28f;
        [SerializeField] private float choiceStagger = 0.06f;

        private readonly List<SpeciesChip> choices = new List<SpeciesChip>();
        private Vector2 flip = Vector2.one;
        private float fit = 1f;
        private float age;

        public event Action<Species> Chosen;

        // Mở bong bóng chỉ vào điểm `screenPoint` (tâm con mèo trên màn hình).
        public void Show(IReadOnlyList<Species> colors, Vector2 screenPoint)
        {
            if (colors.Count == 0) { Hide(); return; }
            gameObject.SetActive(true);
            BuildChoices(colors);
            Place(screenPoint);
            age = 0f;
            Animate();
        }

        public void Hide() => gameObject.SetActive(false);

        private void BuildChoices(IReadOnlyList<Species> colors)
        {
            while (choices.Count < colors.Count)
            {
                var chip = Instantiate(choicePrefab, choiceRoot);
                chip.Clicked += color => Chosen?.Invoke(color);
                choices.Add(chip);
            }
            for (var index = 0; index < choices.Count; index++)
            {
                var active = index < colors.Count;
                choices[index].gameObject.SetActive(active);
                if (!active) continue;
                choices[index].Show(colors[index], art.CatSprite(colors[index], false), GameTexts.ColorName(colors[index]));
                ((RectTransform)choices[index].transform).anchoredPosition = SlotOf(index, colors.Count);
            }
        }

        // Tới 3 màu: một hàng ngang; 4 màu: lưới 2x2; nhiều hơn: tối đa 3 đĩa mỗi hàng. Mỗi hàng được căn giữa.
        private Vector2 SlotOf(int index, int count)
        {
            var perRow = count <= 3 ? count : count == 4 ? 2 : 3;
            var rows = (count + perRow - 1) / perRow;
            var row = index / perRow;
            var inRow = Mathf.Min(perRow, count - row * perRow);
            var column = index - row * perRow;
            return new Vector2((column - (inRow - 1) * 0.5f) * pairSpacing, ((rows - 1) * 0.5f - row) * rowSpacing);
        }

        private void Place(Vector2 screenPoint)
        {
            var area = (RectTransform)transform.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, null, out var cat);
            var rect = area.rect;
            var size = shape.rect.size;
            // Quãng đường từ đầu đuôi tới mép xa của hình, theo từng trục (đuôi nằm ở góc dưới trái nên hình mọc sang phải và lên trên).
            var reachX = size.x * (1f - shape.pivot.x);
            var reachY = size.y * (1f - shape.pivot.y);

            var spaceRight = rect.xMax - edgeMargin - cat.x;
            var spaceLeft = cat.x - (rect.xMin + edgeMargin);
            flip.x = spaceLeft > spaceRight ? -1f : 1f;
            var spaceX = Mathf.Max(spaceLeft, spaceRight);

            var tipY = cat.y + tipAboveCat;
            var spaceUp = rect.yMax - edgeMargin - tipY;
            var spaceDown = cat.y - tipAboveCat - (rect.yMin + edgeMargin);
            // Ưu tiên phía trên mèo; chỉ lật xuống khi phía trên chật mà phía dưới rộng hơn.
            flip.y = spaceUp < reachY * minimumScale && spaceDown > spaceUp ? -1f : 1f;
            var spaceY = flip.y > 0f ? spaceUp : spaceDown;

            fit = Mathf.Clamp(Mathf.Min(1f, spaceX / reachX, spaceY / reachY), minimumScale, 1f);
            ((RectTransform)transform).anchoredPosition = cat + new Vector2(0f, tipAboveCat * flip.y);
        }

        private void Update()
        {
            if (age >= popSeconds * 1.5f + choiceStagger * choices.Count) return;
            age += Time.unscaledDeltaTime;
            Animate();
        }

        // Bong bóng nở ra từ đầu đuôi; các chú mèo bật lên lần lượt.
        private void Animate()
        {
            var grow = Pop(age / popSeconds) * fit;
            shape.localScale = new Vector3(flip.x * grow, flip.y * grow, 1f);
            choiceRoot.localScale = Vector3.one * grow;
            choiceRoot.anchoredPosition = Vector2.Scale(ovalCenter, flip) * grow;
            for (var index = 0; index < choices.Count; index++)
                choices[index].transform.localScale = Vector3.one * Pop((age - popSeconds * 0.5f - index * choiceStagger) / popSeconds);
        }

        private static float Pop(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.7f ? Mathf.Lerp(0f, 1.1f, t / 0.7f) : Mathf.Lerp(1.1f, 1f, (t - 0.7f) / 0.3f);
        }
    }
}
