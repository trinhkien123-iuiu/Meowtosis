namespace Meowtosis.Domain
{
    // Cấu hình mèo hai màu ở một ô lúc bắt đầu level: màu hiện tại lấy từ bàn, đây là màu kia (có thể chưa có con nào trên bàn),
    // chu kỳ đổi màu (số lượt) và số lượt còn lại trước lần đổi đầu tiên.
    public readonly struct ColorSwitch
    {
        public ColorSwitch(int row, int column, Species otherColor, int period, int turnsLeft)
        {
            Row = row;
            Column = column;
            OtherColor = otherColor;
            Period = period;
            TurnsLeft = turnsLeft;
        }

        public int Row { get; }

        public int Column { get; }

        public Species OtherColor { get; }

        public int Period { get; }

        public int TurnsLeft { get; }
    }
}
