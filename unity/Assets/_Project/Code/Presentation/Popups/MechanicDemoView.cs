using System.Collections;
using System.Collections.Generic;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Minh hoạ chạy lặp trong thẻ cơ chế: bàn trước khi vuốt, bàn tay lướt theo nước vuốt, rồi bàn sau khi vuốt.
    // Bàn "sau" do chính luật chơi tính ra nên luôn khớp với game.
    public sealed class MechanicDemoView : MonoBehaviour
    {
        [SerializeField] private BoardArt art;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private DemoCell cellPrefab;
        [SerializeField] private RectTransform membranePrefab;
        [SerializeField] private RectTransform hand;
        [SerializeField] private float maximumCellSize = 170f;
        [SerializeField] private float beforeSeconds = 0.9f;
        [SerializeField] private float swipeSeconds = 0.55f;
        [SerializeField] private float afterSeconds = 1.3f;

        private readonly List<DemoCell> cells = new List<DemoCell>();
        private readonly List<RectTransform> membranes = new List<RectTransform>();
        private float cellSize;

        public void Play(LevelDefinition definition, Move move)
        {
            StopAllCoroutines();
            var level = new Level(definition);
            var before = level.CreateStartBoard();
            var swipes = new SwipeRule(TurnResolver.CreateStandard());
            var after = swipes.TrySwipe(before, move, out var result) ? result.After : before;
            Build(level);
            StartCoroutine(Loop(level, before, after, move));
        }

        private void Build(Level level)
        {
            foreach (var cell in cells) Destroy(cell.gameObject);
            foreach (var membrane in membranes) Destroy(membrane.gameObject);
            cells.Clear();
            membranes.Clear();

            var area = boardArea.rect.size;
            cellSize = Mathf.Min(maximumCellSize, area.x / level.Width, area.y / level.Height);
            for (var cell = 0; cell < level.CellCount; cell++)
            {
                var view = Instantiate(cellPrefab, boardArea);
                ((RectTransform)view.transform).anchoredPosition = CellPosition(level, cell);
                view.transform.localScale = Vector3.one * (cellSize / ((RectTransform)view.transform).rect.width);
                view.ShowCell(level, cell, art);
                cells.Add(view);
            }
            foreach (var membrane in level.Membranes)
            {
                var view = Instantiate(membranePrefab, boardArea);
                var offset = BoardLayout.Offset(membrane.Direction) * cellSize * 0.5f;
                view.anchoredPosition = CellPosition(level, level.IndexOf(membrane.Row, membrane.Column)) + (Vector2)offset;
                view.localRotation = Quaternion.Euler(0f, 0f, BoardView.AngleOf(membrane.Direction));
                view.localScale = Vector3.one * (cellSize / view.rect.width);
                membranes.Add(view);
            }
            hand.SetAsLastSibling();
        }

        private IEnumerator Loop(Level level, Board before, Board after, Move move)
        {
            var from = CellPosition(level, level.IndexOf(move.Row, move.Column));
            var to = from + (Vector2)(BoardLayout.Offset(move.Direction) * cellSize);
            while (true)
            {
                ShowBoard(level, before);
                hand.gameObject.SetActive(true);
                hand.anchoredPosition = from;
                yield return new WaitForSecondsRealtime(beforeSeconds);

                for (var time = 0f; time < swipeSeconds; time += Time.unscaledDeltaTime)
                {
                    hand.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, time / swipeSeconds));
                    yield return null;
                }
                hand.gameObject.SetActive(false);
                ShowBoard(level, after);
                yield return new WaitForSecondsRealtime(afterSeconds);
            }
        }

        private void ShowBoard(Level level, Board board)
        {
            for (var cell = 0; cell < cells.Count; cell++)
            {
                var piece = board[cell];
                var isHappy = piece.HasValue && piece.Value.IsCat && level.TargetAt(cell) == piece.Value.Species;
                cells[cell].ShowPiece(piece, art, ((RectTransform)cells[cell].transform).rect.width, isHappy);
            }
        }

        private Vector2 CellPosition(Level level, int cell) => new BoardLayout(level).CellCenter(cell) * cellSize;
    }
}
