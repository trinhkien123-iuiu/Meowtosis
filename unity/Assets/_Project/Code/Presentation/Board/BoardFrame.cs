using System;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Khung gỗ liền một khối của bàn (Prefabs/Board/BoardFrame), vẽ thành một ảnh theo hình dạng từng level: bóng đổ, viền sẫm,
    // gỗ đổ màu từ trên xuống, dải sáng bên trong, lòng khay. Ô khuyết giữa bàn thành hố có gờ nổi, góc lõm và lồi bo theo
    // từng bán kính. Mọi số đo tính bằng ô và lấy từ prototype (đo trên ô 90 điểm ảnh).
    public sealed class BoardFrame : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;

        [Header("Bóng đổ")]
        [SerializeField] private Color shadowColor = new Color(0.039f, 0.078f, 0.314f, 0.4f);
        [SerializeField] private float shadowOffset = 8f / 90f;

        [Header("Khung ngoài (viền sẫm và gỗ)")]
        [SerializeField] private FrameShape outer = new FrameShape(19f / 90f, 34f / 90f, 20f / 90f);
        [SerializeField] private float outlineWidth = 4f / 90f;
        [SerializeField] private Color outlineColor = new Color(0.490f, 0.263f, 0.133f, 1f);
        [SerializeField] private Color woodTopColor = new Color(0.953f, 0.725f, 0.561f, 1f);
        [SerializeField] private Color woodBottomColor = new Color(0.769f, 0.490f, 0.322f, 1f);

        [Header("Dải sáng")]
        [SerializeField] private FrameShape highlight = new FrameShape(15.5f / 90f, 30f / 90f, 17f / 90f);
        [SerializeField] private float highlightWidth = 2f / 90f;
        [SerializeField] private Color highlightColor = new Color(1f, 0.882f, 0.761f, 0.6f);

        [Header("Lòng khay")]
        [SerializeField] private FrameShape tray = new FrameShape(6f / 90f, 22f / 90f, 10f / 90f);
        [SerializeField] private float trayOutlineWidth = 2f / 90f;
        [SerializeField] private Color trayOutlineColor = new Color(0.576f, 0.322f, 0.180f, 1f);
        [SerializeField] private Color trayColor = new Color(0.706f, 0.443f, 0.290f, 1f);

        [Header("Độ phân giải")]
        [Tooltip("Cạnh dài nhất của ảnh khung, tính bằng điểm ảnh. Bàn nhỏ được nhiều điểm ảnh mỗi ô hơn nên vẫn nét khi phóng to.")]
        [SerializeField, Min(256)] private int largestImageSize = 1024;
        [Tooltip("Cạnh dài nhất của trường khoảng cách dùng để nới, co hình; thấp hơn ảnh thật cho nhanh, được nội suy lên.")]
        [SerializeField, Min(64)] private int largestFieldSize = 256;
        [Tooltip("Chừa quanh bàn bao nhiêu ô cho bóng và viền.")]
        [SerializeField] private float margin = 0.6f;

        private int pixelsPerCell;
        private int fieldPixelsPerCell;
        private Texture2D texture;
        private Sprite sprite;

        // Một đường viền: nới ra `grow` ô quanh các ô bàn, góc lồi bo `convexRadius`, góc lõm bo `concaveRadius`.
        [Serializable]
        public struct FrameShape
        {
            public float grow;
            public float convexRadius;
            public float concaveRadius;

            public FrameShape(float grow, float convexRadius, float concaveRadius)
            {
                this.grow = grow;
                this.convexRadius = convexRadius;
                this.concaveRadius = concaveRadius;
            }
        }

        public void Build(Level level)
        {
            ReleaseImage();
            var extent = new Vector2(level.Width + margin * 2f, level.Height + margin * 2f);
            var longestSide = Mathf.Max(extent.x, extent.y);
            pixelsPerCell = Mathf.FloorToInt(largestImageSize / longestSide);
            fieldPixelsPerCell = Mathf.FloorToInt(largestFieldSize / longestSide);
            var fieldSize = new Vector2Int(Mathf.CeilToInt(extent.x * fieldPixelsPerCell), Mathf.CeilToInt(extent.y * fieldPixelsPerCell));
            extent = new Vector2((float)fieldSize.x / fieldPixelsPerCell, (float)fieldSize.y / fieldPixelsPerCell);

            var distanceToOutside = DistanceToOutside(level, fieldSize, extent);
            var outlineField = BuildField(distanceToOutside, fieldSize, outer.grow + outlineWidth * 0.5f, outer.convexRadius, outer.concaveRadius);
            var woodField = BuildField(distanceToOutside, fieldSize, outer.grow - outlineWidth * 0.5f, outer.convexRadius, outer.concaveRadius);
            var highlightOuterField = BuildField(distanceToOutside, fieldSize, highlight.grow + highlightWidth * 0.5f, highlight.convexRadius, highlight.concaveRadius);
            var highlightInnerField = BuildField(distanceToOutside, fieldSize, highlight.grow - highlightWidth * 0.5f, highlight.convexRadius, highlight.concaveRadius);
            var trayOuterField = BuildField(distanceToOutside, fieldSize, tray.grow + trayOutlineWidth * 0.5f, tray.convexRadius, tray.concaveRadius);
            var trayInnerField = BuildField(distanceToOutside, fieldSize, tray.grow - trayOutlineWidth * 0.5f, tray.convexRadius, tray.concaveRadius);

            var imageSize = new Vector2Int(Mathf.RoundToInt(extent.x * pixelsPerCell), Mathf.RoundToInt(extent.y * pixelsPerCell));
            var pixels = new Color32[imageSize.x * imageSize.y];
            var woodHeight = level.Height + outer.grow * 2f;
            var columns = new GridPosition[imageSize.x];
            for (var column = 0; column < imageSize.x; column++)
                columns[column] = LocateOnGrid(((column + 0.5f) / imageSize.x - 0.5f) * extent.x, extent.x, fieldSize.x);
            for (var row = 0; row < imageSize.y; row++)
            {
                var worldY = ((row + 0.5f) / imageSize.y - 0.5f) * extent.y;
                var rowPosition = LocateOnGrid(worldY, extent.y, fieldSize.y);
                var shadowRowPosition = LocateOnGrid(worldY + shadowOffset, extent.y, fieldSize.y);
                var gradient = Mathf.Clamp01((level.Height * 0.5f + outer.grow - worldY) / woodHeight);
                var woodColor = Color.Lerp(woodTopColor, woodBottomColor, gradient);
                for (var column = 0; column < imageSize.x; column++)
                {
                    var columnPosition = columns[column];
                    // Phần lớn ảnh là lòng khay kín hoặc khoảng trống: tính nhanh, không cần chồng từng lớp.
                    if (Coverage(Sample(trayInnerField, fieldSize.x, columnPosition, rowPosition)) >= 1f)
                    {
                        pixels[row * imageSize.x + column] = trayColor;
                        continue;
                    }
                    var outline = Coverage(Sample(outlineField, fieldSize.x, columnPosition, rowPosition));
                    var shadow = Coverage(Sample(outlineField, fieldSize.x, columnPosition, shadowRowPosition));
                    if (outline <= 0f && shadow <= 0f) continue;

                    var color = new Color(0f, 0f, 0f, 0f);
                    color = Over(color, shadowColor, shadow);
                    color = Over(color, outlineColor, outline);
                    color = Over(color, woodColor, Coverage(Sample(woodField, fieldSize.x, columnPosition, rowPosition)));
                    var ring = Coverage(Sample(highlightOuterField, fieldSize.x, columnPosition, rowPosition))
                        * (1f - Coverage(Sample(highlightInnerField, fieldSize.x, columnPosition, rowPosition)));
                    color = Over(color, highlightColor, ring);
                    color = Over(color, trayOutlineColor, Coverage(Sample(trayOuterField, fieldSize.x, columnPosition, rowPosition)));
                    color = Over(color, trayColor, Coverage(Sample(trayInnerField, fieldSize.x, columnPosition, rowPosition)));
                    pixels[row * imageSize.x + column] = color;
                }
            }

            texture = new Texture2D(imageSize.x, imageSize.y, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, imageSize.x, imageSize.y), new Vector2(0.5f, 0.5f), imageSize.x / extent.x);
            target.sprite = sprite;
        }

        private void OnDestroy() => ReleaseImage();

        private void ReleaseImage()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
            sprite = null;
            texture = null;
        }

        // Khoảng cách (điểm ảnh của trường) từ mỗi điểm tới phần ngoài các ô bàn: ô khuyết và vùng ngoài bàn.
        private float[] DistanceToOutside(Level level, Vector2Int fieldSize, Vector2 extent)
        {
            var isOutside = new bool[fieldSize.x * fieldSize.y];
            for (var row = 0; row < fieldSize.y; row++)
            {
                var worldY = ((row + 0.5f) / fieldSize.y - 0.5f) * extent.y;
                var boardRow = Mathf.FloorToInt(level.Height * 0.5f - worldY);
                for (var column = 0; column < fieldSize.x; column++)
                {
                    var worldX = ((column + 0.5f) / fieldSize.x - 0.5f) * extent.x;
                    var boardColumn = Mathf.FloorToInt(worldX + level.Width * 0.5f);
                    isOutside[row * fieldSize.x + column] = !(level.Contains(boardRow, boardColumn) && level.IsSlot(level.IndexOf(boardRow, boardColumn)));
                }
            }
            return DistanceTransform.ToSeeds(isOutside, fieldSize.x, fieldSize.y);
        }

        // Co các ô bàn vào (bán kính lồi trừ phần nới) rồi nới ra bán kính lồi: góc lồi thành cung tròn, cạnh dịch đúng phần nới.
        // Sau đó nới và co bán kính lõm để bo góc lõm. Trường trả về có dấu dương ở trong, đơn vị ô.
        private float[] BuildField(float[] distanceToOutside, Vector2Int fieldSize, float grow, float convexRadius, float concaveRadius)
        {
            var pixelsPerUnit = (float)fieldPixelsPerCell;
            var erodeBy = (convexRadius - grow) * pixelsPerUnit;
            var count = distanceToOutside.Length;

            var eroded = new bool[count];
            for (var index = 0; index < count; index++) eroded[index] = distanceToOutside[index] >= erodeBy;

            var distanceToEroded = DistanceTransform.ToSeeds(eroded, fieldSize.x, fieldSize.y);
            var dilated = new bool[count];
            for (var index = 0; index < count; index++) dilated[index] = distanceToEroded[index] <= convexRadius * pixelsPerUnit;

            var distanceToDilated = DistanceTransform.ToSeeds(dilated, fieldSize.x, fieldSize.y);
            var isGap = new bool[count];
            for (var index = 0; index < count; index++) isGap[index] = distanceToDilated[index] > concaveRadius * pixelsPerUnit;
            // Rìa ảnh cũng là khoảng trống, nếu không bước co lại không có gì để co vào và viền phình ra.
            for (var column = 0; column < fieldSize.x; column++) isGap[column] = isGap[(fieldSize.y - 1) * fieldSize.x + column] = true;
            for (var row = 0; row < fieldSize.y; row++) isGap[row * fieldSize.x] = isGap[row * fieldSize.x + fieldSize.x - 1] = true;

            var distanceToGap = DistanceTransform.ToSeeds(isGap, fieldSize.x, fieldSize.y);
            var signed = new float[count];
            for (var index = 0; index < count; index++) signed[index] = (distanceToGap[index] - 0.5f) / pixelsPerUnit - concaveRadius;
            return signed;
        }

        // Vị trí trên lưới của trường: ô lưới bên dưới/trái và phần lẻ để nội suy. Tính một lần cho mỗi cột, mỗi hàng.
        private struct GridPosition
        {
            public int First;
            public float Fraction;
        }

        private static GridPosition LocateOnGrid(float world, float extent, int gridSize)
        {
            var grid = Mathf.Clamp((world / extent + 0.5f) * gridSize - 0.5f, 0f, gridSize - 1f);
            var first = Mathf.Min((int)grid, gridSize - 2);
            return new GridPosition { First = first, Fraction = grid - first };
        }

        // Trường khoảng cách có dấu (dương ở trong hình, tính bằng ô): giá trị tại một điểm, nội suy hai chiều giữa các điểm lưới.
        private static float Sample(float[] field, int gridWidth, GridPosition along, GridPosition across)
        {
            var start = across.First * gridWidth + along.First;
            var low = Mathf.Lerp(field[start], field[start + 1], along.Fraction);
            var high = Mathf.Lerp(field[start + gridWidth], field[start + gridWidth + 1], along.Fraction);
            return Mathf.Lerp(low, high, across.Fraction);
        }

        // Độ phủ của một hình tại điểm có khoảng cách có dấu `distance` (ô): làm mịn răng cưa trong khoảng một điểm ảnh.
        private float Coverage(float distance) => Mathf.Clamp01(distance * pixelsPerCell + 0.5f);

        // Chồng màu `top` (độ phủ `coverage`) lên màu `below`, cả hai có độ trong suốt riêng.
        private static Color Over(Color below, Color top, float coverage)
        {
            var topAlpha = top.a * coverage;
            var alpha = topAlpha + below.a * (1f - topAlpha);
            if (alpha <= 0f) return new Color(0f, 0f, 0f, 0f);
            var weightBelow = below.a * (1f - topAlpha);
            return new Color(
                (top.r * topAlpha + below.r * weightBelow) / alpha,
                (top.g * topAlpha + below.g * weightBelow) / alpha,
                (top.b * topAlpha + below.b * weightBelow) / alpha,
                alpha);
        }
    }
}
