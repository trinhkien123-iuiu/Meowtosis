namespace Meowtosis.Domain
{
    // Một con mèo, vi khuẩn hoặc virus đứng trong một ô. Id đi theo quân khi nó di chuyển, để giao diện bám theo được.
    public readonly struct Piece
    {
        public Piece(int id, Species species, bool isAsleep = false, Direction facing = Direction.Up)
        {
            Id = id;
            Species = species;
            IsAsleep = isAsleep;
            Facing = facing;
        }

        public int Id { get; }

        public Species Species { get; }

        public bool IsAsleep { get; }

        // Hướng virus lan. Loài khác bỏ qua.
        public Direction Facing { get; }

        public bool IsVirus => Species == Species.Virus;

        public bool IsCat => Species != Species.Germ && Species != Species.Virus;

        public bool CanSwipe => !IsAsleep && !IsVirus;

        public Piece WakeUp() => new Piece(Id, Species, false, Facing);
    }
}
