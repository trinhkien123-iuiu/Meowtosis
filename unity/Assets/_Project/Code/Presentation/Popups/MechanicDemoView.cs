using System.Collections;
using System.Collections.Generic;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Minh hoạ chạy lặp trong thẻ luật: bàn ban đầu, rồi từng nước một: bàn tay lướt theo nước vuốt, bản sao hiện ra
    // (bàn ngay sau khi đặt), và nếu lượt đó có khử 3 thì một nhịp sau mới tới bàn đã khử. Hết chuỗi thì nghỉ rồi lặp lại.
    // Mọi bàn đều do chính luật chơi tính ra nên luôn khớp với game.
    public sealed class MechanicDemoView : MonoBehaviour
    {
        [SerializeField] private BoardArt art;
        [SerializeField] private RectTransform boardArea;
        [SerializeField] private DemoCell cellPrefab;
        [SerializeField] private RectTransform membranePrefab;
        [SerializeField] private DemoPortalGate portalGatePrefab;
        [Tooltip("Màu từng cặp cổng, theo thứ tự trong level (giống bàn thật).")]
        [SerializeField] private Color[] portalColors = { new Color(0.72f, 0.48f, 1f), new Color(1f, 0.48f, 0.78f), new Color(0.22f, 0.85f, 0.78f) };
        [SerializeField] private RectTransform hand;
        [SerializeField] private float maximumCellSize = 170f;
        [SerializeField] private float beforeSeconds = 0.9f;
        [SerializeField] private float swipeSeconds = 0.55f;
        [SerializeField] private float afterSeconds = 1.3f;
        [Tooltip("Nghỉ giữa hai nước trong chuỗi.")]
        [SerializeField] private float betweenSeconds = 0.55f;
        [Tooltip("Thời gian thấy bàn ngay sau khi đặt bản sao, trước khi 3 con biến mất.")]
        [SerializeField] private float beforeClearSeconds = 0.45f;

        private readonly List<DemoCell> cells = new List<DemoCell>();
        private readonly List<RectTransform> membranes = new List<RectTransform>();
        private readonly List<DemoPortalGate> portals = new List<DemoPortalGate>();
        private float cellSize;

        public void Play(LevelDefinition definition, IReadOnlyList<Move> moves)
        {
            StopAllCoroutines();
            var level = new Level(definition);
            var start = level.CreateStartBoard();
            var swipes = new SwipeRule(TurnResolver.CreateStandard());
            var steps = new List<MoveResult>();
            var board = start;
            foreach (var move in moves)
            {
                if (!swipes.TrySwipe(board, move, out var result)) break;
                steps.Add(result);
                board = result.After;
            }
            Build(level);
            StartCoroutine(Loop(level, start, moves, steps));
        }

        private void Build(Level level)
        {
            foreach (var cell in cells) Destroy(cell.gameObject);
            foreach (var membrane in membranes) Destroy(membrane.gameObject);
            foreach (var portal in portals) Destroy(portal.gameObject);
            cells.Clear();
            membranes.Clear();
            portals.Clear();

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
            if (portalGatePrefab != null)
            {
                for (var index = 0; index < level.Portals.Count; index++)
                {
                    var portal = level.Portals[index];
                    var color = portalColors[index % portalColors.Length];
                    AddPortalGate(level, level.IndexOf(portal.Row, portal.Column), portal.Direction, color);
                    AddPortalGate(level, level.IndexOf(portal.TargetRow, portal.TargetColumn), portal.Direction.Opposite(), color);
                }
            }
            hand.SetAsLastSibling();
        }

        // Một đầu cổng nằm ở cạnh ô nhìn ra ngoài bàn, xoay theo hướng ra ngoài.
        private void AddPortalGate(Level level, int cell, Direction outward, Color color)
        {
            var gate = Instantiate(portalGatePrefab, boardArea);
            var rect = (RectTransform)gate.transform;
            rect.anchoredPosition = CellPosition(level, cell) + (Vector2)(BoardLayout.Offset(outward) * cellSize * 0.52f);
            rect.localRotation = Quaternion.Euler(0f, 0f, BoardView.AngleOf(outward));
            rect.localScale = Vector3.one * (cellSize / rect.rect.width);
            gate.SetColor(color);
            portals.Add(gate);
        }

        private IEnumerator Loop(Level level, Board start, IReadOnlyList<Move> moves, IReadOnlyList<MoveResult> steps)
        {
            while (true)
            {
                ShowBoard(level, start);
                hand.gameObject.SetActive(false);
                yield return new WaitForSecondsRealtime(beforeSeconds * 0.5f);
                for (var index = 0; index < steps.Count; index++)
                {
                    var move = moves[index];
                    var from = CellPosition(level, level.IndexOf(move.Row, move.Column));
                    var to = from + (Vector2)(BoardLayout.Offset(move.Direction) * cellSize);
                    hand.gameObject.SetActive(true);
                    hand.anchoredPosition = from;
                    yield return new WaitForSecondsRealtime(index == 0 ? beforeSeconds * 0.5f : betweenSeconds);
                    for (var time = 0f; time < swipeSeconds; time += Time.unscaledDeltaTime)
                    {
                        hand.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, time / swipeSeconds));
                        yield return null;
                    }
                    hand.gameObject.SetActive(false);
                    var step = steps[index];
                    ShowBoard(level, step.Placed);
                    if (step.Stages.Count > 0)
                    {
                        yield return new WaitForSecondsRealtime(beforeClearSeconds);
                        ShowBoard(level, step.After);
                    }
                }
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
