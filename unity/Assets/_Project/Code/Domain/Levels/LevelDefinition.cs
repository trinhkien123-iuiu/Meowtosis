using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Một level đúng như viết trong danh sách level, trước khi thành Level chơi được.
    public sealed class LevelDefinition
    {
        // Id cố định để lưu tiến độ. Không đổi khi sắp xếp lại level.
        public string Id { get; set; } = "";

        // Khối cơ chế chứa level (nhóm nội bộ, bản đồ không hiện).
        public int Block { get; set; }

        // Số nước thắng ít nhất (bộ giải tính). Bằng par thì được 3 sao.
        public int Par { get; set; }

        // Bàn chơi, mỗi hàng một chuỗi, dùng ký hiệu của LevelSymbols.
        public List<string> Rows { get; } = new List<string>();

        // Hộp mục tiêu: cùng cỡ với Rows, chữ cái là hộp của màu đó, '.' là không có hộp. Rỗng nếu level không có hộp.
        public List<string> Goal { get; } = new List<string>();

        public List<Membrane> Membranes { get; } = new List<Membrane>();

        // Các cặp cổng dịch chuyển nối hai phía của bàn.
        public List<Portal> Portals { get; } = new List<Portal>();

        // Các mèo hai màu lúc bắt đầu (màu hiện tại lấy từ Rows).
        public List<ColorSwitch> ColorSwitches { get; } = new List<ColorSwitch>();

        public List<TutorialStep> Tutorial { get; } = new List<TutorialStep>();

        // Tên thẻ cơ chế riêng của level (thử thách mới không tự nhận ra được từ bàn chơi); rỗng nếu không có.
        public string Card { get; set; } = "";

        // Nước cố ý dẫn tới thua để người chơi tự thấy luật khử; null nếu level không có.
        public TutorialStep Trap { get; set; }

        // Một câu mẹo hiện đầu màn; rỗng nếu không có.
        public string Tip { get; set; } = "";

        // Bảng màu riêng của level (thứ tự tương ứng với A, B, C, D, E...); nếu có thì thay thế các màu mặc định.
        public List<Species> Palette { get; } = new List<Species>();

        // Một lời giải ngắn nhất, dài đúng Par nước.
        public List<Move> Solution { get; } = new List<Move>();
    }
}
