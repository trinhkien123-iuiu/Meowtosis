using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Hoạt cảnh thắng: các khối gỗ lần lượt rơi xuống hố (từ hàng trước ra hàng sau, khối sau bắt đầu khi khối trước
    // chưa rơi xong), mèo trên khối nhảy cẫng lên rồi rơi theo; hộp mục tiêu và màng ngăn của ô rơi cùng khối. Khối nhỏ dần và tối dần như rơi xa khỏi màn hình.
    // Mọi thông số chỉnh trong Inspector trên object Board.
    public sealed class BoardCollapse : MonoBehaviour
    {
        [Tooltip("Chờ mèo nhảy mừng bao lâu rồi mới bắt đầu rơi.")]
        [SerializeField] private float startDelay = 0.35f;
        [Tooltip("Khối sau bắt đầu rơi sau khối trước bao lâu (nhỏ hơn thời gian rơi thì các khối rơi chồng lên nhau).")]
        [SerializeField] private float stagger = 0.06f;
        [SerializeField, Min(0f)] private float maxStaggerSpan = 0.45f;
        [SerializeField] private WinPopup winPopup;
        private const float BackgroundTransitionSeconds = 0.26f;
        [SerializeField] private float tileFallSeconds = 0.7f;
        [Tooltip("Mèo rơi chậm hơn khối của nó bao lâu.")]
        [SerializeField] private float catDelay = 0.1f;
        [SerializeField] private float catFallSeconds = 0.85f;
        [Tooltip("Khối rơi xuống bao xa (đơn vị ô).")]
        [SerializeField] private float fallDistance = 2.2f;
        [Tooltip("Mèo nhảy lên bao cao trước khi rơi (đơn vị ô).")]
        [SerializeField] private float catJump = 0.45f;
        [SerializeField] private float endScale = 0.45f;
        [SerializeField] private float maxSpinDegrees = 18f;
        [Tooltip("Đường rơi theo thời gian: 0 là chỗ cũ, 1 là rơi hết fallDistance.")]
        [SerializeField] private AnimationCurve fallCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2.2f, 0f));
        [Tooltip("Mèo: lên rồi xuống, giá trị âm là đang nhảy lên.")]
        [SerializeField] private AnimationCurve catCurve = new AnimationCurve(new Keyframe(0f, 0f, -4f, -4f), new Keyframe(0.25f, -0.45f, 0f, 0f), new Keyframe(1f, 1f, 3f, 0f));
        [SerializeField] private Color fadeTo = new Color(0.05f, 0.04f, 0.12f, 0f);

        // Một khối, các quân đứng trên nó, và các vật gắn với ô (hộp mục tiêu, màng ngăn), xếp theo thứ tự rơi.
        public readonly struct Column
        {
            public Column(Transform tile, IReadOnlyList<Transform> pieces, IReadOnlyList<Transform> extras)
            {
                Tile = tile;
                Pieces = pieces;
                Extras = extras;
            }

            public Transform Tile { get; }

            public IReadOnlyList<Transform> Pieces { get; }

            // Rơi y như khối, nhưng co lại và xoay quanh tâm khối chứ không quanh chính nó.
            public IReadOnlyList<Transform> Extras { get; }
        }

        public IEnumerator Run(IReadOnlyList<Column> columns, Action finished)
        {
            yield return new WaitForSeconds(startDelay);
            var running = 0;
            var gap = Mathf.Min(Mathf.Max(0f, stagger), maxStaggerSpan / Mathf.Max(1, columns.Count - 1));
            var latestFallEnd = Time.time;
            for (var index = 0; index < columns.Count; index++)
            {
                latestFallEnd = Mathf.Max(latestFallEnd, Time.time + tileFallSeconds);
                var spin = UnityEngine.Random.Range(-maxSpinDegrees, maxSpinDegrees);
                running++;
                var tileCenter = columns[index].Tile != null ? columns[index].Tile.localPosition : Vector3.zero;
                StartCoroutine(Fall(columns[index].Tile, tileFallSeconds, 0f, fallCurve, spin, 0f, null, () => running--));
                foreach (var extra in columns[index].Extras)
                {
                    running++;
                    StartCoroutine(Fall(extra, tileFallSeconds, 0f, fallCurve, spin, 0f, tileCenter, () => running--));
                }
                foreach (var piece in columns[index].Pieces)
                {
                    latestFallEnd = Mathf.Max(latestFallEnd, Time.time + catDelay + catFallSeconds);
                    running++;
                    StartCoroutine(Fall(piece, catFallSeconds, catDelay, catCurve, -spin * 1.5f, catJump, null, () => running--));
                }
                // No extra stagger after the final column.
                if (index + 1 < columns.Count) yield return new WaitForSeconds(gap);
            }
            if (winPopup != null)
            {
                // Cover the last part of the fall instead of cutting to an opaque screen.
                // Completion still waits for all board animations below.
                var leadIn = Mathf.Max(0f, latestFallEnd - Time.time - BackgroundTransitionSeconds);
                if (leadIn > 0f) yield return new WaitForSeconds(leadIn);
                yield return winPopup.CoverBoard(BackgroundTransitionSeconds);
            }
            while (running > 0) yield return null;
            finished?.Invoke();
        }

        public void ResetTransition()
        {
            StopAllCoroutines();
            if (winPopup != null) winPopup.ClearBackgroundTransition();
        }

        // `jump`: độ cao lúc đường cong xuống thấp nhất (giá trị âm nhỏ nhất của đường cong ứng với nhảy cao `jump`).
        // `pivot`: tâm để co lại và xoay quanh (vị trí cục bộ của khối); null thì quanh chính vật đó.
        private IEnumerator Fall(Transform target, float seconds, float delay, AnimationCurve curve, float spin, float jump, Vector3? pivot, Action done)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (target == null)
            {
                done();
                yield break;
            }
            // Quân tự trượt về ô và thở trong PieceView.Update: dừng lại để hoạt cảnh rơi điều khiển vị trí.
            if (target.TryGetComponent<PieceView>(out var piece)) piece.enabled = false;
            // Ô độc đang nhúc nhích bằng ToxicTileView: dừng để hoạt cảnh rơi điều khiển.
            foreach (var toxic in target.GetComponentsInChildren<ToxicTileView>()) toxic.enabled = false;
            var renderers = target.GetComponentsInChildren<SpriteRenderer>();
            var colors = new Color[renderers.Length];
            for (var index = 0; index < renderers.Length; index++) colors[index] = renderers[index].color;
            var start = target.localPosition;
            var startScale = target.localScale;
            var startRotation = target.localRotation;
            var center = pivot ?? start;
            var lowest = 0f;
            foreach (var key in curve.keys) lowest = Mathf.Min(lowest, key.value);
            var lift = lowest < 0f ? jump / -lowest : 0f;
            for (var time = 0f; time < seconds && target != null; time += Time.deltaTime)
            {
                var progress = time / seconds;
                var drop = curve.Evaluate(progress);
                var height = drop < 0f ? -drop * lift : -drop * fallDistance;
                var scale = Mathf.Lerp(1f, endScale, Mathf.Clamp01(drop));
                var turn = Quaternion.Euler(0f, 0f, spin * Mathf.Clamp01(drop));
                target.localPosition = center + turn * ((start - center) * scale) + Vector3.up * height;
                target.localScale = startScale * scale;
                target.localRotation = turn * startRotation;
                var fade = Mathf.Clamp01((progress - 0.35f) / 0.65f);
                for (var index = 0; index < renderers.Length; index++)
                {
                    if (renderers[index] != null) renderers[index].color = Color.Lerp(colors[index], colors[index] * fadeTo, fade);
                }
                yield return null;
            }
            if (target != null) target.gameObject.SetActive(false);
            done();
        }
    }
}
