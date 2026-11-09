using System.Collections;
using System.Collections.Generic;
using Meowtosis.Domain;
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
        [SerializeField] private ParticleSystem dustPrefab;
        [SerializeField] private ParticleSystem splatPrefab;
        [SerializeField] private ParticleSystem rainbowPrefab;
        [SerializeField] private ParticleSystem confettiPrefab;
        [SerializeField] private ParticleSystem glowPrefab;
        [Tooltip("Hào quang khi quân đi qua màng một chiều. Dựng sẵn theo hướng đi là +X, được xoay theo hướng thật.")]
        [SerializeField] private ParticleSystem gateGlowPrefab;
        [Tooltip("Tuyết bung ra khi bản sao trượt qua hoặc dừng trên ô băng.")]
        [SerializeField] private ParticleSystem frostPrefab;
        [Tooltip("Vạch tốc độ phía sau con mèo đang trượt trên băng. Dựng sẵn theo hướng trượt là +X.")]
        [SerializeField] private ParticleSystem speedLinesPrefab;
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

        // Bụi trắng kem tung sang hai bên sát mặt ô, như vừa đáp đất từ trên cao (bản sao của Nhân bản đáp xuống).
        public void Dust(Vector3 position) => Spawn(dustPrefab, position, null);

        public void Splat(Vector3 position) => Spawn(splatPrefab, position, null);

        public void Rainbow(Vector3 position) => Spawn(rainbowPrefab, position, null);

        // `position` là giữa cạnh hai ô, `direction` là hướng quân đi qua.
        public void GateGlow(Vector3 position, Direction direction)
        {
            var offset = BoardLayout.Offset(direction);
            var angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            Spawn(gateGlowPrefab, position, null, Quaternion.Euler(0f, 0f, angle));
        }

        public void Frost(Vector3 position) => Spawn(frostPrefab, position, null);

        // Quân đi vào hoặc ra khỏi một đầu cổng: vòng sóng và tàn sáng cùng màu cổng.
        public void PortalPulse(Vector3 position, Color color)
        {
            Spawn(ringPrefab, position, color);
            Spawn(burstPrefab, position, color);
        }

        // Mèo hai màu đổi màu: tia và vòng sóng màu mới nổ ra, kèm tàn sáng.
        public void ColorFlip(Vector3 position, Color from, Color to)
        {
            Spawn(burstPrefab, position, from);
            Spawn(ringPrefab, position, to);
            Spawn(sparklePrefab, position, null);
        }

        // Vạch tốc độ bám theo `piece` trong `seconds` giây; vạch đã phát ra thì ở lại phía sau rồi tan.
        public void SpeedLines(PieceView piece, Direction direction, float seconds)
        {
            var offset = BoardLayout.Offset(direction);
            var angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            var effect = Instantiate(speedLinesPrefab, piece.transform);
            effect.transform.localPosition = Vector3.zero;
            effect.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            var main = effect.main;
            main.duration = Mathf.Max(seconds, 0.1f);
            effect.Play();
            Destroy(effect.gameObject, main.duration + main.startLifetime.constantMax);
        }

        // Confetti rơi từ mép trên màn hình; hạt sáng bay lên từ mép dưới.
        public void Confetti() => SpawnAcrossScreen(confettiPrefab, 1f);

        public void Glow() => SpawnAcrossScreen(glowPrefab, -1f);

        public void Soul(Vector3 position) => Instantiate(soulPrefab, position, Quaternion.identity, transform);

        public void Jaws(Vector3 position) => Instantiate(jawsPrefab, position, Quaternion.identity, transform);

        public void Dizzy(PieceView piece) => Instantiate(dizzyPrefab, piece.Visual);

        public void FloatText(Vector3 position, string text) =>
            Instantiate(floatingTextPrefab, position, Quaternion.identity, transform).Show(text, targetCamera);

        // Hạt dùng xong thì cất vào kho theo từng prefab và dùng lại, không tạo rồi huỷ liên tục (đỡ rác bộ nhớ trên máy yếu).
        private readonly Dictionary<ParticleSystem, Stack<ParticleSystem>> pool = new Dictionary<ParticleSystem, Stack<ParticleSystem>>();

        private void Spawn(ParticleSystem prefab, Vector3 position, Color? color, Quaternion? rotation = null)
        {
            var effect = Take(prefab);
            effect.transform.SetPositionAndRotation(position, rotation ?? Quaternion.identity);
            if (color.HasValue)
            {
                var main = effect.main;
                main.startColor = color.Value;
            }
            effect.gameObject.SetActive(true);
            effect.Play();
            StartCoroutine(Release(prefab, effect, effect.main.duration + effect.main.startLifetime.constantMax));
        }

        private ParticleSystem Take(ParticleSystem prefab)
        {
            if (pool.TryGetValue(prefab, out var free))
            {
                while (free.Count > 0)
                {
                    var reused = free.Pop();
                    if (reused != null) return reused;
                }
            }
            return Instantiate(prefab, transform);
        }

        private IEnumerator Release(ParticleSystem prefab, ParticleSystem effect, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (effect == null) yield break;
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.gameObject.SetActive(false);
            if (!pool.TryGetValue(prefab, out var free))
            {
                free = new Stack<ParticleSystem>();
                pool[prefab] = free;
            }
            if (free.Count < MaxPooledPerEffect) free.Push(effect);
            else Destroy(effect.gameObject);
        }

        private const int MaxPooledPerEffect = 12;

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
