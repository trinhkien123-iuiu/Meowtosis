using System.Collections.Generic;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Hiệu ứng mèo ngủ (con của SleepMarks trong prefab Piece, chỉ chạy khi mèo đang ngủ):
    //   chữ Z nhỏ lần lượt bay lên từ đỉnh đầu, lượn qua lại, to dần rồi mờ đi;
    //   khoảng một nửa số mèo (theo id) thổi bong bóng mũi hình giọt nước, mỗi con một kiểu:
    //     - kiểu thở: phồng lên xẹp xuống đều đặn theo nhịp thở (hình sin, liên tục, không giật);
    //     - kiểu vỡ: phồng dần lên rất to, phình thêm một chút rồi vỡ "bụp" (mờ đi), nghỉ một nhịp rồi thổi lại từ đầu.
    // Mọi đại lượng (cỡ, độ mờ, độ nghiêng, vị trí) đều là hàm liên tục của thời gian, chỗ nối giữa các chu kỳ đều bằng nhau.
    public sealed class SleepyEffect : MonoBehaviour
    {
        [Header("Chữ Z")]
        [SerializeField] private Sprite letter;
        [SerializeField] private Vector2 letterStart = new Vector2(0.22f, 0.36f);
        [SerializeField] private Vector2 letterTravel = new Vector2(0.22f, 0.55f);
        [SerializeField] private float letterSeconds = 1.8f;
        [SerializeField] private float letterEvery = 0.75f;
        [SerializeField] private float letterStartSize = 0.12f;
        [SerializeField] private float letterEndSize = 0.26f;
        [SerializeField] private float swayWidth = 0.06f;
        [SerializeField] private int letterOrder = 14;

        [Header("Bong bóng mũi")]
        [SerializeField] private SpriteRenderer bubble;
        [SerializeField] private Vector2 bubbleAnchor = new Vector2(0.02f, 0.06f);
        [SerializeField] private float bubbleMin = 0.07f;
        [SerializeField] private float bubbleMax = 0.3f;
        [Tooltip("Độ nghiêng của giọt nước (dương: đầu nhọn quay sang trái, về phía mũi).")]
        [SerializeField] private float bubbleTiltDegrees = 38f;
        [SerializeField] private float tiltSwayDegrees = 6f;
        [Header("Kiểu thở")]
        [SerializeField] private float breathSeconds = 2.6f;
        [Header("Kiểu vỡ")]
        [SerializeField] private float inflateSeconds = 3.2f;
        [SerializeField] private float popSeconds = 0.16f;
        [SerializeField] private float popOvershoot = 1.35f;
        [SerializeField] private float restSeconds = 0.7f;

        private readonly List<SpriteRenderer> letters = new List<SpriteRenderer>();
        private readonly List<float> letterAges = new List<float>();
        private float nextLetter;
        private bool blowsBubble;
        private bool popsBubble;
        private float phase;

        // Gọi khi gán quân cho prefab: id quyết định mèo này có thổi bong bóng không, kiểu nào, và lệch nhịp với mèo khác.
        public void Setup(int pieceId)
        {
            blowsBubble = pieceId % 2 == 0;
            popsBubble = pieceId % 4 == 0;
            phase = pieceId * 0.37f;
            if (bubble != null) bubble.gameObject.SetActive(blowsBubble);
        }

        private void OnEnable()
        {
            nextLetter = Random.value * letterEvery;
            foreach (var renderer in letters) renderer.gameObject.SetActive(false);
            letterAges.Clear();
            for (var index = 0; index < letters.Count; index++) letterAges.Add(-1f);
        }

        private void Update()
        {
            UpdateLetters();
            if (blowsBubble && bubble != null) UpdateBubble();
        }

        private void UpdateLetters()
        {
            nextLetter -= Time.deltaTime;
            if (nextLetter <= 0f)
            {
                nextLetter = letterEvery;
                SpawnLetter();
            }
            for (var index = 0; index < letters.Count; index++)
            {
                if (letterAges[index] < 0f) continue;
                letterAges[index] += Time.deltaTime;
                var t = letterAges[index] / letterSeconds;
                var renderer = letters[index];
                if (t >= 1f)
                {
                    letterAges[index] = -1f;
                    renderer.gameObject.SetActive(false);
                    continue;
                }
                var sway = Mathf.Sin(t * Mathf.PI * 2.2f) * swayWidth;
                renderer.transform.localPosition = new Vector3(letterStart.x + letterTravel.x * t + sway, letterStart.y + letterTravel.y * t, 0f);
                renderer.transform.localScale = Vector3.one * Mathf.Lerp(letterStartSize, letterEndSize, t);
                renderer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 3f) * 14f);
                var alpha = t < 0.15f ? t / 0.15f : 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
                renderer.color = new Color(1f, 1f, 1f, alpha);
            }
        }

        private void SpawnLetter()
        {
            var index = letterAges.FindIndex(age => age < 0f);
            if (index < 0)
            {
                var go = new GameObject("Z");
                go.transform.SetParent(transform, false);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = letter;
                renderer.sortingOrder = letterOrder;
                letters.Add(renderer);
                letterAges.Add(-1f);
                index = letters.Count - 1;
            }
            letters[index].gameObject.SetActive(true);
            letterAges[index] = 0f;
        }

        private void UpdateBubble()
        {
            var time = Time.time + phase;
            float size;
            float alpha;
            if (popsBubble) PopCycle(time, out size, out alpha);
            else
            {
                // Hình sin giữa nhỏ nhất và lớn nhất: bắt đầu và kết thúc chu kỳ cùng một cỡ nên nối liền mạch.
                size = Mathf.Lerp(bubbleMin, bubbleMax, 0.5f - 0.5f * Mathf.Cos(time / breathSeconds * Mathf.PI * 2f));
                alpha = 1f;
            }
            // Giọt nước nghiêng về phía mũi, đung đưa nhẹ theo nhịp thở; vị trí bám theo cỡ để đầu nhọn luôn chạm mũi.
            var tilt = bubbleTiltDegrees + Mathf.Sin(time / breathSeconds * Mathf.PI * 2f) * tiltSwayDegrees;
            bubble.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            bubble.transform.localPosition = new Vector3(bubbleAnchor.x + size * 0.38f, bubbleAnchor.y - size * 0.34f, 0f);
            bubble.transform.localScale = Vector3.one * size;
            bubble.color = new Color(1f, 1f, 1f, alpha);
        }

        // Một chu kỳ: phồng (inflate) -> vỡ (pop) -> nghỉ (rest) rồi lặp lại.
        //   phồng: cỡ tăng mượt từ nhỏ nhất lên lớn nhất (nhanh lúc đầu, chậm dần), độ mờ tăng nhanh từ 0 ở đầu chu kỳ;
        //   vỡ: phình thêm tới popOvershoot lần cỡ lớn nhất trong lúc mờ dần về 0;
        //   nghỉ: không nhìn thấy gì (độ mờ 0), đủ lâu để mắt coi như bong bóng đã tan trước khi thổi cái mới.
        private void PopCycle(float time, out float size, out float alpha)
        {
            var cycle = inflateSeconds + popSeconds + restSeconds;
            var t = time % cycle;
            if (t < inflateSeconds)
            {
                var u = t / inflateSeconds;
                size = Mathf.Lerp(bubbleMin, bubbleMax, Mathf.Sin(u * Mathf.PI * 0.5f));
                alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.25f));
                return;
            }
            if (t < inflateSeconds + popSeconds)
            {
                var u = (t - inflateSeconds) / popSeconds;
                size = bubbleMax * Mathf.Lerp(1f, popOvershoot, u);
                alpha = 1f - u;
                return;
            }
            size = bubbleMax * popOvershoot;   // giữ nguyên cỡ lúc vừa vỡ (đã trong suốt) để không có bước nhảy
            alpha = 0f;
        }
    }
}
