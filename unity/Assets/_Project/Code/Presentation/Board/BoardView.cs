using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Dựng bàn chơi từ các prefab ở Prefabs/Board (khung, ô, ô độc, hộp, màng, quân) và đổi thao tác trên màn hình
    // thành vuốt/chạm một ô. Hình dáng, màu, thứ tự vẽ nằm trong prefab; lớp này chỉ đặt chúng vào đúng ô.
    public sealed class BoardView : MonoBehaviour, IBoardView
    {
        [SerializeField] private BoardArt art;
        [SerializeField] private PieceMotions motions;
        [SerializeField] private SwipeInput swipeInput;
        [SerializeField] private CameraFraming cameraFraming;
        [SerializeField] private BoardEffects effects;
        [SerializeField] private SoundPlayer sounds;

        [Header("Prefab")]
        [Tooltip("Khung gỗ liền một khối, tự vẽ theo hình dạng từng level.")]
        [SerializeField] private BoardFrame framePrefab;
        [SerializeField] private Transform tilePrefab;
        [SerializeField] private Transform toxicTilePrefab;
        [SerializeField] private SpriteRenderer boxPrefab;
        [Tooltip("Màng cho đi qua sang phải; hướng khác xoay theo.")]
        [SerializeField] private Transform membranePrefab;
        [SerializeField] private PieceView piecePrefab;
        [SerializeField] private Transform highlightPrefab;
        [Tooltip("Mũi tên gợi ý chỉ sang phải; hướng khác xoay theo.")]
        [SerializeField] private Transform hintArrowPrefab;
        [SerializeField] private TutorialHand tutorialHandPrefab;

        [Header("Rung bàn")]
        [SerializeField] private float shakeSeconds = 0.4f;
        [SerializeField] private AnimationCurve shakeX = PieceMotion.Keys(0f, 0f, 0.2f, -0.078f, 0.4f, 0.067f, 0.6f, -0.044f, 0.8f, 0.033f, 1f, 0f);
        [SerializeField] private AnimationCurve shakeY = PieceMotion.Keys(0f, 0f, 0.2f, 0.022f, 0.4f, -0.033f, 0.6f, 0.022f, 0.8f, -0.011f, 1f, 0f);

        private readonly Dictionary<int, PieceView> pieceViews = new Dictionary<int, PieceView>();
        private readonly List<Transform> highlights = new List<Transform>();
        private readonly List<GameObject> tutorialParts = new List<GameObject>();
        private MovePlayback playback;
        private Transform boardRoot;
        private Transform pieceRoot;
        private Transform hintArrow;
        private PieceView leaningView;
        private BoardLayout layout;
        private Level level;
        private Board currentBoard;

        // Kéo bao xa (tính bằng ô) thì quân nghiêng hết cỡ.
        private const float LeanFullDrag = 0.45f;

        public event Action<int, Direction> CellSwiped;

        public event Action<int> CellTapped;

        public bool IdleEnabled { get; set; }

        public void Build(Level newLevel)
        {
            if (boardRoot != null) Destroy(boardRoot.gameObject);
            StopAllCoroutines();
            pieceViews.Clear();
            highlights.Clear();
            tutorialParts.Clear();
            level = newLevel;
            layout = new BoardLayout(level);
            boardRoot = CreateGroup(transform, "Board");
            Instantiate(framePrefab, boardRoot).Build(level);
            var cellRoot = CreateGroup(boardRoot, "Cells");
            pieceRoot = CreateGroup(boardRoot, "Pieces");

            for (var cell = 0; cell < level.CellCount; cell++)
            {
                if (level.IsSlot(cell)) BuildCell(cell, cellRoot);
            }
            foreach (var membrane in level.Membranes) BuildMembrane(membrane, cellRoot);

            hintArrow = Instantiate(hintArrowPrefab, boardRoot);
            hintArrow.gameObject.SetActive(false);
            cameraFraming.Frame(layout.Bounds);
        }

        public void Show(Board board)
        {
            foreach (var view in pieceViews.Values) view.ClearMood();
            ShowPieces(board, null);
        }

        public void Play(MoveResult move, Action finished, bool lingerOnClear = false) =>
            StartCoroutine(playback.Run(move, finished, lingerOnClear));

        public void Refuse(int cell)
        {
            if (!(currentBoard[cell] is Piece piece))
            {
                sounds.Play(SoundId.AngryMeow);
                return;
            }
            PieceViewOf(piece.Id)?.PlayMotion(motions.Nope);
            if (piece.IsVirus) sounds.Play(SoundId.Grumble);
            else if (!piece.IsCat) sounds.Play(SoundId.Squish);
            else sounds.Play(SoundId.AngryMeow);
        }

        public void Pet(int cell)
        {
            if (!(currentBoard[cell] is Piece piece)) return;
            var view = PieceViewOf(piece.Id);
            if (view == null) return;
            if (piece.IsVirus) Refuse(cell);
            else if (piece.IsAsleep) view.PlayMotion(motions.Nope);
            else
            {
                view.PlayMotion(motions.Pet);
                view.ShowFaceFor(FaceKind.Blink, 0.5f);
                if (piece.IsCat) sounds.PlayMeow(piece.Species, 0.47f, 1.12f);
                else sounds.Play(SoundId.Squish);
            }
        }

        public void Celebrate()
        {
            foreach (var view in pieceViews.Values.Where(view => view.Piece.IsCat))
            {
                view.Show(view.Piece, art, true);
                view.PlayMotion(motions.Hop);
            }
            effects.Confetti();
            sounds.Play(SoundId.Win);
        }

        public void Mourn()
        {
            foreach (var view in pieceViews.Values) view.ShowSadMood();
            Shake();
            sounds.Play(SoundId.Lose);
        }

        public void PlayRevive()
        {
            effects.Glow();
            sounds.Play(SoundId.Revive);
        }

        public void Highlight(IEnumerable<int> cells)
        {
            foreach (var marker in highlights) Destroy(marker.gameObject);
            highlights.Clear();
            foreach (var cell in cells)
            {
                var marker = Instantiate(highlightPrefab, boardRoot);
                marker.localPosition = layout.CellCenter(cell);
                highlights.Add(marker);
            }
        }

        public void ShowHint(int cell, Direction direction)
        {
            hintArrow.localPosition = layout.CellCenter(cell);
            hintArrow.localRotation = Quaternion.Euler(0f, 0f, AngleOf(direction));
            hintArrow.gameObject.SetActive(true);
        }

        public void HideHint()
        {
            if (hintArrow != null) hintArrow.gameObject.SetActive(false);
        }

        public void ShowTutorial(int cell, Direction direction)
        {
            HideTutorial();
            var target = level.Step(cell, direction);
            // Vòng sáng nhấp nhẹ quanh ô cần vuốt và ô đích, không che tối các ô khác.
            foreach (var marked in new[] { cell, target })
            {
                if (marked == Level.NoCell) continue;
                var ring = Instantiate(highlightPrefab, boardRoot);
                ring.localPosition = layout.CellCenter(marked);
                tutorialParts.Add(ring.gameObject);
            }
            var hand = Instantiate(tutorialHandPrefab, boardRoot);
            hand.Swipe(layout.CellCenter(cell), target == Level.NoCell ? layout.CellCenter(cell) : layout.CellCenter(target));
            tutorialParts.Add(hand.gameObject);
        }

        public void HideTutorial()
        {
            foreach (var part in tutorialParts) Destroy(part);
            tutorialParts.Clear();
        }

        // Prefab vẽ cho hướng phải; Unity quay ngược chiều kim đồng hồ.
        public static float AngleOf(Direction direction) => direction switch
        {
            Direction.Up => 90f,
            Direction.Left => 180f,
            Direction.Down => 270f,
            _ => 0f,
        };

        internal Vector3 CellPosition(int cell) => boardRoot.TransformPoint(layout.CellCenter(cell));

        // Vùng các ô của bàn trong không gian thế giới, chưa tính viền gỗ.
        internal Bounds PlayArea => new Bounds(boardRoot.TransformPoint(layout.Bounds.center), layout.Bounds.size);

        internal PieceView PieceViewOf(int pieceId) => pieceViews.TryGetValue(pieceId, out var view) ? view : null;

        // Quân đang đứng ở ô `cell` theo bàn đang hiện.
        internal PieceView PieceAtCell(int cell) => currentBoard[cell] is Piece piece ? PieceViewOf(piece.Id) : null;

        // Lấy PieceView ra khỏi bàn để diễn nốt rồi tự xoá; bàn không quản lý nó nữa.
        internal PieceView TakePieceView(int pieceId)
        {
            if (!pieceViews.TryGetValue(pieceId, out var view)) return null;
            pieceViews.Remove(pieceId);
            return view;
        }

        internal void Shake() => StartCoroutine(RunShake());

        // Mèo đang thức, chưa làm gì, để chọn làm động tác lúc rảnh.
        internal PieceView RandomRestingCat()
        {
            var cats = pieceViews.Values.Where(view => view.IsResting && view.Piece.IsCat && !view.Piece.IsAsleep).ToList();
            return cats.Count == 0 ? null : cats[UnityEngine.Random.Range(0, cats.Count)];
        }

        // Giữ một PieceView cho mỗi id quân: quân cũ trượt tới ô mới, quân mới mọc lên, quân mất thì xoá.
        // Mèo vừa nhân đôi mọc ra từ ô của mèo mẹ.
        internal void ShowPieces(Board board, MoveResult move)
        {
            currentBoard = board;
            var newPieceId = move?.Placed[move.PlacedCell]?.Id;
            var shownIds = new HashSet<int>();
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (!(board[cell] is Piece piece)) continue;
                shownIds.Add(piece.Id);
                var position = layout.CellCenter(cell);
                if (!pieceViews.TryGetValue(piece.Id, out var view))
                {
                    view = Instantiate(piecePrefab, pieceRoot);
                    view.Place(piece.Id == newPieceId ? layout.CellCenter(move.SourceCell) : position);
                    view.PlayMotion(motions.Born);
                    pieceViews.Add(piece.Id, view);
                }
                view.Show(piece, art, piece.IsCat && level.TargetAt(cell) == piece.Species);
                view.MoveTo(position);
            }

            foreach (var id in pieceViews.Keys.Where(id => !shownIds.Contains(id)).ToList())
            {
                Destroy(pieceViews[id].gameObject);
                pieceViews.Remove(id);
            }
        }

        private void Awake() => playback = new MovePlayback(this, effects, sounds, motions, art);

        private void OnEnable()
        {
            swipeInput.Swiped += OnSwiped;
            swipeInput.Tapped += OnTapped;
            swipeInput.Dragged += OnDragged;
            swipeInput.DragEnded += OnDragEnded;
        }

        private void OnDisable()
        {
            swipeInput.Swiped -= OnSwiped;
            swipeInput.Tapped -= OnTapped;
            swipeInput.Dragged -= OnDragged;
            swipeInput.DragEnded -= OnDragEnded;
            OnDragEnded();
        }

        private void OnSwiped(Vector2 worldPoint, Direction direction)
        {
            if (TryGetCell(worldPoint, out var cell)) CellSwiped?.Invoke(cell, direction);
        }

        // Đang kéo: quân dưới ngón tay nghiêng theo hướng kéo (theo trục kéo nhiều hơn), chưa tính nước đi.
        private void OnDragged(Vector2 worldPoint, Vector2 drag)
        {
            var piece = TryGetCell(worldPoint, out var cell) && currentBoard != null ? currentBoard[cell] : null;
            var view = piece.HasValue ? PieceViewOf(piece.Value.Id) : null;
            if (view != leaningView) OnDragEnded();
            leaningView = view;
            if (view == null) return;
            var axis = Mathf.Abs(drag.x) > Mathf.Abs(drag.y) ? new Vector2(Mathf.Sign(drag.x), 0f) : new Vector2(0f, Mathf.Sign(drag.y));
            view.Lean(axis * Mathf.Clamp01(drag.magnitude / LeanFullDrag));
        }

        private void OnDragEnded()
        {
            if (leaningView != null) leaningView.Lean(Vector2.zero);
            leaningView = null;
        }

        private void OnTapped(Vector2 worldPoint)
        {
            if (TryGetCell(worldPoint, out var cell)) CellTapped?.Invoke(cell);
        }

        private bool TryGetCell(Vector2 worldPoint, out int cell)
        {
            cell = Level.NoCell;
            return layout != null && layout.TryGetCell(transform.InverseTransformPoint(worldPoint), out cell);
        }

        private IEnumerator RunShake()
        {
            for (var time = 0f; time < shakeSeconds; time += Time.deltaTime)
            {
                var progress = time / shakeSeconds;
                boardRoot.localPosition = new Vector3(shakeX.Evaluate(progress), shakeY.Evaluate(progress), 0f);
                yield return null;
            }
            boardRoot.localPosition = Vector3.zero;
        }

        // Mỗi ô: ô gạch hoặc ô độc và hộp mục tiêu (khung gỗ là một khối riêng, vẽ bởi BoardFrame).
        private void BuildCell(int cell, Transform cellRoot)
        {
            var center = layout.CellCenter(cell);
            Instantiate(level.IsToxic(cell) ? toxicTilePrefab : tilePrefab, cellRoot).localPosition = center;

            var target = level.TargetAt(cell);
            if (!target.HasValue) return;
            var box = Instantiate(boxPrefab, cellRoot);
            box.transform.localPosition = center;
            box.sprite = art.BoxSprite(target.Value);
        }

        // Màng nằm trên cạnh giữa hai ô; mũi tên chỉ chiều được đi qua.
        private void BuildMembrane(Membrane membrane, Transform parent)
        {
            var cell = level.IndexOf(membrane.Row, membrane.Column);
            var view = Instantiate(membranePrefab, parent);
            view.localPosition = layout.CellCenter(cell) + BoardLayout.Offset(membrane.Direction) * 0.5f;
            view.localRotation = Quaternion.Euler(0f, 0f, AngleOf(membrane.Direction));
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }
    }
}
