using System.Collections;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Diễn hoạt hai booster làm đổi bàn chơi (MovePlayback gọi khi nước đi không phải lần vuốt):
    //   Hoán đổi: vòng ma thuật bừng lên dưới chân, mèo cũ xoay tít thu nhỏ thành đốm sáng, nổ ra màu mới, mèo mới bật lên.
    //   Nhân bản: mèo gốc nảy lên, một bản sao trong suốt bay vòng cung tới ô trống (rắc lấp lánh dọc đường),
    //             đáp xuống như từ trên cao: bụi tung lên hai bên chân, vòng ma thuật loé, mèo thật bật ra.
    internal sealed class BoosterPlayback
    {
        public const float MutateSeconds = 0.95f;
        public const float CloneSeconds = 0.95f;
        private const float ChargeSeconds = 0.45f;
        private const float FlightSeconds = 0.5f;
        private const float PopSeconds = 0.3f;
        private const float ArcHeight = 0.9f;
        private const float TrailStep = 0.06f;
        private const int GhostOrderBoost = 25;
        private const float DustDrop = 0.28f;   // bụi tung ở ngang chân mèo, thấp hơn tâm ô

        private readonly BoardView board;
        private readonly BoardEffects effects;
        private readonly ISoundPlayer sounds;
        private readonly PieceMotions motions;
        private readonly BoardArt art;

        public BoosterPlayback(BoardView board, BoardEffects effects, ISoundPlayer sounds, PieceMotions motions, BoardArt art)
        {
            this.board = board;
            this.effects = effects;
            this.sounds = sounds;
            this.motions = motions;
            this.art = art;
        }

        // Bắt đầu diễn; trả về số giây các bước sau phải chờ.
        public float Play(MoveResult move)
        {
            var newPiece = move.Placed[move.PlacedCell];
            if (!newPiece.HasValue) { board.ShowPieces(move.Placed, move); return 0f; }
            if (move.PlacedCell == move.SourceCell)
            {
                var oldView = move.Replaced is Piece replaced ? board.TakePieceView(replaced.Id) : null;
                board.ShowPieces(move.Placed, move);
                var newView = board.PieceViewOf(newPiece.Value.Id);
                board.StartCoroutine(Mutate(move, oldView, newView, newPiece.Value));
                return MutateSeconds;
            }
            var sourceView = board.PieceViewOf(move.Before[move.SourceCell].Value.Id);
            board.ShowPieces(move.Placed, move);
            var copyView = board.PieceViewOf(newPiece.Value.Id);
            board.StartCoroutine(Clone(move, sourceView, copyView, newPiece.Value));
            return CloneSeconds;
        }

        private IEnumerator Mutate(MoveResult move, PieceView oldView, PieceView newView, Piece newPiece)
        {
            var cell = move.PlacedCell;
            var position = board.CellPosition(cell);
            var oldColor = move.Replaced is Piece replaced ? art.ColorOf(replaced.Species) : Color.white;
            var newColor = art.ColorOf(newPiece.Species);
            sounds.Play(SoundId.MutateMagic);
            board.MagicFlare(cell, oldColor);
            effects.Rainbow(position);
            if (newView != null) newView.transform.localScale = Vector3.zero;

            // Tụ phép: mèo cũ xoay tít, co lại, sáng dần màu mới.
            if (oldView != null)
            {
                for (var time = 0f; time < ChargeSeconds; time += Time.deltaTime)
                {
                    var t = time / ChargeSeconds;
                    var eased = t * t;
                    oldView.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, eased);
                    oldView.transform.localRotation = Quaternion.Euler(0f, 0f, -720f * eased);
                    yield return null;
                }
                Object.Destroy(oldView.gameObject);
            }
            else yield return new WaitForSeconds(ChargeSeconds);

            // Bùm: màu mới nổ ra, mèo mới bật lên.
            effects.Burst(position, newColor);
            effects.Ring(position, newColor);
            effects.Sparkle(position);
            board.MagicFlare(cell, newColor);
            if (newView != null)
            {
                newView.transform.localRotation = Quaternion.identity;
                newView.PlayMotion(motions.Morph);
                yield return Pop(newView.transform);
            }
            sounds.PlayMeow(newPiece.Species, 1f, 1.1f);
        }

        private IEnumerator Clone(MoveResult move, PieceView sourceView, PieceView copyView, Piece newPiece)
        {
            var from = board.CellPosition(move.SourceCell);
            var to = board.CellPosition(move.PlacedCell);
            var color = art.ColorOf(newPiece.Species);
            sounds.Play(SoundId.CloneMagic);
            board.MagicFlare(move.SourceCell, color);
            sourceView?.PlayMotion(motions.Hop);

            // Bản sao thật nằm sẵn ở ô đích nhưng ẩn tới khi bản sao trong suốt bay tới.
            if (copyView != null)
            {
                copyView.Place(copyView.transform.parent.InverseTransformPoint(to));
                copyView.transform.localScale = Vector3.zero;
            }
            var ghost = MakeGhost(sourceView);
            var nextTrail = 0f;
            for (var time = 0f; time < FlightSeconds; time += Time.deltaTime)
            {
                var t = time / FlightSeconds;
                var eased = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                var point = Vector3.Lerp(from, to, eased) + Vector3.up * (Mathf.Sin(eased * Mathf.PI) * ArcHeight);
                if (ghost != null)
                {
                    ghost.position = point;
                    ghost.localScale = Vector3.one * (0.75f + 0.25f * Mathf.Sin(t * Mathf.PI));
                    ghost.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 2f) * 14f);
                }
                if (time >= nextTrail)
                {
                    effects.Sparkle(point);
                    nextTrail += TrailStep;
                }
                yield return null;
            }
            if (ghost != null) Object.Destroy(ghost.gameObject);

            // Đáp xuống như từ trên cao: bụi tung lên hai bên chân, vòng ma thuật loé, mèo thật bật ra.
            board.MagicFlare(move.PlacedCell, color);
            effects.Dust(to + Vector3.down * DustDrop);
            effects.Ring(to, color);
            if (copyView != null)
            {
                copyView.PlayMotion(motions.Born);
                yield return Pop(copyView.transform);
            }
            sounds.PlayMeow(newPiece.Species, 1f, 1.15f);
        }

        // Bản sao trong suốt: chép lớp hình của mèo gốc, vẽ đè lên mọi quân khác.
        private Transform MakeGhost(PieceView sourceView)
        {
            if (sourceView == null) return null;
            var copy = Object.Instantiate(sourceView.Visual.gameObject, sourceView.transform.parent, true);
            copy.name = "CloneGhost";
            foreach (var renderer in copy.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingOrder += GhostOrderBoost;
                var c = renderer.color;
                renderer.color = new Color(c.r, c.g, c.b, c.a * 0.8f);
            }
            return copy.transform;
        }

        // Bật lên: 0 → to hơn một chút → về cỡ thường.
        private static IEnumerator Pop(Transform target)
        {
            for (var time = 0f; time < PopSeconds; time += Time.deltaTime)
            {
                var t = time / PopSeconds;
                var scale = t < 0.6f ? Mathf.Lerp(0f, 1.25f, t / 0.6f) : Mathf.Lerp(1.25f, 1f, (t - 0.6f) / 0.4f);
                if (target != null) target.localScale = Vector3.one * scale;
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }
    }
}
