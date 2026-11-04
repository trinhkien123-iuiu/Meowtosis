using System;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;
using Meowtosis.Gameplay;

namespace Meowtosis.Presentation
{
    // Chọn mục tiêu cho booster bằng cách chạm lên bàn, như prototype:
    //   Hoán đổi: chạm một con (không phải virus) → chọn màu mới (chỉ còn một màu thì đổi luôn).
    //   Nhân bản: chạm con muốn nhân bản → chạm ô trống (không phải ô độc).
    // Khi đủ thông tin thì trả về lệnh để presenter đi nước đó.
    public sealed class BoosterTargeting
    {
        private readonly GameRules rules;
        private readonly IBoardView board;
        private readonly IHudView hud;
        private int sourceCell = Level.NoCell;

        public BoosterTargeting(GameRules rules, IBoardView board, IHudView hud)
        {
            this.rules = rules;
            this.board = board;
            this.hud = hud;
        }

        public BoosterKind Kind { get; private set; }

        public bool IsActive => Kind != BoosterKind.None;

        // Lời nhắn cho bước đang chọn, rỗng khi không chọn gì.
        public string Prompt { get; private set; } = "";

        public void Begin(BoosterKind kind, Board current)
        {
            Kind = kind;
            sourceCell = Level.NoCell;
            Prompt = kind == BoosterKind.Mutate ? GameTexts.MutatePickCat : GameTexts.TransplantPickSource;
            board.Highlight(PiecesThatCanBeUsed(current));
            hud.ShowColorChoices(Array.Empty<Species>());
        }

        public void Cancel()
        {
            Kind = BoosterKind.None;
            sourceCell = Level.NoCell;
            Prompt = "";
            board.Highlight(Array.Empty<int>());
            hud.ShowColorChoices(Array.Empty<Species>());
        }

        // Chạm một ô khi đang chọn; trả lệnh khi đã đủ mục tiêu, không thì null.
        public IGameCommand Tap(int cell, Board current)
        {
            var piece = current[cell];
            if (piece.HasValue && piece.Value.IsVirus)
            {
                hud.ShowToast(GameTexts.NotOnVirus);
                return null;
            }
            return Kind == BoosterKind.Mutate ? TapForMutate(cell, piece, current) : TapForTransplant(cell, piece, current);
        }

        public IGameCommand ChooseColor(Species color) =>
            Kind == BoosterKind.Mutate && sourceCell != Level.NoCell ? new MutateCommand(rules.Boosters, sourceCell, color) : null;

        private IGameCommand TapForMutate(int cell, Piece? piece, Board current)
        {
            if (!piece.HasValue) return null;
            var level = current.Level;
            var species = piece.Value.Species;
            if (level.StartingColors.Contains(species) && current.Count(other => other.Species == species) == 1)
            {
                hud.ShowToast(GameTexts.LastOfColor(species));
                return null;
            }
            var otherColors = level.StartingColors.Where(color => color != species).ToList();
            if (otherColors.Count == 0)
            {
                hud.ShowToast(GameTexts.NoOtherColor);
                return null;
            }
            if (otherColors.Count == 1) return new MutateCommand(rules.Boosters, cell, otherColors[0]);

            sourceCell = cell;
            Prompt = GameTexts.MutatePickColor;
            board.Highlight(new[] { cell });
            hud.ShowColorChoices(otherColors);
            return null;
        }

        private IGameCommand TapForTransplant(int cell, Piece? piece, Board current)
        {
            var level = current.Level;
            if (piece.HasValue)
            {
                sourceCell = cell;
                Prompt = GameTexts.TransplantPickTarget;
                board.Highlight(EmptyCells(current).Append(cell));
                return null;
            }
            if (sourceCell == Level.NoCell || !level.IsSlot(cell)) return null;
            if (level.IsToxic(cell))
            {
                hud.ShowToast(GameTexts.NotOnToxic);
                return null;
            }
            return new TransplantCommand(rules.Boosters, sourceCell, cell);
        }

        private static IEnumerable<int> PiecesThatCanBeUsed(Board current) =>
            Enumerable.Range(0, current.CellCount).Where(cell => current[cell].HasValue && !current[cell].Value.IsVirus);

        private static IEnumerable<int> EmptyCells(Board current) =>
            Enumerable.Range(0, current.CellCount)
                .Where(cell => current.Level.IsSlot(cell) && !current.Level.IsToxic(cell) && !current[cell].HasValue);
    }
}
