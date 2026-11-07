using System.Collections;
using System.Collections.Generic;
using Meowtosis.Domain;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Vài chú mèo ngóc đầu lên từ sau khối gỗ ở hàng trên cùng của bàn, nháy mắt rồi bật một trái tim hoặc ngôi sao, xong thụt xuống.
    // Đầu mèo vẽ sau khối gỗ (thứ tự vẽ thấp hơn), nên phần thân luôn bị chính khối đó che.
    public sealed class CatPeekEvent : AmbientEvent
    {
        private static readonly Species[] CatColors = { Species.Red, Species.Blue, Species.Yellow, Species.Pink, Species.Purple };

        [SerializeField] private Camera targetCamera;
        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardArt art;
        [SerializeField] private SoundPlayer sounds;
        [Tooltip("Đầu mèo, thứ tự vẽ phải thấp hơn khối gỗ (-20) và cao hơn bóng đổ của khối (-40) để phần thân bị khối che.")]
        [SerializeField] private SpriteRenderer headPrefab;
        [Tooltip("Hình bật lên khi nháy mắt, thứ tự vẽ cao hơn bàn.")]
        [SerializeField] private SpriteRenderer iconPrefab;
        [Tooltip("Các hình có thể bật lên (trái tim, ngôi sao).")]
        [SerializeField] private Sprite[] icons;

        [Header("Số lượng")]
        [SerializeField, Min(1)] private int maximumCats = 2;
        [Tooltip("Mèo sau ngóc lên trễ hơn mèo trước bao lâu.")]
        [SerializeField] private float catStagger = 0.35f;

        [Header("Vị trí")]
        [Tooltip("Đầu mèo ẩn thấp hơn mép trên khối gỗ bao nhiêu ô, để phần mép bo tròn của khối không lộ đầu lúc đang trốn.")]
        [SerializeField] private float rimInset = 0.04f;
        [SerializeField] private float headHeight = 0.95f;
        [Tooltip("Đầu mèo nhô lên khỏi mép bàn bao nhiêu phần chiều cao.")]
        [SerializeField, Range(0.2f, 1f)] private float headShown = 0.62f;
        [Tooltip("Phía trên mức này là HUD, tính theo chiều cao màn hình. Không đủ chỗ thì không ngóc lên.")]
        [SerializeField, Range(0f, 0.5f)] private float topHudHeight = 0.19f;
        [SerializeField] private float iconSize = 0.42f;
        [Tooltip("Vị trí hình bật lên so với tâm đầu mèo, tính bằng ô.")]
        [SerializeField] private Vector2 iconOffset = new Vector2(0.36f, 0.34f);

        [Header("Nhịp diễn, tính bằng giây")]
        [SerializeField] private float riseSeconds = 0.35f;
        [SerializeField] private float lookSeconds = 0.35f;
        [SerializeField] private float winkSeconds = 0.3f;
        [SerializeField] private float iconSeconds = 0.9f;
        [Tooltip("Hình bay lên bao xa, tính bằng ô.")]
        [SerializeField] private float iconRise = 0.4f;
        [SerializeField] private AnimationCurve iconPop = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.18f, 1.2f), new Keyframe(0.32f, 1f), new Keyframe(1f, 1f));
        [SerializeField, Range(0f, 1f)] private float meowLoudness = 0.3f;

        private readonly List<SpriteRenderer> heads = new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> shownIcons = new List<SpriteRenderer>();
        private int catsPlaying;

        public override bool IsPlaying => catsPlaying > 0;

        public override bool TryStart(Board board)
        {
            var columns = FindOpenCells(board);
            if (columns.Count == 0) return false;

            var count = Mathf.Min(Random.Range(1, maximumCats + 1), columns.Count);
            for (var index = 0; index < count; index++)
            {
                var pick = Random.Range(index, columns.Count);
                var cell = columns[pick];
                columns[pick] = columns[index];
                EnsureViews(index);
                catsPlaying++;
                StartCoroutine(RunCat(index, cell, index * catStagger));
            }
            return true;
        }

        public override void Stop()
        {
            StopAllCoroutines();
            catsPlaying = 0;
            foreach (var head in heads) head.gameObject.SetActive(false);
            foreach (var icon in shownIcons) icon.gameObject.SetActive(false);
        }

        private void EnsureViews(int index)
        {
            while (heads.Count <= index)
            {
                heads.Add(Instantiate(headPrefab, transform));
                shownIcons.Add(Instantiate(iconPrefab, transform));
            }
        }

        // Các ô trống ở mép ngoài cùng phía trên của bàn mà phía trên còn đủ chỗ cho đầu mèo (HUD không che).
        // Bỏ ô có quân (mèo ló sẽ chồng lên quân) và ô dưới một lỗ giữa bàn (mèo ló trong lỗ trông như một quân thật).
        private List<int> FindOpenCells(Board board)
        {
            var cells = new List<int>();
            var level = board.Level;
            var hudBottom = targetCamera.ViewportToWorldPoint(new Vector3(0f, 1f - topHudHeight, 0f)).y;
            for (var cell = 0; cell < level.CellCount; cell++)
            {
                if (!level.IsSlot(cell) || board[cell].HasValue || !IsOuterTop(level, cell)) continue;
                var rim = boardView.TileTop(cell) - rimInset;
                if (rim + headHeight * headShown <= hudBottom) cells.Add(cell);
            }
            return cells;
        }

        // Phía trên ô này trong cùng cột không còn ô nào của bàn.
        private static bool IsOuterTop(Level level, int cell)
        {
            for (var above = cell - level.Width; above >= 0; above -= level.Width)
            {
                if (level.IsSlot(above)) return false;
            }
            return true;
        }

        private IEnumerator RunCat(int index, int cell, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

            var head = heads[index];
            var species = CatColors[Random.Range(0, CatColors.Length)];
            var normalFace = art.CatSprite(species, false);
            head.sprite = normalFace;
            head.transform.localScale = Vector3.one * (headHeight / normalFace.bounds.size.y);

            var center = boardView.CellPosition(cell);
            var rim = boardView.TileTop(cell) - rimInset;
            var hidden = new Vector3(center.x, rim - headHeight * 0.5f, 0f);
            var shown = hidden + Vector3.up * (headHeight * headShown);
            head.gameObject.SetActive(true);
            yield return Slide(head.transform, hidden, shown);
            yield return new WaitForSecondsRealtime(lookSeconds);

            head.sprite = art.CatSprite(species, true);
            if (meowLoudness > 0f) sounds.PlayMeow(species, meowLoudness);
            yield return ShowIcon(shownIcons[index], shown, head, normalFace);

            yield return Slide(head.transform, shown, hidden);
            head.gameObject.SetActive(false);
            catsPlaying--;
        }

        // Hình bay lên trong lúc mèo nháy mắt; hết nhịp nháy thì mèo mở mắt lại, hình vẫn bay tiếp.
        private IEnumerator ShowIcon(SpriteRenderer icon, Vector3 headCenter, SpriteRenderer head, Sprite normalFace)
        {
            icon.sprite = icons[Random.Range(0, icons.Length)];
            var baseScale = iconSize / icon.sprite.bounds.size.y;
            var start = headCenter + (Vector3)iconOffset;
            var color = Color.white;
            icon.gameObject.SetActive(true);
            for (var time = 0f; time < iconSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / iconSeconds;
                icon.transform.position = start + Vector3.up * (iconRise * progress);
                icon.transform.localScale = Vector3.one * (baseScale * iconPop.Evaluate(progress));
                color.a = Mathf.Clamp01((1f - progress) * 3f);
                icon.color = color;
                if (time >= winkSeconds) head.sprite = normalFace;
                yield return null;
            }
            head.sprite = normalFace;
            icon.gameObject.SetActive(false);
        }

        private IEnumerator Slide(Transform target, Vector3 from, Vector3 to)
        {
            for (var time = 0f; time < riseSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / riseSeconds;
                target.position = Vector3.Lerp(from, to, progress * progress * (3f - 2f * progress));
                yield return null;
            }
            target.position = to;
        }
    }
}
