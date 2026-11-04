using UnityEngine;

namespace Meowtosis.Presentation
{
    // Hai hàm răng tím đớp lại ở ô bị virus ăn (Prefabs/Effects/Jaws).
    public sealed class JawsEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer top;
        [SerializeField] private SpriteRenderer bottom;
        [SerializeField] private float seconds = 0.46f;
        [Tooltip("Khoảng cách mỗi hàm so với tâm ô, tính bằng ô.")]
        [SerializeField] private AnimationCurve gap = PieceMotion.Keys(0f, 0.333f, 0.55f, -0.033f, 0.7f, 0f, 1f, 0f);
        [SerializeField] private AnimationCurve opacity = PieceMotion.Keys(0f, 0f, 0.2f, 1f, 0.7f, 1f, 1f, 0f);

        private float time;

        private void Start() => Destroy(gameObject, seconds);

        private void Update()
        {
            time += Time.deltaTime;
            var progress = Mathf.Clamp01(time / seconds);
            top.transform.localPosition = Vector3.up * gap.Evaluate(progress);
            bottom.transform.localPosition = Vector3.down * gap.Evaluate(progress);
            var color = new Color(1f, 1f, 1f, opacity.Evaluate(progress));
            top.color = color;
            bottom.color = color;
        }
    }
}
