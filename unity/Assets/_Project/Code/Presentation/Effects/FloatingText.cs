using TMPro;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Chữ bay lên rồi mờ dần ("Khử 3!") (Prefabs/Effects/FloatingText).
    public sealed class FloatingText : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private float seconds = 1.1f;
        [SerializeField] private float riseSpeed = 0.78f;

        private float time;

        public void Show(string text)
        {
            label.text = text;
            transform.localPosition += Vector3.up * 0.27f;
            Destroy(gameObject, seconds);
        }

        private void Update()
        {
            time += Time.deltaTime;
            transform.localPosition += Vector3.up * (riseSpeed * Time.deltaTime);
            label.alpha = Mathf.Clamp01(1f - time / seconds);
        }
    }
}
