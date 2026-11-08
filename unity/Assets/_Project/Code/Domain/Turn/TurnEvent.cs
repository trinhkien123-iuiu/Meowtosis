namespace Meowtosis.Domain
{
    public enum TurnEventKind
    {
        Poisoned,       // quân đứng trên ô độc và chết
        Cleared,        // quân nằm trong dãy từ 3 trở lên và bị khử
        Eaten,          // virus lan vào ô và ăn quân ở đó
        VirusSpread,    // virus mới mọc vào ô, lan từ virus ở SourceCell
        WokeUp,         // mèo ngủ thức dậy
        ColorChanged,   // mèo hai màu đổi màu (Piece là quân sau khi đổi; màu cũ nằm ở OtherColor)
    }

    // Một việc xảy ra ở một ô lúc cuối lượt, để giao diện diễn hoạt.
    public readonly struct TurnEvent
    {
        public TurnEvent(TurnEventKind kind, int cell, Piece piece, int sourceCell = Level.NoCell)
        {
            Kind = kind;
            Cell = cell;
            Piece = piece;
            SourceCell = sourceCell;
        }

        public TurnEventKind Kind { get; }

        public int Cell { get; }

        // Quân bị mất, mới mọc hoặc vừa thức.
        public Piece Piece { get; }

        // Với VirusSpread: ô virus gốc. Còn lại là NoCell.
        public int SourceCell { get; }
    }
}
