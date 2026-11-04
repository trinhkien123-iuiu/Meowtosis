namespace Meowtosis.Domain
{
    // Ký hiệu ô dùng trong file level và khoá của bàn:
    //   # lỗ (không thuộc bàn)   . ô trống   ~ ô độc
    //   A B C D E  mèo đỏ, xanh, vàng, hồng, tím   G vi khuẩn   chữ thường = đang ngủ
    //   ^ v < >    virus quay lên, xuống, trái, phải
    public static class LevelSymbols
    {
        public const char Hole = '#';
        public const char Empty = '.';
        public const char Toxic = '~';

        public static bool TryReadSpecies(char letter, out Species species)
        {
            switch (letter)
            {
                case 'A': species = Species.Red; return true;
                case 'B': species = Species.Blue; return true;
                case 'C': species = Species.Yellow; return true;
                case 'D': species = Species.Pink; return true;
                case 'E': species = Species.Purple; return true;
                case 'G': species = Species.Germ; return true;
                default: species = default; return false;
            }
        }

        public static char LetterOf(Species species) => species switch
        {
            Species.Red => 'A',
            Species.Blue => 'B',
            Species.Yellow => 'C',
            Species.Pink => 'D',
            Species.Purple => 'E',
            Species.Germ => 'G',
            _ => 'V',
        };

        public static bool TryReadPiece(char symbol, int id, out Piece piece)
        {
            if (TryReadArrow(symbol, out var facing))
            {
                piece = new Piece(id, Species.Virus, false, facing);
                return true;
            }
            if (TryReadSpecies(char.ToUpperInvariant(symbol), out var species))
            {
                piece = new Piece(id, species, char.IsLower(symbol));
                return true;
            }
            piece = default;
            return false;
        }

        public static char SymbolOf(Piece? piece)
        {
            if (!piece.HasValue) return Empty;
            var value = piece.Value;
            if (value.IsVirus) return ArrowOf(value.Facing);
            var letter = LetterOf(value.Species);
            return value.IsAsleep ? char.ToLowerInvariant(letter) : letter;
        }

        public static char ArrowOf(Direction direction) => direction switch
        {
            Direction.Up => '^',
            Direction.Down => 'v',
            Direction.Left => '<',
            _ => '>',
        };

        private static bool TryReadArrow(char symbol, out Direction facing)
        {
            switch (symbol)
            {
                case '^': facing = Direction.Up; return true;
                case 'v': facing = Direction.Down; return true;
                case '<': facing = Direction.Left; return true;
                case '>': facing = Direction.Right; return true;
                default: facing = default; return false;
            }
        }
    }
}
