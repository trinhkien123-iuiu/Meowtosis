using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Đổi qua lại giữa ô của bàn và toạ độ (cục bộ của bàn). Mỗi ô rộng 1 đơn vị, tâm bàn ở gốc toạ độ.
    public sealed class BoardLayout
    {
        private readonly Level level;

        public BoardLayout(Level level)
        {
            this.level = level;
        }

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(level.Width, level.Height, 0f));

        public Vector3 CellCenter(int cell) => new Vector3(
            level.ColumnOf(cell) - (level.Width - 1) * 0.5f,
            (level.Height - 1) * 0.5f - level.RowOf(cell),
            0f);

        // Hàng tăng xuống dưới còn trục y của Unity tăng lên trên, nên đổi dấu hàng.
        public static Vector3 Offset(Direction direction) => new Vector3(direction.ColumnOffset(), -direction.RowOffset(), 0f);

        public bool TryGetCell(Vector2 point, out int cell)
        {
            var column = Mathf.RoundToInt(point.x + (level.Width - 1) * 0.5f);
            var row = Mathf.RoundToInt((level.Height - 1) * 0.5f - point.y);
            cell = level.Contains(row, column) ? level.IndexOf(row, column) : Level.NoCell;
            return cell != Level.NoCell && level.IsSlot(cell);
        }
    }
}
