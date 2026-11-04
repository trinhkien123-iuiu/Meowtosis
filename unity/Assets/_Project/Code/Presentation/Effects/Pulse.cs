using UnityEngine;

namespace Meowtosis.Presentation
{
    // Phồng xẹp nhẹ liên tục (vòng sáng hướng dẫn, ô đang chọn). Gắn vào prefab, chỉnh trong Inspector.
    public sealed class Pulse : MonoBehaviour
    {
        [SerializeField] private float amount = 0.05f;
        [SerializeField] private float cyclesPerSecond = 1.1f;

        private Vector3 baseScale;

        private void Awake() => baseScale = transform.localScale;

        private void Update()
        {
            var wave = Mathf.Sin(Time.time * cyclesPerSecond * Mathf.PI * 2f);
            transform.localScale = baseScale * (1f + amount * wave);
        }
    }
}
