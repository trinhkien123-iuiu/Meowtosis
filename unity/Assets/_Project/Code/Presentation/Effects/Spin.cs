using UnityEngine;

namespace Meowtosis.Presentation
{
    // Xoay liên tục (mắt @@). Gắn vào prefab, chỉnh tốc độ trong Inspector.
    public sealed class Spin : MonoBehaviour
    {
        [Tooltip("Độ mỗi giây (prototype: 1 vòng trong 0,55 giây).")]
        [SerializeField] private float degreesPerSecond = -655f;

        private void Update() => transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
