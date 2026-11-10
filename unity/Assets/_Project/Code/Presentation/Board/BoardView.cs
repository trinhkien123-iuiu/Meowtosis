using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;

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
        [SerializeField] private BoardCollapse collapse;

        [Header("Prefab")]
        [Tooltip("Một khối gỗ 3D cho mỗi ô. SortingGroup trên prefab được cộng thêm số hàng, để khối hàng dưới che mặt trước khối hàng trên.")]
        [SerializeField] private Transform tilePrefab;
        [SerializeField] private Transform toxicTilePrefab;
        [SerializeField] private Transform iceTilePrefab;
        [SerializeField] private SpriteRenderer boxPrefab;
        [Tooltip("Màng cho đi qua sang phải; hướng khác xoay theo.")]
        [SerializeField] private Transform membranePrefab;
        [Tooltip("Một đầu cổng dịch chuyển, dựng sẵn theo hướng ra ngoài là +X; hướng khác xoay theo.")]
        [SerializeField] private PortalGateView portalGatePrefab;
        [Tooltip("Màu từng cặp cổng, theo thứ tự trong level.")]
        [SerializeField] private Color[] portalColors = { new Color(0.72f, 0.48f, 1f), new Color(1f, 0.48f, 0.78f), new Color(0.22f, 0.85f, 0.78f) };
        [SerializeField] private PieceView piecePrefab;
        [Tooltip("Vòng ma thuật đánh dấu ô (MagicCircle).")]
        [SerializeField] private MagicCircle highlightPrefab;
        [Tooltip("Ô có mèo: vòng nằm dưới chân mèo, thấp hơn tâm ô chừng này (tính bằng ô).")]
        [SerializeField] private float highlightFeetDrop = 0.27f;
        [Tooltip("Ô trống: vòng nằm giữa mặt ô gỗ, thấp hơn tâm ô chừng này.")]
        [SerializeField] private float highlightEmptyDrop = 0.06f;
        [Tooltip("Mũi tên gợi ý chỉ sang phải; hướng khác xoay theo.")]
        [SerializeField] private Transform hintArrowPrefab;
        [SerializeField] private TutorialHand tutorialHandPrefab;

        [Header("Làm tối khi hướng dẫn")]
        [Tooltip("Lớp tối phủ cả màn hình (SpriteRenderer, Mask Interaction = Visible Outside Mask).")]
        [SerializeField] private Transform focusShadePrefab;
        [Tooltip("Lỗ sáng trên một ô: SpriteMask theo hình ô, khoét lớp tối.")]
        [SerializeField] private Transform focusWindowPrefab;

        [Header("Rung bàn")]
        [SerializeField] private float shakeSeconds = 0.4f;
        [SerializeField] private AnimationCurve shakeX = PieceMotion.Keys(0f, 0f, 0.2f, -0.078f, 0.4f, 0.067f, 0.6f, -0.044f, 0.8f, 0.033f, 1f, 0f);
        [SerializeField] private AnimationCurve shakeY = PieceMotion.Keys(0f, 0f, 0.2f, 0.022f, 0.4f, -0.033f, 0.6f, 0.022f, 0.8f, -0.011f, 1f, 0f);

        private readonly Dictionary<int, PieceView> pieceViews = new Dictionary<int, PieceView>();
        private readonly List<Transform> highlights = new List<Transform>();
        private readonly List<GameObject> tutorialParts = new List<GameObject>();
        private readonly List<GameObject> focusParts = new List<GameObject>();
        private readonly Dictionary<int, Transform> tiles = new Dictionary<int, Transform>();
        // Vật gắn với một ô nhưng không phải quân: hộp mục tiêu và màng ngăn. Rơi cùng khối gỗ của ô khi thắng.
        private readonly Dictionary<int, List<Transform>> cellExtras = new Dictionary<int, List<Transform>>();
        // Các đầu cổng theo khoá GateKey(ô, hướng đi ra), và ô của từng quân ở lần hiển thị trước (nhận ra quân vừa đi xuyên cổng).
        private readonly Dictionary<int, PortalGateView> gates = new Dictionary<int, PortalGateView>();
        private readonly Dictionary<int, int> pieceCells = new Dictionary<int, int>();
        private MovePlayback playback;
        private Transform boardRoot;
        private Transform pieceRoot;
        private Transform hintArrow;
        private PieceView leaningView;
        // Các con đang được báo trước lúc kéo: bị đẩy (nghiêng theo) hoặc bị đè (mờ đi).
        private readonly List<PieceView> previewedViews = new List<PieceView>();
        // Nước đang kéo đã tính báo trước rồi: cùng ô, cùng hướng, cùng bàn thì khỏi tính lại mỗi khung hình (TrySwipe tốn
        // cấp phát vì dựng cả bàn mới và chạy hết các bước của lượt).
        private int previewedCell = Level.NoCell;
        private Direction previewedDirection;
        private Board previewedBoard;
        private PieceView highlightedPiece;
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
            focusParts.Clear();
            tiles.Clear();
            cellExtras.Clear();
            gates.Clear();
            pieceCells.Clear();
            highlightedPiece = null;
            level = newLevel;
            layout = new BoardLayout(level);
            boardRoot = CreateGroup(transform, "Board");
            var cellRoot = CreateGroup(boardRoot, "Cells");
            pieceRoot = CreateGroup(boardRoot, "Pieces");

            for (var cell = 0; cell < level.CellCount; cell++)
            {
                if (level.IsSlot(cell)) BuildCell(cell, cellRoot);
            }
            foreach (var membrane in level.Membranes) BuildMembrane(membrane, cellRoot);
            BuildPortals(cellRoot);

            hintArrow = Instantiate(hintArrowPrefab, boardRoot);
            hintArrow.gameObject.SetActive(false);
            ClearSpotlight();
            HideTutorial();
            cameraFraming.Frame(layout.Bounds);
        }

        public void Show(Board board)
        {
            ClearSpotlight();
            HideTutorial();
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
            sounds.Play(piece.IsVirus ? SoundId.Grumble : SoundId.AngryMeow);
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
                sounds.PlayMeow(piece.Species, 0.47f, 1.12f);
            }
        }

        public void Celebrate(Action finished)
        {
            // Nước lấp ô cuối: bàn khựng một nhịp rồi các chú mèo nhảy lên lần lượt như một làn sóng, mỗi con tung hạt sáng.
            HitStop(0.09f);
            var wave = 0;
            foreach (var view in pieceViews.Values.Where(view => view.Piece.IsCat).OrderBy(view => view.transform.localPosition.x + view.transform.localPosition.y))
            {
                view.Show(view.Piece, art, true);
                var delay = wave++ * 0.06f;
                view.PlayMotion(motions.Hop, default, delay);
                StartCoroutine(SparkleLater(view, delay));
            }
            sounds.Play(SoundId.Win);
            foreach (var gate in gates.Values) gate.Close();
            HideHint();
            ClearSpotlight();
            HideTutorial();
            StartCoroutine(collapse.Run(CollapseOrder(), finished));
        }

        // Khối hàng dưới (gần người chơi) rơi trước, trong hàng từ trái sang phải; quân đứng trên khối rơi theo.
        private List<BoardCollapse.Column> CollapseOrder()
        {
            var columns = new List<BoardCollapse.Column>();
            for (var row = level.Height - 1; row >= 0; row--)
            {
                for (var column = 0; column < level.Width; column++)
                {
                    var cell = level.IndexOf(row, column);
                    if (!tiles.TryGetValue(cell, out var tile)) continue;
                    var pieces = new List<Transform>();
                    var view = PieceAtCell(cell);
                    if (view != null) pieces.Add(view.transform);
                    cellExtras.TryGetValue(cell, out var extras);
                    columns.Add(new BoardCollapse.Column(tile, pieces, extras ?? new List<Transform>()));
                }
            }
            return columns;
        }

        private IEnumerator SparkleLater(PieceView view, float delay)
        {
            yield return new WaitForSeconds(delay + 0.1f);
            if (view != null) effects.Sparkle(view.transform.position);
        }

        private float lastMournTime = -100f;

        // Khóc, rung bàn, tiếng thua. Cảnh thua có thể gọi từ hai nơi (diễn hoạt khử 3 và lúc nước đi kết thúc): chỉ chạy một lần.
        public void Mourn()
        {
            if (Time.unscaledTime - lastMournTime < 3f) return;
            lastMournTime = Time.unscaledTime;
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
                var marker = Instantiate(highlightPrefab, boardRoot).transform;
                marker.localPosition = HighlightSpot(cell);
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
            if (target == Level.NoCell) return;

            if (tutorialHandPrefab != null)
            {
                var hand = Instantiate(tutorialHandPrefab, boardRoot);
                hand.Swipe(layout.CellCenter(cell), layout.CellCenter(target));
                tutorialParts.Add(hand.gameObject);
            }

            // Onboarding trực quan bằng hình ảnh: bóng mờ lướt từ ô gốc sang ô đích mô phỏng động tác vuốt
            if (currentBoard != null && currentBoard[cell] is Piece piece)
            {
                var ghostObj = new GameObject("GhostPreview");
                ghostObj.transform.SetParent(boardRoot, false);

                var sprite = piece.IsCat ? art.CatSprite(piece.Species, false) : art.VirusSprite(piece.Facing);
                var width = piece.IsCat ? 0.778f : 1.5f;
                var lift = piece.IsCat ? 0.011f : 0f;

                var landing = LandingOf(target, direction);
                var ghost = ghostObj.AddComponent<GhostPiecePreview>();
                ghost.Init(layout.CellCenter(cell), layout.CellCenter(landing), sprite, width, lift, 15);
                tutorialParts.Add(ghostObj);

                effects.Sparkle(boardRoot.TransformPoint(layout.CellCenter(landing)));
            }
        }

        // Ô bản sao sẽ dừng khi vào ô `target`: trống và là băng thì trượt tiếp như SwipeRule, để bóng mờ chỉ đúng chỗ.
        private int LandingOf(int target, Direction direction)
        {
            if (currentBoard[target].HasValue) return target;
            var at = target;
            while (level.IsIce(at))
            {
                var next = level.Step(at, direction);
                if (next == Level.NoCell || currentBoard[next].HasValue) break;
                at = next;
            }
            return at;
        }

        public void HideTutorial()
        {
            foreach (var part in tutorialParts)
            {
                if (part != null) Destroy(part);
            }
            tutorialParts.Clear();
        }

        public void Focus(IEnumerable<int> cells)
        {
            ClearFocus();
            focusParts.Add(Instantiate(focusShadePrefab, boardRoot).gameObject);
            foreach (var cell in cells)
            {
                var window = Instantiate(focusWindowPrefab, boardRoot);
                window.localPosition = layout.CellCenter(cell);
                focusParts.Add(window.gameObject);
            }
        }

        public void ClearFocus()
        {
            foreach (var part in focusParts)
            {
                if (part != null) Destroy(part);
            }
            focusParts.Clear();
        }

        public void Spotlight(int cell)
        {
            ClearSpotlight();
            Focus(new[] { cell });
            highlightedPiece = PieceAtCell(cell);
            if (highlightedPiece != null)
            {
                highlightedPiece.SetHighlightOrder(true);
                highlightedPiece.PlayMotion(motions.Hop);
            }
            effects.Sparkle(boardRoot.TransformPoint(layout.CellCenter(cell)));
        }

        public void ClearSpotlight()
        {
            if (highlightedPiece != null)
            {
                highlightedPiece.SetHighlightOrder(false);
                highlightedPiece = null;
            }
            ClearFocus();
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

        public Vector2 ScreenPointOf(int cell) => Camera.main.WorldToScreenPoint(CellPosition(cell));

        // Chỗ đặt vòng ma thuật: dưới chân mèo nếu ô có quân, giữa mặt ô nếu ô trống.
        private Vector3 HighlightSpot(int cell)
        {
            var hasPiece = currentBoard != null && currentBoard[cell].HasValue;
            return layout.CellCenter(cell) + Vector3.down * (hasPiece ? highlightFeetDrop : highlightEmptyDrop);
        }

        // Vòng ma thuật loé một lần ở ô (booster vừa tác động).
        internal void MagicFlare(int cell, Color color)
        {
            var flare = Instantiate(highlightPrefab, boardRoot);
            flare.transform.localPosition = layout.CellCenter(cell) + Vector3.down * highlightEmptyDrop;
            flare.Flare(color);
        }

        // Cho quân lướt chậm tới ô `cell` trong `seconds` giây (trượt trên băng).
        internal void SlidePieceTo(PieceView view, int cell, float seconds) => view.SlideTo(layout.CellCenter(cell), seconds);

        // Mép trên của khối gỗ ở ô này trong không gian thế giới (không tính bóng đổ); ô không có khối thì lấy mép trên của ô.
        internal float TileTop(int cell)
        {
            var top = CellPosition(cell).y + 0.5f;
            if (!tiles.TryGetValue(cell, out var tile)) return top;
            var found = false;
            foreach (var renderer in tile.GetComponentsInChildren<SpriteRenderer>())
            {
                if (renderer.name == "Shadow") continue;
                top = found ? Mathf.Max(top, renderer.bounds.max.y) : renderer.bounds.max.y;
                found = true;
            }
            return top;
        }

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

        // `strength` 1 là rung đầy đủ (thua, nổ); khử 3 an toàn chỉ rung nhẹ. Chế độ giảm chuyển động thì không rung.
        internal void Shake(float strength = 1f)
        {
            if (!MotionSettings.Reduced) StartCoroutine(RunShake(strength));
        }

        private float slowScale = 1f;
        private float slowUntil;
        private float hitStopUntil;
        private Coroutine timeRoutine;

        // Làm chậm thời gian trong `seconds` giây thật (không bị chính nó kéo dài), để người chơi kịp nhìn thấy vì sao thua.
        internal void SlowMotion(float timeScale, float seconds)
        {
            if (MotionSettings.Reduced) return;
            slowScale = timeScale;
            slowUntil = Time.unscaledTime + seconds;
            EnsureTimeRoutine();
        }

        // Đứng hình thoáng chốc ở khoảnh khắc va chạm mạnh (chuỗi dài, thắng): thời gian gần như dừng trong `seconds` giây thật.
        internal void HitStop(float seconds)
        {
            if (MotionSettings.Reduced) return;
            hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + seconds);
            EnsureTimeRoutine();
        }

        private void EnsureTimeRoutine()
        {
            if (timeRoutine == null) timeRoutine = StartCoroutine(RunTimeScale());
        }

        // Một nơi duy nhất đặt Time.timeScale để dừng hình và làm chậm không giẫm lên nhau: dừng hình thắng làm chậm, làm chậm thắng 1.
        private IEnumerator RunTimeScale()
        {
            while (Time.unscaledTime < Mathf.Max(slowUntil, hitStopUntil))
            {
                Time.timeScale = Time.unscaledTime < hitStopUntil ? 0.03f : slowScale;
                yield return null;
            }
            Time.timeScale = 1f;
            timeRoutine = null;
        }

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
            var portalUsed = false;
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (!(board[cell] is Piece piece)) continue;
                shownIds.Add(piece.Id);
                var position = layout.CellCenter(cell);
                if (!pieceViews.TryGetValue(piece.Id, out var view))
                {
                    view = Instantiate(piecePrefab, pieceRoot);
                    var isCopy = piece.Id == newPieceId;
                    var startCell = isCopy ? move.SourceCell : cell;
                    view.Place(layout.CellCenter(startCell));
                    if (isCopy && move.SwipeDirection.HasValue) TryTeleport(startCell, move.SwipeDirection.Value, view, Level.NoCell, ref portalUsed);
                    view.PlayMotion(motions.Born);
                    pieceViews.Add(piece.Id, view);
                }
                else if (move != null && move.SwipeDirection.HasValue && pieceCells.TryGetValue(piece.Id, out var previousCell) && previousCell != cell)
                {
                    // Quân bị đẩy xuyên cổng: hiện ra từ đầu cổng bên kia thay vì trượt ngang qua cả bàn.
                    TryTeleport(previousCell, move.SwipeDirection.Value, view, cell, ref portalUsed);
                }
                view.Show(piece, art, piece.IsCat && level.TargetAt(cell) == piece.Species);
                view.MoveTo(position);
            }
            pieceCells.Clear();
            for (var cell = 0; cell < board.CellCount; cell++)
            {
                if (board[cell] is Piece placed) pieceCells[placed.Id] = cell;
            }
            if (portalUsed) sounds.Play(SoundId.Portal);

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
            Time.timeScale = 1f;
            timeRoutine = null;
            swipeInput.Swiped -= OnSwiped;
            swipeInput.Tapped -= OnTapped;
            swipeInput.Dragged -= OnDragged;
            swipeInput.DragEnded -= OnDragEnded;
            OnDragEnded();
        }

        private void OnSwiped(Vector2 worldPoint, Direction direction)
        {
            ClearPreviewedViews();
            if (TryGetCell(worldPoint, out var cell)) CellSwiped?.Invoke(cell, direction);
        }

        private readonly GameRules rules = GameRules.CreateStandard();
        private readonly HashSet<int> endangeredPieceIds = new HashSet<int>();

        // Đang kéo: quân dưới ngón tay nghiêng nhẹ theo hướng kéo, vẫn gọn trong ô; kéo đủ xa thì báo trước kết quả
        // (dãy bị đẩy nghiêng theo, con sắp bị đè mờ đi) và cảnh báo nếu nước đó làm mất hết một màu.
        private void OnDragged(Vector2 worldPoint, Vector2 drag)
        {
            var piece = TryGetCell(worldPoint, out var cell) && currentBoard != null ? currentBoard[cell] : null;
            var view = piece.HasValue ? PieceViewOf(piece.Value.Id) : null;
            if (view != leaningView) OnDragEnded();
            leaningView = view;
            if (view == null) return;
            var axis = Mathf.Abs(drag.x) > Mathf.Abs(drag.y) ? new Vector2(Mathf.Sign(drag.x), 0f) : new Vector2(0f, Mathf.Sign(drag.y));
            view.Lean(axis * Mathf.Clamp01(drag.magnitude / LeanFullDrag));

            // Cảnh báo nguy hiểm trực quan (Telegraphed Danger):
            // Nếu người chơi kéo đủ một khoảng (>= 0.18 ô), dự đoán xem hướng vuốt này có dẫn đến tuyệt chủng loài nào không.
            if (drag.magnitude >= 0.18f && currentBoard != null)
            {
                var isHorizontal = Mathf.Abs(drag.x) >= Mathf.Abs(drag.y);
                var dir = isHorizontal
                    ? (drag.x > 0f ? Direction.Right : Direction.Left)
                    : (drag.y > 0f ? Direction.Up : Direction.Down);

                if (cell == previewedCell && dir == previewedDirection && ReferenceEquals(currentBoard, previewedBoard)) return;
                if (piece.Value.CanSwipe && rules.Swipes.TrySwipe(currentBoard, cell, dir, out var previewMove))
                {
                    ShowDragPreview(previewMove, dir);
                    previewedCell = cell;
                    previewedDirection = dir;
                    previewedBoard = currentBoard;
                    var loss = LossReport.Of(previewMove);
                    if (loss != null)
                    {
                        SetEndangeredWarning(loss.Color);
                        return;
                    }
                }
                else ClearPreviewedViews();
            }
            else ClearPreviewedViews();
            ClearEndangeredWarning();
        }

        private void SetEndangeredWarning(Species color)
        {
            if (currentBoard == null) return;
            var toWarn = new HashSet<int>();
            for (var i = 0; i < currentBoard.CellCount; i++)
            {
                if (currentBoard[i] is Piece p && p.IsCat && p.Species == color)
                {
                    toWarn.Add(p.Id);
                }
            }

            if (endangeredPieceIds.SetEquals(toWarn)) return;

            ClearEndangeredWarning();
            foreach (var id in toWarn)
            {
                var pv = PieceViewOf(id);
                if (pv != null)
                {
                    pv.ShowSadMood();
                    endangeredPieceIds.Add(id);
                }
            }
        }

        private void ClearEndangeredWarning()
        {
            if (endangeredPieceIds.Count == 0) return;
            foreach (var id in endangeredPieceIds)
            {
                var pv = PieceViewOf(id);
                pv?.ClearMood();
            }
            endangeredPieceIds.Clear();
        }

        // Báo trước kết quả nước đang kéo: đẩy thì cả dãy bị đẩy nghiêng theo hướng; đè thì con sắp bị đè mờ đi và run.
        private void ShowDragPreview(MoveResult preview, Direction direction)
        {
            ClearPreviewedViews();
            if (preview.Replaced is Piece replaced)
            {
                var victim = PieceViewOf(replaced.Id);
                if (victim != null)
                {
                    victim.ShowDoomed(true);
                    previewedViews.Add(victim);
                }
                return;
            }
            if (preview.Pushes.Count > 0)
            {
                var axis = (Vector2)BoardLayout.Offset(direction);
                foreach (var shift in preview.Pushes)
                {
                    var pushed = preview.Before[shift.From] is Piece moved ? PieceViewOf(moved.Id) : null;
                    if (pushed == null) continue;
                    pushed.Lean(axis * 0.7f);
                    previewedViews.Add(pushed);
                }
            }
        }

        private void ClearPreviewedViews()
        {
            previewedCell = Level.NoCell;
            previewedBoard = null;
            foreach (var view in previewedViews)
            {
                if (view == null) continue;
                view.ShowDoomed(false);
                if (view != leaningView) view.Lean(Vector2.zero);
            }
            previewedViews.Clear();
        }

        private void OnDragEnded()
        {
            ClearPreviewedViews();
            if (leaningView != null) leaningView.Lean(Vector2.zero);
            leaningView = null;
            ClearEndangeredWarning();
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

        private IEnumerator RunShake(float strength)
        {
            for (var time = 0f; time < shakeSeconds; time += Time.deltaTime)
            {
                var progress = time / shakeSeconds;
                boardRoot.localPosition = new Vector3(shakeX.Evaluate(progress), shakeY.Evaluate(progress), 0f) * strength;
                yield return null;
            }
            boardRoot.localPosition = Vector3.zero;
        }

        // Mỗi ô: một khối gỗ (hoặc khối độc, khối băng) và hộp mục tiêu. Không có khung bao quanh: các khối đứng sát nhau thành hình bàn.
        private void BuildCell(int cell, Transform cellRoot)
        {
            var center = layout.CellCenter(cell);
            var tile = Instantiate(level.IsToxic(cell) ? toxicTilePrefab : level.IsIce(cell) ? iceTilePrefab : tilePrefab, cellRoot);
            tile.localPosition = center;
            var row = cell / level.Width;
            foreach (var group in tile.GetComponentsInChildren<SortingGroup>()) group.sortingOrder += row;
            tiles[cell] = tile;

            var target = level.TargetAt(cell);
            if (!target.HasValue) return;
            var box = Instantiate(boxPrefab, cellRoot);
            box.transform.localPosition = center;
            box.sprite = art.BoxSprite(target.Value);
            AddExtra(cell, box.transform);
        }

        private void AddExtra(int cell, Transform extra)
        {
            if (!cellExtras.TryGetValue(cell, out var extras)) cellExtras[cell] = extras = new List<Transform>();
            extras.Add(extra);
        }

        // Mỗi cặp cổng dịch chuyển: hai đầu ở cạnh nhìn ra ngoài bàn của hai ô, cùng một màu, xoay theo hướng ra ngoài.
        private void BuildPortals(Transform parent)
        {
            if (portalGatePrefab == null) return;
            for (var index = 0; index < level.Portals.Count; index++)
            {
                var portal = level.Portals[index];
                var color = portalColors[index % portalColors.Length];
                AddGate(parent, level.IndexOf(portal.Row, portal.Column), portal.Direction, color);
                AddGate(parent, level.IndexOf(portal.TargetRow, portal.TargetColumn), portal.Direction.Opposite(), color);
            }
        }

        private void AddGate(Transform parent, int cell, Direction outward, Color color)
        {
            var gate = Instantiate(portalGatePrefab, parent);
            gate.transform.localPosition = layout.CellCenter(cell) + BoardLayout.Offset(outward) * 0.52f;
            gate.transform.localRotation = Quaternion.Euler(0f, 0f, AngleOf(outward));
            gate.SetColor(color);
            gates[GateKey(cell, outward)] = gate;
        }

        private static int GateKey(int cell, Direction direction) => cell * 4 + (int)direction;

        // Quân đi một bước từ ô `from` theo `direction` mà xuyên một cổng: hai đầu cổng sáng lên và quân hiện ra từ đầu cổng bên kia.
        // `arrival` là ô quân phải tới (bản sao thì Level.NoCell: lấy ô bên kia cổng). Trả false nếu bước đó không qua cổng.
        private bool TryTeleport(int from, Direction direction, PieceView view, int arrival, ref bool used)
        {
            if (!gates.TryGetValue(GateKey(from, direction), out var entry)) return false;
            var landing = level.Step(from, direction);
            if (landing == Level.NoCell || (arrival != Level.NoCell && landing != arrival)) return false;
            if (!gates.TryGetValue(GateKey(landing, direction.Opposite()), out var exit)) return false;

            view.Place(layout.CellCenter(landing) + BoardLayout.Offset(direction.Opposite()) * 0.7f);
            entry.Flash();
            exit.Flash();
            effects.PortalPulse(entry.transform.position, entry.Tint);
            effects.PortalPulse(exit.transform.position, exit.Tint);
            used = true;
            return true;
        }

        // Màng nằm trên cạnh giữa hai ô; mũi tên chỉ chiều được đi qua.
        private void BuildMembrane(Membrane membrane, Transform parent)
        {
            var cell = level.IndexOf(membrane.Row, membrane.Column);
            var view = Instantiate(membranePrefab, parent);
            view.localPosition = layout.CellCenter(cell) + BoardLayout.Offset(membrane.Direction) * 0.5f;
            view.localRotation = Quaternion.Euler(0f, 0f, AngleOf(membrane.Direction));
            AddExtra(cell, view);
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }
    }
}
