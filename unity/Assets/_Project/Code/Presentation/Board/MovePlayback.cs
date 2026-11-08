using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Diễn lại một nước đi theo từng chặng, đúng thứ tự và nhịp của prototype (commit trong meowtosis.html):
    // bản sao mọc ra và dãy trượt, quân bị thay thế vỡ, ô độc bốc khói, bộ 3 nổ, virus lan và ăn, mèo ngủ thức dậy.
    internal sealed class MovePlayback
    {
        private const float DelayBeforeRemoving = 0.22f;
        private const float LingerOnClearSeconds = 0.9f;
        private const float RemovingSeconds = 0.33f;
        private const float DelayBeforeSpread = 0.24f;
        private const float DelayAfterRemoval = 0.08f;
        private const float BiteSeconds = 0.3f;
        private const float AfterEatingSeconds = 0.26f;
        private const float AfterSpreadSeconds = 0.34f;
        private const float DizzyDelay = 0.23f;
        private const float ChainDelay = 0.1f;
        private const float ChainStep = 0.05f;
        private const float GateGlowDelay = 0.08f;
        private const float ColorChangeSeconds = 0.42f;
        private const float IceSlideSecondsPerCell = 0.17f;   // lướt chậm để mắt kịp nhìn
        private const float IceSlideMinimumSeconds = 0.4f;

        private readonly BoardView board;
        private readonly BoardEffects effects;
        private readonly ISoundPlayer sounds;
        private readonly PieceMotions motions;
        private readonly BoardArt art;

        public MovePlayback(BoardView board, BoardEffects effects, ISoundPlayer sounds, PieceMotions motions, BoardArt art)
        {
            this.board = board;
            this.effects = effects;
            this.sounds = sounds;
            this.motions = motions;
            this.art = art;
        }

        public IEnumerator Run(MoveResult move, Action finished, bool lingerOnClear)
        {
            var slideSeconds = PlacePiece(move);
            if (slideSeconds > 0f) yield return new WaitForSeconds(slideSeconds);
            var removedAlready = false;
            foreach (var stage in move.Stages)
            {
                var poisoned = EventsOf(stage, TurnEventKind.Poisoned);
                var cleared = EventsOf(stage, TurnEventKind.Cleared);
                var spreads = EventsOf(stage, TurnEventKind.VirusSpread);
                var woken = EventsOf(stage, TurnEventKind.WokeUp);
                var flips = EventsOf(stage, TurnEventKind.ColorChanged);

                if (flips.Count > 0)
                {
                    PlayColorChanges(stage, flips);
                    yield return new WaitForSeconds(ColorChangeSeconds);
                }

                if (poisoned.Count > 0 || cleared.Count > 0)
                {
                    yield return new WaitForSeconds(removedAlready ? 0f : DelayBeforeRemoving);
                    if (lingerOnClear && cleared.Count > 0)
                    {
                        board.Highlight(cleared.Select(turnEvent => turnEvent.Cell));
                        yield return new WaitForSeconds(LingerOnClearSeconds);
                        board.Highlight(Enumerable.Empty<int>());
                    }
                    RemovePoisoned(poisoned);
                    RemoveCleared(cleared, stage.Board);
                    removedAlready = true;
                    yield return new WaitForSeconds(RemovingSeconds);
                    board.ShowPieces(stage.Board, null);
                }
                if (spreads.Count > 0)
                {
                    yield return new WaitForSeconds(removedAlready ? DelayAfterRemoval : DelayBeforeSpread);
                    yield return Spread(stage, spreads, EventsOf(stage, TurnEventKind.Eaten));
                }
                if (woken.Count > 0) WakeUp(stage, woken);
            }

            board.ShowPieces(move.After, null);
            PlayBoxSound(move);
            finished?.Invoke();
        }

        private static List<TurnEvent> EventsOf(TurnStage stage, TurnEventKind kind) =>
            stage.Events.Where(turnEvent => turnEvent.Kind == kind).ToList();

        // Trả về số giây bản sao lướt trên băng (0 nếu không lướt): bước sau chờ chừng đó mới diễn tiếp.
        private float PlacePiece(MoveResult move)
        {
            var parent = move.Before[move.SourceCell];
            if (move.Replaced is Piece replaced)
            {
                board.TakePieceView(replaced.Id)?.PlayThenDestroy(motions.Pop);
                effects.Burst(board.CellPosition(move.PlacedCell), art.ColorOf(replaced.Species));
                sounds.Play(SoundId.Replace);
            }
            board.ShowPieces(move.Placed, move);
            if (move.PlacedCell != move.SourceCell)
            {
                effects.Sparkle(board.CellPosition(move.PlacedCell));
            }
            var slideSeconds = move.SwipeDirection.HasValue ? PlayMechanics(move, move.SwipeDirection.Value) : 0f;

            if (move.SwipeDirection.HasValue) PlaySwipe(move, parent.Value, move.SwipeDirection.Value);
            else PlayBooster(move, parent);
            return slideSeconds;
        }

        private void PlaySwipe(MoveResult move, Piece parent, Direction direction)
        {
            var vector = (Vector2)BoardLayout.Offset(direction);
            sounds.PlayMeow(parent.Species);
            var parentView = board.PieceViewOf(parent.Id);
            if (move.Pushes.Count == 0)
            {
                parentView?.PlayMotion(motions.Squash, vector);
                return;
            }

            sounds.Play(SoundId.Push);
            parentView?.PlayMotion(motions.Shove, vector);
            // Con mẹ chồm tới; cả dãy bị đẩy dồn lại rồi nảy về, con càng xa càng nảy sau. Các đợt đẩy xếp từ xa về gần.
            for (var index = 0; index < move.Pushes.Count; index++)
            {
                var shift = move.Pushes[index];
                var pushed = move.Before[shift.From].Value;
                var nearness = move.Pushes.Count - 1 - index;
                board.PieceViewOf(pushed.Id)?.PlayMotion(motions.Bumped, vector, ChainDelay + ChainStep * nearness);
                var level = move.Before.Level;
                if (level.TargetAt(shift.From) == pushed.Species && level.TargetAt(shift.To) != pushed.Species)
                    board.StartCoroutine(MakeDizzy(pushed.Id));
            }
        }

        // Hiệu ứng riêng của cơ chế trên đường đi của cú vuốt: màng một chiều phát hào quang khi có quân đi qua,
        // bản sao trượt trên băng để lại tuyết ở từng ô băng đi qua.
        // Trả về số giây bản sao lướt trên băng (0 nếu không lướt).
        private float PlayMechanics(MoveResult move, Direction direction)
        {
            var level = move.Before.Level;
            var crossings = new List<KeyValuePair<int, int>>();
            var target = level.Step(move.SourceCell, direction);
            if (target != Level.NoCell) crossings.Add(new KeyValuePair<int, int>(move.SourceCell, target));
            foreach (var shift in move.Pushes) crossings.Add(new KeyValuePair<int, int>(shift.From, shift.To));

            // Bản sao trượt từ ô đầu tiên tới ô dừng; mỗi ô băng đi qua có một làn tuyết.
            var frostCells = new List<int>();
            if (target != Level.NoCell)
            {
                var at = target;
                for (var guard = 0; guard < level.CellCount; guard++)
                {
                    if (level.IsIce(at)) frostCells.Add(at);
                    if (at == move.PlacedCell) break;
                    var next = level.Step(at, direction);
                    if (next == Level.NoCell) break;
                    crossings.Add(new KeyValuePair<int, int>(at, next));
                    at = next;
                }
            }

            // Bản sao trượt xa hơn ô đầu tiên (do băng): lướt chậm tới ô dừng, vạch tốc độ bám theo suốt quãng lướt.
            var slideSeconds = 0f;
            if (target != Level.NoCell && move.PlacedCell != target && move.Placed[move.PlacedCell] is Piece slider)
            {
                var view = board.PieceViewOf(slider.Id);
                if (view != null)
                {
                    var distance = Vector3.Distance(board.CellPosition(move.SourceCell), board.CellPosition(move.PlacedCell));
                    slideSeconds = Mathf.Max(IceSlideMinimumSeconds, distance * IceSlideSecondsPerCell);
                    board.SlidePieceTo(view, move.PlacedCell, slideSeconds);
                    effects.SpeedLines(view, direction, slideSeconds);
                }
            }

            var frostStep = frostCells.Count > 0 ? Mathf.Max(slideSeconds, 0.1f) / frostCells.Count : 0.07f;
            board.StartCoroutine(PlayMechanicsLater(level, direction, crossings, frostCells, frostStep));
            return slideSeconds;
        }

        private IEnumerator PlayMechanicsLater(Level level, Direction direction, List<KeyValuePair<int, int>> crossings, List<int> frostCells, float frostStep)
        {
            yield return new WaitForSeconds(GateGlowDelay);
            var passed = false;
            foreach (var crossing in crossings)
            {
                if (!HasMembrane(level, crossing.Key, direction)) continue;
                var middle = (board.CellPosition(crossing.Key) + board.CellPosition(crossing.Value)) * 0.5f;
                effects.GateGlow(middle, direction);
                passed = true;
            }
            if (passed) sounds.Play(SoundId.Sparkle);

            foreach (var cell in frostCells)
            {
                effects.Frost(board.CellPosition(cell));
                yield return new WaitForSeconds(frostStep);
            }
        }

        // Có màng một chiều trên cạnh từ `cell` theo `direction` (quân đi đúng chiều của mũi tên).
        private static bool HasMembrane(Level level, int cell, Direction direction)
        {
            foreach (var membrane in level.Membranes)
            {
                if (level.IndexOf(membrane.Row, membrane.Column) == cell && membrane.Direction == direction) return true;
            }
            return false;
        }

        // Mèo đang đứng đúng hộp mà bị đẩy ra khỏi hộp: choáng.
        private IEnumerator MakeDizzy(int pieceId)
        {
            yield return new WaitForSeconds(DizzyDelay);
            var view = board.PieceViewOf(pieceId);
            if (view == null) yield break;
            view.PlayMotion(motions.Dazed);
            effects.Dizzy(view);
        }

        private void PlayBooster(MoveResult move, Piece? parent)
        {
            sounds.Play(SoundId.Booster);
            var position = board.CellPosition(move.PlacedCell);
            var newPiece = move.Placed[move.PlacedCell];
            if (move.PlacedCell == move.SourceCell)
            {
                effects.Rainbow(position);
                if (newPiece.HasValue) board.PieceViewOf(newPiece.Value.Id)?.PlayMotion(motions.Morph);
            }
            else
            {
                effects.Sparkle(position);
                if (parent.HasValue) sounds.PlayMeow(parent.Value.Species);
            }
        }

        // Mèo hai màu đổi màu: vẽ lại đúng màu mới, thân biến hình một nhịp, tia sáng màu mới nổ ra.
        private void PlayColorChanges(TurnStage stage, List<TurnEvent> flips)
        {
            board.ShowPieces(stage.Board, null);
            foreach (var turnEvent in flips)
            {
                var piece = turnEvent.Piece;
                board.PieceViewOf(piece.Id)?.PlayMotion(motions.Morph);
                effects.ColorFlip(board.CellPosition(turnEvent.Cell), art.ColorOf(piece.OtherColor.Value), art.ColorOf(piece.Species));
            }
            sounds.Play(SoundId.ColorSwitch);
        }

        private void RemovePoisoned(List<TurnEvent> poisoned)
        {
            foreach (var turnEvent in poisoned)
            {
                board.TakePieceView(turnEvent.Piece.Id)?.PlayThenDestroy(motions.Pop);
                var position = board.CellPosition(turnEvent.Cell);
                effects.Smoke(position);
                if (turnEvent.Piece.IsCat) effects.Soul(position);
            }
            if (poisoned.Count > 0) sounds.Play(SoundId.Toxic);
        }

        private void RemoveCleared(List<TurnEvent> cleared, Board boardAfterStage)
        {
            if (cleared.Count == 0) return;
            var species = cleared[0].Piece.Species;

            // Kiểm tra xem sau đợt khử này loài mèo đó có bị tuyệt chủng trên bàn không:
            // - isFatal = true: Khử 3 làm hết loài -> THUA (Visual/VFX/SFX nổ tung, khóc, báo động)
            // - isFatal = false: Khử 3 chiến thuật dọn chỗ -> KHÔNG THUA (Visual/VFX/SFX pháo hoa, vui vẻ)
            var isFatal = !boardAfterStage.Contains(species);

            if (isFatal)
            {
                // Toàn bộ mèo còn lại trên bàn khóc / hoảng hốt
                board.Mourn();

                foreach (var turnEvent in cleared)
                {
                    board.TakePieceView(turnEvent.Piece.Id)?.PlayThenDestroy(motions.Pop);
                    var position = board.CellPosition(turnEvent.Cell);
                    effects.Smoke(position);
                    effects.Soul(position);
                    effects.Burst(position, new Color(1f, 0.25f, 0.25f));
                }

                sounds.Play(SoundId.AngryMeow);
                sounds.Play(SoundId.Lose);
                board.Shake();
                effects.FloatText(board.CellPosition(cleared[cleared.Count / 2].Cell), $"HẾT MÈO {GameTexts.ColorName(species).ToUpper()}!");
            }
            else
            {
                foreach (var turnEvent in cleared)
                {
                    board.TakePieceView(turnEvent.Piece.Id)?.PlayThenDestroy(motions.Pop);
                    var position = board.CellPosition(turnEvent.Cell);
                    var color = art.ColorOf(turnEvent.Piece.Species);
                    effects.Burst(position, color);
                    effects.Ring(position, color);
                }

                sounds.Play(SoundId.Clear);
                sounds.Play(SoundId.Sparkle);
                board.Shake();
                effects.FloatText(board.CellPosition(cleared[cleared.Count / 2].Cell), $"+{cleared.Count} Ô TRỐNG!");
            }
        }

        // Virus chồm tới, hàm răng đớp, con mồi tím tái rồi bị nuốt; xong mới hiện virus mới ở ô đó.
        private IEnumerator Spread(TurnStage stage, List<TurnEvent> spreads, List<TurnEvent> eaten)
        {
            if (eaten.Count > 0)
            {
                foreach (var spread in spreads)
                {
                    var facing = BoardLayout.Offset(spread.Piece.Facing);
                    board.PieceAtCell(spread.SourceCell)?.PlayMotion(motions.Lunge, facing);
                }
                foreach (var turnEvent in eaten)
                {
                    effects.Jaws(board.CellPosition(turnEvent.Cell));
                    board.TakePieceView(turnEvent.Piece.Id)?.PlayThenDestroy(motions.Eaten);
                }
                sounds.Play(SoundId.Eat);
                yield return new WaitForSeconds(BiteSeconds);
            }

            board.ShowPieces(stage.Board, null);
            foreach (var turnEvent in eaten) effects.Splat(board.CellPosition(turnEvent.Cell));
            if (eaten.Count == 0) sounds.Play(SoundId.Push);
            yield return new WaitForSeconds(eaten.Count > 0 ? AfterEatingSeconds : AfterSpreadSeconds);
        }

        private void WakeUp(TurnStage stage, List<TurnEvent> woken)
        {
            board.ShowPieces(stage.Board, null);
            foreach (var turnEvent in woken)
            {
                board.PieceViewOf(turnEvent.Piece.Id)?.PlayMotion(motions.Waking);
                effects.Sparkle(board.CellPosition(turnEvent.Cell));
                effects.Ring(board.CellPosition(turnEvent.Cell), new Color(1f, 0.95f, 0.7f));
            }
            sounds.PlayMeow(woken[0].Piece.Species, 0.73f, 1.15f);
        }

        // Có hộp vừa được lấp đúng màu thì kêu "ting".
        private void PlayBoxSound(MoveResult move)
        {
            var level = move.Before.Level;
            for (var cell = 0; cell < level.CellCount; cell++)
            {
                var target = level.TargetAt(cell);
                if (!target.HasValue) continue;
                var wasDone = move.Before[cell] is Piece before && before.Species == target.Value;
                var isDone = move.After[cell] is Piece after && after.Species == target.Value;
                if (wasDone || !isDone) continue;
                sounds.Play(SoundId.BoxDone);
                effects.Sparkle(board.CellPosition(cell));
                effects.Ring(board.CellPosition(cell), art.ColorOf(target.Value));
            }
        }
    }
}
