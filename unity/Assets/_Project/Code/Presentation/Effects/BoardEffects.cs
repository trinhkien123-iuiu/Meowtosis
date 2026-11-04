using UnityEngine;

namespace Meowtosis.Presentation
{
    // Sinh hiệu ứng từ các prefab ở Prefabs/Effects, chỉnh hình dáng trong prefab; lớp này chỉ đặt chỗ và tô màu.
    public sealed class BoardEffects : MonoBehaviour
    {
        [SerializeField] private ParticleSystem burstPrefab;
        [SerializeField] private ParticleSystem ringPrefab;
        [SerializeField] private ParticleSystem sparklePrefab;
        [SerializeField] private ParticleSystem smokePrefab;
        [SerializeField] private ParticleSystem splatPrefab;
        [SerializeField] private ParticleSystem rainbowPrefab;
        [SerializeField] private ParticleSystem confettiPrefab;
        [SerializeField] private ParticleSystem glowPrefab;
        [SerializeField] private SoulEffect soulPrefab;
        [SerializeField] private JawsEffect jawsPrefab;
        [SerializeField] private DizzyEffect dizzyPrefab;
        [SerializeField] private FloatingText floatingTextPrefab;
        [SerializeField] private Camera targetCamera;
        [Tooltip("Cỡ khung nhìn mà confetti, hạt sáng được chỉnh trong prefab. Khung khác thì co theo để luôn cùng cỡ trên màn hình.")]
        [SerializeField] private float screenEffectViewSize = 6f;

        public void Burst(Vector3 position, Color color) => Spawn(burstPrefab, position, color);

        public void Ring(Vector3 position, Color color) => Spawn(ringPrefab, position, color);

        public void Sparkle(Vector3 position) => Spawn(sparklePrefab, position, null);

        public void Smoke(Vector3 position) => Spawn(smokePrefab, position, null);

        public void Splat(Vector3 position) => Spawn(splatPrefab, position, null);

        public void Rainbow(Vector3 position) => Spawn(rainbowPrefab, position, null);

        // Confetti rơi từ mép trên màn hình; hạt sáng bay lên từ mép dưới.
        public void Confetti() => SpawnAcrossScreen(confettiPrefab, 1f);

        public void Glow() => SpawnAcrossScreen(glowPrefab, -1f);

        public void Soul(Vector3 position) => Instantiate(soulPrefab, position, Quaternion.identity, transform);

        public void Jaws(Vector3 position) => Instantiate(jawsPrefab, position, Quaternion.identity, transform);

        public void Dizzy(PieceView piece) => Instantiate(dizzyPrefab, piece.Visual);

        public void FloatText(Vector3 position, string text) => Instantiate(floatingTextPrefab, position, Quaternion.identity, transform).Show(text);

        private void Spawn(ParticleSystem prefab, Vector3 position, Color? color)
        {
            var effect = Instantiate(prefab, position, Quaternion.identity, transform);
            if (color.HasValue)
            {
                var main = effect.main;
                main.startColor = color.Value;
            }
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
        }

        private void SpawnAcrossScreen(ParticleSystem prefab, float side)
        {
            var halfHeight = targetCamera.orthographicSize;
            var center = targetCamera.transform.position;
            var effect = Instantiate(prefab, new Vector3(center.x, center.y + side * halfHeight, 0f), Quaternion.identity);
            var scale = halfHeight / screenEffectViewSize;
            effect.transform.localScale = Vector3.one * scale;
            var main = effect.main;
            main.gravityModifierMultiplier *= scale;
            var shape = effect.shape;
            shape.scale = new Vector3(screenEffectViewSize * targetCamera.aspect * 2f, 0.2f, 1f);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
        }
    }
}
