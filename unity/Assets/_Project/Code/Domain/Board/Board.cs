using System;
using System.Collections.Generic;

namespace Meowtosis.Domain
{
    // Một thời điểm của ván chơi: level cộng quân trong từng ô. Bất biến: mọi luật trả về một Board mới.
    public sealed class Board
    {
        private readonly Piece?[] pieces;
        private string cachedKey;

        // Nhận luôn mảng truyền vào: bên gọi không được sửa mảng đó nữa.
        internal Board(Level level, Piece?[] pieces, int nextPieceId)
        {
            Level = level;
            this.pieces = pieces;
            NextPieceId = nextPieceId;
        }

        public Level Level { get; }

        // Id cho quân mới tiếp theo, để mọi quân trên bàn có id riêng.
        public int NextPieceId { get; }

        public int CellCount => pieces.Length;

        public Piece? this[int cell] => pieces[cell];

        public bool Contains(Species species)
        {
            foreach (var piece in pieces)
            {
                if (piece.HasValue && piece.Value.Species == species) return true;
            }
            return false;
        }

        public int Count(Func<Piece, bool> predicate)
        {
            var count = 0;
            foreach (var piece in pieces)
            {
                if (piece.HasValue && predicate(piece.Value)) count++;
            }
            return count;
        }

        // Mỗi ô một ký hiệu, theo từng hàng, viết như trong file level ('.' cho ô trống hoặc lỗ); bỏ qua id.
        // Hai bàn cùng khoá thì chơi y hệt nhau, nên bộ giải dùng khoá này để bỏ qua thế cờ đã gặp.
        public string ToKey()
        {
            if (cachedKey != null) return cachedKey;
            var symbols = new char[pieces.Length];
            for (var cell = 0; cell < pieces.Length; cell++) symbols[cell] = LevelSymbols.SymbolOf(pieces[cell]);
            var key = new string(symbols);
            // Mèo hai màu: thêm màu kia và lượt còn lại ở cuối khoá (ToRows chỉ lấy phần đầu), để bộ giải phân biệt các thế cờ.
            for (var cell = 0; cell < pieces.Length; cell++)
            {
                if (pieces[cell] is Piece piece && piece.IsTwoColor)
                    key += $"|{cell}{LevelSymbols.LetterOf(piece.OtherColor.Value)}{piece.TurnsLeft}";
            }
            cachedKey = key;
            return cachedKey;
        }

        public IReadOnlyList<string> ToRows()
        {
            var key = ToKey();
            var rows = new string[Level.Height];
            for (var row = 0; row < Level.Height; row++) rows[row] = key.Substring(row * Level.Width, Level.Width);
            return rows;
        }

        public override string ToString() => string.Join("\n", ToRows());

        internal Piece?[] CopyPieces() => (Piece?[])pieces.Clone();

        internal Board With(Piece?[] changedPieces) => new Board(Level, changedPieces, NextPieceId);

        internal Board With(Piece?[] changedPieces, int nextPieceId) => new Board(Level, changedPieces, nextPieceId);
    }
}
