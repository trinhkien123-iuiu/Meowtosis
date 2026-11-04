using System;
using System.Collections;
using Meowtosis.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    // Một nút level trên bản đồ (Prefabs/Home/LevelMapNode): số level, sao đã đạt, ổ khoá, huy hiệu cơ chế mới, vòng sáng level hiện tại.
    // Nhấn thì lún xuống, thả thì nảy về, được chọn thì nảy to rồi mới báo xong để vào màn chơi.
    public sealed class LevelMapNode : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image body;
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private GameObject starRow;
        [SerializeField] private Image[] stars;
        [SerializeField] private Sprite litStar;
        [SerializeField] private Sprite emptyStar;
        [SerializeField] private GameObject padlock;
        [SerializeField] private GameObject newBadge;
        [SerializeField] private GameObject currentGlow;
        [SerializeField] private Color lockedColor = new Color(0.55f, 0.58f, 0.72f, 1f);
        [SerializeField] private float shakeSeconds = 0.3f;
        [SerializeField] private float shakeDistance = 14f;

        [Header("Phản hồi khi bấm")]
        [SerializeField] private float pressedScale = 0.86f;
        [SerializeField] private float pressSeconds = 0.07f;
        [SerializeField] private float releaseSeconds = 0.22f;
        [SerializeField] private AnimationCurve releaseCurve = new AnimationCurve(new Keyframe(0f, 0.86f), new Keyframe(0.55f, 1.1f), new Keyframe(1f, 1f));
        [SerializeField] private float selectSeconds = 0.38f;
        [SerializeField] private AnimationCurve selectCurve = new AnimationCurve(new Keyframe(0f, 0.86f), new Keyframe(0.4f, 1.3f), new Keyframe(0.75f, 1.05f), new Keyframe(1f, 1.15f));

        private Coroutine scaling;
        private Coroutine shaking;
        private bool isSelected;

        public event Action Clicked;

        public RectTransform Area => (RectTransform)transform;

        public void Show(LevelMapEntry entry)
        {
            numberText.text = (entry.Index + 1).ToString();
            numberText.gameObject.SetActive(entry.IsUnlocked);
            padlock.SetActive(!entry.IsUnlocked);
            body.color = entry.IsUnlocked ? Color.white : lockedColor;
            newBadge.SetActive(entry.IntroducesMechanic);
            currentGlow.SetActive(entry.IsCurrent);
            starRow.SetActive(entry.Stars > 0);
            for (var star = 0; star < stars.Length; star++) stars[star].sprite = star < entry.Stars ? litStar : emptyStar;
        }

        // Rung nhẹ khi bấm vào level chưa mở.
        public void Shake()
        {
            if (shaking != null) StopCoroutine(shaking);
            shaking = StartCoroutine(RunShake());
        }

        // Diễn lúc được chọn; `finished` được gọi khi diễn xong.
        public void PlaySelected(Action finished)
        {
            isSelected = true;
            RunScale(PlayCurve(selectCurve, selectSeconds, finished));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isSelected) RunScale(Press());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isSelected) RunScale(PlayCurve(releaseCurve, releaseSeconds, null));
        }

        private void Awake() => button.onClick.AddListener(() => Clicked?.Invoke());

        private void RunScale(IEnumerator animation)
        {
            if (scaling != null) StopCoroutine(scaling);
            scaling = StartCoroutine(animation);
        }

        private IEnumerator Press()
        {
            var from = transform.localScale.x;
            for (var time = 0f; time < pressSeconds; time += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(from, pressedScale, time / pressSeconds);
                yield return null;
            }
            transform.localScale = Vector3.one * pressedScale;
        }

        private IEnumerator PlayCurve(AnimationCurve curve, float seconds, Action finished)
        {
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                transform.localScale = Vector3.one * curve.Evaluate(time / seconds);
                yield return null;
            }
            transform.localScale = Vector3.one * curve.Evaluate(1f);
            finished?.Invoke();
        }

        private IEnumerator RunShake()
        {
            var visual = body.rectTransform;
            for (var time = 0f; time < shakeSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / shakeSeconds;
                visual.anchoredPosition = Vector2.right * (Mathf.Sin(progress * Mathf.PI * 6f) * shakeDistance * (1f - progress));
                yield return null;
            }
            visual.anchoredPosition = Vector2.zero;
        }
    }
}
