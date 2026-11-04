using System;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Một chuyển động của quân, mô tả bằng các đường cong theo tiến trình từ 0 tới 1. Khoảng cách tính bằng ô.
    [Serializable]
    public sealed class PieceMotion
    {
        [SerializeField] private float seconds;
        [SerializeField] private int repeats;
        [Tooltip("Lệch theo hướng của nước đi.")]
        [SerializeField] private AnimationCurve offset;
        [Tooltip("Nhấc lên theo chiều dọc.")]
        [SerializeField] private AnimationCurve lift;
        [Tooltip("Co giãn dọc theo hướng của nước đi.")]
        [SerializeField] private AnimationCurve along;
        [Tooltip("Co giãn vuông góc với hướng của nước đi.")]
        [SerializeField] private AnimationCurve across;
        [SerializeField] private AnimationCurve rotation;
        [SerializeField] private AnimationCurve alpha;
        [Tooltip("Mức nhuốm màu tím (khi bị virus ăn).")]
        [SerializeField] private AnimationCurve tint;

        public PieceMotion(float seconds, int repeats = 1, AnimationCurve offset = null, AnimationCurve lift = null,
            AnimationCurve along = null, AnimationCurve across = null, AnimationCurve rotation = null,
            AnimationCurve alpha = null, AnimationCurve tint = null)
        {
            this.seconds = seconds;
            this.repeats = repeats;
            this.offset = offset ?? Keys(0f, 0f, 1f, 0f);
            this.lift = lift ?? Keys(0f, 0f, 1f, 0f);
            this.along = along ?? Keys(0f, 1f, 1f, 1f);
            this.across = across ?? Keys(0f, 1f, 1f, 1f);
            this.rotation = rotation ?? Keys(0f, 0f, 1f, 0f);
            this.alpha = alpha ?? Keys(0f, 1f, 1f, 1f);
            this.tint = tint ?? Keys(0f, 0f, 1f, 0f);
        }

        public float Seconds => seconds;

        public int Repeats => repeats;

        public float TotalSeconds => seconds * repeats;

        public float Offset(float progress) => offset.Evaluate(progress);

        public float Lift(float progress) => lift.Evaluate(progress);

        public float Along(float progress) => along.Evaluate(progress);

        public float Across(float progress) => across.Evaluate(progress);

        public float Rotation(float progress) => rotation.Evaluate(progress);

        public float Alpha(float progress) => alpha.Evaluate(progress);

        public float Tint(float progress) => tint.Evaluate(progress);

        // Đường cong từ các cặp (thời điểm, giá trị).
        public static AnimationCurve Keys(params float[] timesAndValues)
        {
            var keys = new Keyframe[timesAndValues.Length / 2];
            for (var index = 0; index < keys.Length; index++) keys[index] = new Keyframe(timesAndValues[index * 2], timesAndValues[index * 2 + 1]);
            return new AnimationCurve(keys);
        }
    }

    // Bộ chuyển động của quân (Data/PieceMotions). Giá trị khởi đầu lấy từ keyframe CSS của prototype (ô rộng 90);
    // chỉnh đường cong trong Inspector là thấy ngay, không cần sửa code.
    [CreateAssetMenu(fileName = "PieceMotions", menuName = "Meowtosis/Piece Motions")]
    public sealed class PieceMotions : ScriptableObject
    {
        [Header("Nước đi")]
        [SerializeField] private PieceMotion born = new PieceMotion(0.45f,
            along: PieceMotion.Keys(0f, 0.25f, 0.6f, 1.12f, 1f, 1f), across: PieceMotion.Keys(0f, 0.25f, 0.6f, 1.12f, 1f, 1f));
        [SerializeField] private PieceMotion squash = new PieceMotion(0.3f,
            along: PieceMotion.Keys(0f, 1f, 0.35f, 1.24f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.35f, 0.8f, 1f, 1f));
        [SerializeField] private PieceMotion shove = new PieceMotion(0.32f,
            offset: PieceMotion.Keys(0f, 0f, 0.4f, 0.078f, 1f, 0f),
            along: PieceMotion.Keys(0f, 1f, 0.4f, 0.9f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.4f, 1.1f, 1f, 1f));
        [SerializeField] private PieceMotion bumped = new PieceMotion(0.42f,
            offset: PieceMotion.Keys(0f, 0f, 0.4f, 0.067f, 0.75f, -0.023f, 1f, 0f),
            along: PieceMotion.Keys(0f, 1f, 0.4f, 0.86f, 0.75f, 1f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.4f, 1.14f, 0.75f, 1f, 1f, 1f));
        [SerializeField] private PieceMotion morph = new PieceMotion(0.6f,
            along: PieceMotion.Keys(0f, 0.3f, 0.7f, 1.08f, 1f, 1f), across: PieceMotion.Keys(0f, 0.3f, 0.7f, 1.08f, 1f, 1f),
            rotation: PieceMotion.Keys(0f, -220f, 1f, 0f));

        [Header("Mất quân")]
        [SerializeField] private PieceMotion pop = new PieceMotion(0.3f,
            along: PieceMotion.Keys(0f, 1f, 0.35f, 1.32f, 1f, 0.1f), across: PieceMotion.Keys(0f, 1f, 0.35f, 1.32f, 1f, 0.1f),
            alpha: PieceMotion.Keys(0f, 1f, 0.35f, 1f, 1f, 0f));
        [SerializeField] private PieceMotion eaten = new PieceMotion(0.44f,
            along: PieceMotion.Keys(0f, 1f, 0.3f, 1.08f, 0.55f, 0.85f, 1f, 0.05f), across: PieceMotion.Keys(0f, 1f, 0.3f, 1.08f, 0.55f, 0.85f, 1f, 0.05f),
            rotation: PieceMotion.Keys(0f, 0f, 0.3f, -9f, 0.55f, 9f, 1f, 30f),
            alpha: PieceMotion.Keys(0f, 1f, 0.55f, 1f, 1f, 0f), tint: PieceMotion.Keys(0f, 0f, 0.3f, 1f, 1f, 1f));
        [SerializeField] private PieceMotion lunge = new PieceMotion(0.34f,
            offset: PieceMotion.Keys(0f, 0f, 0.45f, 0.133f, 1f, 0f),
            along: PieceMotion.Keys(0f, 1f, 0.45f, 1.2f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.45f, 1.2f, 1f, 1f));

        [Header("Phản ứng")]
        [SerializeField] private PieceMotion dazed = new PieceMotion(0.42f, 3, rotation: PieceMotion.Keys(0f, 0f, 0.25f, -8f, 0.75f, 8f, 1f, 0f));
        [SerializeField] private PieceMotion nope = new PieceMotion(0.32f, offset: PieceMotion.Keys(0f, 0f, 0.25f, -0.067f, 0.5f, 0.067f, 0.75f, -0.033f, 1f, 0f));
        [SerializeField] private PieceMotion waking = new PieceMotion(0.6f,
            lift: PieceMotion.Keys(0f, 0f, 0.35f, 0.133f, 1f, 0f),
            along: PieceMotion.Keys(0f, 1f, 0.35f, 1.14f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.35f, 1.14f, 1f, 1f));
        [SerializeField] private PieceMotion hop = new PieceMotion(0.45f, 3,
            lift: PieceMotion.Keys(0f, 0f, 0.5f, 0.156f, 1f, 0f),
            along: PieceMotion.Keys(0f, 1f, 0.5f, 1.06f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.5f, 0.94f, 1f, 1f));
        [SerializeField] private PieceMotion pet = new PieceMotion(0.5f,
            lift: PieceMotion.Keys(0f, 0f, 0.3f, 0.06f, 1f, 0f),
            along: PieceMotion.Keys(0f, 1f, 0.3f, 1.12f, 0.6f, 0.96f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.3f, 1.12f, 0.6f, 0.96f, 1f, 1f));

        [Header("Lúc rảnh")]
        [SerializeField] private PieceMotion look = new PieceMotion(1.75f, rotation: PieceMotion.Keys(0f, 0f, 0.2f, 6f, 0.8f, 6f, 1f, 0f));
        [SerializeField] private PieceMotion stretch = new PieceMotion(1.15f,
            along: PieceMotion.Keys(0f, 1f, 0.4f, 0.92f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.4f, 1.12f, 1f, 1f));
        [SerializeField] private PieceMotion yawn = new PieceMotion(1.5f,
            along: PieceMotion.Keys(0f, 1f, 0.5f, 1.05f, 1f, 1f), across: PieceMotion.Keys(0f, 1f, 0.5f, 1.08f, 1f, 1f));
        [SerializeField] private PieceMotion nuzzle = new PieceMotion(1.4f,
            rotation: PieceMotion.Keys(0f, 0f, 0.2f, 10f, 0.35f, 6f, 0.5f, 10f, 0.65f, 6f, 0.8f, 10f, 1f, 0f));

        public PieceMotion Born => born;

        public PieceMotion Squash => squash;

        public PieceMotion Shove => shove;

        public PieceMotion Bumped => bumped;

        public PieceMotion Morph => morph;

        public PieceMotion Pop => pop;

        public PieceMotion Eaten => eaten;

        public PieceMotion Lunge => lunge;

        public PieceMotion Dazed => dazed;

        public PieceMotion Nope => nope;

        public PieceMotion Waking => waking;

        public PieceMotion Hop => hop;

        public PieceMotion Pet => pet;

        public PieceMotion Look => look;

        public PieceMotion Stretch => stretch;

        public PieceMotion Yawn => yawn;

        public PieceMotion Nuzzle => nuzzle;
    }
}
