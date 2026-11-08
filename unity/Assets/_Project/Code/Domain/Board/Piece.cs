namespace Meowtosis.Domain
{
    // Một con mèo hoặc virus đứng trong một ô. Id đi theo quân khi nó di chuyển, để giao diện bám theo được.
    public readonly struct Piece
    {
        public Piece(int id, Species species, bool isAsleep = false, Direction facing = Direction.Up,
            Species? otherColor = null, int period = 0, int turnsLeft = 0)
        {
            Id = id;
            Species = species;
            IsAsleep = isAsleep;
            Facing = facing;
            OtherColor = otherColor;
            Period = period;
            TurnsLeft = turnsLeft;
        }

        public int Id { get; }

        public Species Species { get; }

        public bool IsAsleep { get; }

        // Hướng virus lan. Loài khác bỏ qua.
        public Direction Facing { get; }

        // Mèo hai màu: màu kia (sẽ đổi sang), chu kỳ đổi (số lượt) và số lượt còn lại trước lần đổi tới. Mèo thường: null và 0.
        public Species? OtherColor { get; }

        public int Period { get; }

        public int TurnsLeft { get; }

        public bool IsTwoColor => OtherColor.HasValue;

        public bool IsVirus => Species == Species.Virus;

        public bool IsCat => Species != Species.Virus;

        public bool CanSwipe => !IsAsleep && !IsVirus;

        public Piece WakeUp() => new Piece(Id, Species, false, Facing, OtherColor, Period, TurnsLeft);

        // Đếm lùi một lượt (mèo hai màu đang thức); về 0 thì đổi sang màu kia và đếm lại từ chu kỳ. Quân khác giữ nguyên.
        public Piece CountDown()
        {
            if (!IsTwoColor || IsAsleep) return this;
            if (TurnsLeft > 1) return new Piece(Id, Species, IsAsleep, Facing, OtherColor, Period, TurnsLeft - 1);
            return new Piece(Id, OtherColor.Value, IsAsleep, Facing, Species, Period, Period);
        }
    }
}
