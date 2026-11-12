using System;
using System.Collections;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Meowtosis.Presentation
{
    public interface IWinPopup
    {
        event Action ContinueClicked;

        event Action BackClicked;

        // `totalCoins` là tổng xu sau khi đã cộng thưởng của ván này.
        void Show(int stars, int coinsEarned, int totalCoins);

        void Hide();
    }

    // Presentation only: reward is already saved by gameplay before this popup opens.
    public sealed class WinPopup : PopupView, IWinPopup
    {
        private const float CelebrationCatScale = 0.7f;
        private const float CelebrationCatSpacing = 275f;
        [SerializeField] private Image[] stars;
        [SerializeField] private Sprite litStar;
        [SerializeField] private Sprite emptyStar;
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform reward;
        [SerializeField] private CanvasGroup rewardGroup;
        [Tooltip("Xu bay ra từ chỗ này.")]
        [SerializeField] private RectTransform coinSource;
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private CoinCounterView coinCounter;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button backButton;
        [SerializeField] private WinEffects effects;
        [SerializeField] private SoundPlayer sounds;
        [Header("Celebration cats")]
        [SerializeField] private SkeletonDataAsset[] celebrationCats = Array.Empty<SkeletonDataAsset>();
        [SerializeField] private Material celebrationMaterial;
        [Header("Original WELL DONE title")]
        [SerializeField] private SkeletonDataAsset wellDoneTitle;
        [SerializeField] private Material wellDoneMaterial;
        [SerializeField] private float celebrationSeconds = 1.9f;

        [Header("Nhịp diễn")]
        [SerializeField] private float titleDelay = 0.1f;
        [SerializeField] private float firstStarDelay = 0.35f;
        [SerializeField] private float starStep = 0.26f;
        [SerializeField] private float popSeconds = 0.4f;
        [SerializeField] private float counterDelay = 0.2f;
        [SerializeField] private float coinStagger = 0.06f;
        [SerializeField, Range(4, 30)] private int flyingCoinCount = 12;
        [SerializeField] private AnimationCurve popScale = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.7f, 1.25f), new Keyframe(1f, 1f));

        public event Action ContinueClicked;

        public event Action BackClicked;

        private Vector2 rewardHome;
        private bool initialized;
        private bool rewardReady;
        private bool collecting;
        private bool collectionFinished;
        private bool buttonsDismissed;
        private CanvasGroup continueGroup;
        private CanvasGroup backGroup;
        private int rewardCoins;
        private int coinsBeforeReward;
        private int flightCount;
        private RectTransform celebrationRoot;
        private CanvasGroup celebrationGroup;
        private SkeletonGraphic[] celebrationViews;
        private SkeletonGraphic wellDoneView;
        private Vector2[] starHomes;
        private Quaternion[] starRotations;
        private Image[] starGlints;
        private CanvasGroup backgroundTransition;
        private readonly Vector3[] celebrationScreenCorners = new Vector3[4];

        public void Show(int earnedStars, int coinsEarned, int totalCoins)
        {
            // The popup starts inactive, so Show can be called before Unity has run Awake.
            InitializeOnce();
            effects.Clear();
            rewardReady = collecting = collectionFinished = buttonsDismissed = false;
            rewardCoins = coinsEarned;
            coinsBeforeReward = totalCoins - coinsEarned;
            flightCount = Mathf.Clamp(flyingCoinCount, 1, 30);
            continueButton.interactable = backButton.interactable = false;
            continueGroup.alpha = backGroup.alpha = 1f;
            coinCounter.Hide();
            coinsText.text = GameTexts.CoinReward(coinsEarned);
            title.localScale = Vector3.zero;
            reward.localScale = Vector3.zero;
            reward.anchoredPosition = rewardHome;
            rewardGroup.alpha = 1f;
            if (celebrationRoot != null) celebrationRoot.gameObject.SetActive(false);
            continueButton.gameObject.SetActive(false);
            backButton.gameObject.SetActive(false);
            for (var i = 0; i < stars.Length; i++)
            {
                stars[i].rectTransform.localScale = Vector3.zero;
                stars[i].rectTransform.anchoredPosition = starHomes[i];
                stars[i].rectTransform.localRotation = starRotations[i];
                if (starGlints[i] != null) starGlints[i].gameObject.SetActive(false);
            }
            Open();
            ClearBackgroundTransition();
            sounds.SetWinMusic(true);
            StartCoroutine(PlayTitle());
            StartCoroutine(PlaySequence(earnedStars, coinsEarned, totalCoins));
        }

        public IEnumerator PrewarmAssets()
        {
            // Spread skeleton parsing over frames while the popup remains inactive.
            yield return null;
            if (wellDoneTitle != null) wellDoneTitle.GetSkeletonData(true);
            if (celebrationCats != null)
                foreach (var data in celebrationCats)
                {
                    yield return null;
                    if (data != null) data.GetSkeletonData(true);
                }
            yield return null;
            if (effects != null) effects.PrewarmAssets();
        }

        public IEnumerator CoverBoard(float seconds)
        {
            if (backgroundTransition == null)
            {
                var cover = new GameObject("WinBackgroundTransition", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                var rect = (RectTransform)cover.transform;
                rect.SetParent(transform.parent, false);
                rect.SetSiblingIndex(transform.GetSiblingIndex());
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                var background = GetComponent<Image>();
                var image = cover.GetComponent<Image>();
                image.color = background.color;
                image.sprite = background.sprite;
                image.raycastTarget = true;
                // Use the exact same wood and tint so the fully opaque handoff is invisible.
                var wood = transform.Find("WinBackgroundWood");
                if (wood != null) Instantiate(wood.gameObject, rect, false);
                backgroundTransition = cover.GetComponent<CanvasGroup>();
            }
            backgroundTransition.gameObject.SetActive(true);
            backgroundTransition.alpha = 0f;
            for (var time = 0f; time < seconds; time += Time.deltaTime)
            {
                backgroundTransition.alpha = Mathf.SmoothStep(0f, 1f, time / seconds);
                yield return null;
            }
            backgroundTransition.alpha = 1f;
        }

        public void ClearBackgroundTransition()
        {
            if (backgroundTransition != null) backgroundTransition.gameObject.SetActive(false);
        }

        private IEnumerator PlayTitle()
        {
            if (wellDoneTitle == null || wellDoneMaterial == null)
            {
                yield return Pop(title, titleDelay);
                yield break;
            }
            if (titleDelay > 0f) yield return new WaitForSecondsRealtime(titleDelay);
            if (wellDoneView == null)
            {
                wellDoneView = SkeletonGraphic.NewSkeletonGraphicGameObject(wellDoneTitle, title, wellDoneMaterial);
                wellDoneView.gameObject.name = "WellDoneAnimatedTitle";
                wellDoneView.raycastTarget = false;
                wellDoneView.UnscaledTime = true;
                // Preserve additive shine under CanvasGroup: keep blend alpha in UVs
                // instead of letting the normal UI shader multiply its zero alpha into RGB.
                var meshSettings = wellDoneView.MeshGenerator.settings;
                meshSettings.tintBlack = true;
                meshSettings.canvasGroupCompatible = true;
                meshSettings.pmaVertexColors = true;
                meshSettings.useClipping = true;
                wellDoneView.MeshGenerator.settings = meshSettings;
                var titleCanvas = wellDoneView.canvas;
                if (titleCanvas != null)
                    titleCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2;
                wellDoneView.rectTransform.anchorMin = wellDoneView.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                wellDoneView.rectTransform.anchoredPosition = Vector2.zero;
                wellDoneView.rectTransform.localScale = Vector3.one * 0.95f;
            }
            // Spine animates each letter and its shine; avoid a second UI pop on top.
            title.localScale = Vector3.one;
            wellDoneView.AnimationState.ClearTracks();
            wellDoneView.Skeleton.SetToSetupPose();
            wellDoneView.AnimationState.SetAnimation(0, "levelpass_victory_open", false);
            wellDoneView.AnimationState.AddAnimation(0, "levelpass_victory_idle", true, 0f);
        }

        private void Awake() => InitializeOnce();

        private void InitializeOnce()
        {
            if (initialized) return;
            rewardHome = reward.anchoredPosition;
            if (stars != null)
            {
                starHomes = new Vector2[stars.Length];
                starRotations = new Quaternion[stars.Length];
                starGlints = new Image[stars.Length];
                for (var i = 0; i < stars.Length; i++)
                {
                    starHomes[i] = stars[i].rectTransform.anchoredPosition;
                    starRotations[i] = stars[i].rectTransform.localRotation;
                }
            }
            continueGroup = continueButton.GetComponent<CanvasGroup>();
            if (continueGroup == null) continueGroup = continueButton.gameObject.AddComponent<CanvasGroup>();
            backGroup = backButton.GetComponent<CanvasGroup>();
            if (backGroup == null) backGroup = backButton.gameObject.AddComponent<CanvasGroup>();
            continueButton.onClick.AddListener(OnContinueRequested);
            backButton.onClick.AddListener(() => { if (rewardReady && !collecting) BackClicked?.Invoke(); });
            initialized = true;
        }

        private IEnumerator PlaySequence(int earnedStars, int coinsEarned, int totalCoins)
        {
            effects.Confetti();
            effects.Fireworks();
            yield return PlayCats();
            yield return new WaitForSecondsRealtime(firstStarDelay);
            for (var index = 0; index < stars.Length; index++)
            {
                var isEarned = index < earnedStars;
                stars[index].sprite = isEarned ? litStar : emptyStar;
                StartCoroutine(RevealStar(index, isEarned));
                if (isEarned) StartCoroutine(LandStar(stars[index].rectTransform, index));
                yield return new WaitForSecondsRealtime(starStep);
            }

            yield return new WaitForSecondsRealtime(counterDelay);
            var startCoins = totalCoins - coinsEarned;
            coinCounter.Appear(startCoins);
            yield return Pop(reward, 0f);
            continueButton.gameObject.SetActive(true);
            backButton.gameObject.SetActive(true);
            StartCoroutine(Pop((RectTransform)backButton.transform, 0f));
            yield return Pop((RectTransform)continueButton.transform, 0f);
            rewardReady = true;
            continueButton.interactable = backButton.interactable = true;
        }

        private void OnContinueRequested()
        {
            if (!rewardReady || collecting) return;
            collecting = true;
            continueButton.interactable = backButton.interactable = false;
            StartCoroutine(DismissButtons());
            StartCoroutine(CollectReward());
        }

        private IEnumerator DismissButtons()
        {
            var next = (RectTransform)continueButton.transform;
            var back = (RectTransform)backButton.transform;
            const float seconds = 0.22f;
            for (var time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / seconds;
                var collapse = Mathf.Clamp01((progress - 0.25f) / 0.75f);
                var pop = progress < 0.25f ? Mathf.Lerp(1f, 1.06f, progress / 0.25f)
                    : 1.06f * (1f - collapse * collapse);
                next.localScale = Vector3.one * pop;
                back.localScale = Vector3.one * pop;
                continueGroup.alpha = backGroup.alpha = 1f - Mathf.Clamp01((progress - 0.75f) / 0.25f);
                yield return null;
            }
            continueButton.gameObject.SetActive(false);
            backButton.gameObject.SetActive(false);
            buttonsDismissed = true;
        }

        private IEnumerator CollectReward()
        {
            if (rewardCoins <= 0)
            {
                while (!buttonsDismissed) yield return null;
                ContinueClicked?.Invoke();
                yield break;
            }
            sounds.PlayCoin(0);
            effects.CoinFlight(coinSource, coinCounter.Icon, flightCount, coinStagger, OnCoinLanded);
            // Dissolve the pile while its individual coins scatter outwards.
            const float dissolveSeconds = 0.18f;
            for (var time = 0f; time < dissolveSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / dissolveSeconds;
                rewardGroup.alpha = 1f - progress;
                reward.localScale = Vector3.one * Mathf.Lerp(1f, 0.8f, progress);
                yield return null;
            }
            rewardGroup.alpha = 0f;
            while (!collectionFinished || !buttonsDismissed) yield return null;
            yield return new WaitForSecondsRealtime(0.12f);
            ContinueClicked?.Invoke();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            ClearBackgroundTransition();
            if (effects != null) effects.Clear();
            if (sounds != null) sounds.SetWinMusic(false);
        }

        private IEnumerator PlayCats()
        {
            if (celebrationCats == null || celebrationCats.Length == 0 || celebrationMaterial == null) yield break;
            if (celebrationRoot == null)
            {
                var group = new GameObject("ThreeCatCelebration", typeof(RectTransform), typeof(CanvasGroup));
                celebrationRoot = (RectTransform)group.transform;
                celebrationRoot.SetParent(title.parent, false);
                celebrationRoot.anchorMin = celebrationRoot.anchorMax = new Vector2(0.5f, 0.5f);
                celebrationRoot.sizeDelta = new Vector2(900f, 560f);
                celebrationRoot.anchoredPosition = new Vector2(0f, -100f);
                celebrationGroup = group.GetComponent<CanvasGroup>();
                celebrationGroup.blocksRaycasts = false;
                celebrationGroup.interactable = false;
                celebrationViews = new SkeletonGraphic[celebrationCats.Length];
                for (var i = 0; i < celebrationCats.Length; i++)
                {
                    if (celebrationCats[i] == null) continue;
                    var cat = SkeletonGraphic.NewSkeletonGraphicGameObject(celebrationCats[i], celebrationRoot, celebrationMaterial);
                    cat.gameObject.name = "CelebrationCat" + (i + 1);
                    cat.raycastTarget = false;
                    cat.UnscaledTime = true;
                    cat.rectTransform.anchorMin = cat.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    cat.rectTransform.anchoredPosition = new Vector2((i - 1) * CelebrationCatSpacing, 0f);
                    cat.rectTransform.localScale = Vector3.one * CelebrationCatScale;
                    celebrationViews[i] = cat;
                }
                if (celebrationViews.Length > 1 && celebrationViews[1] != null)
                    celebrationViews[1].transform.SetAsLastSibling();
            }
            celebrationRoot.gameObject.SetActive(true);
            celebrationGroup.alpha = 1f;
            ((RectTransform)transform).GetWorldCorners(celebrationScreenCorners);
            // Start and finish below the actual viewport, including tall phones and tablets.
            var offscreenY = celebrationRoot.InverseTransformPoint(celebrationScreenCorners[0]).y - 520f;
            var sequenceSeconds = Mathf.Max(0.1f, celebrationSeconds);
            for (var i = 0; i < celebrationViews.Length; i++)
            {
                var cat = celebrationViews[i];
                if (cat == null) continue;
                cat.rectTransform.anchoredPosition = new Vector2((i - 1) * CelebrationCatSpacing, offscreenY);
                cat.rectTransform.localRotation = Quaternion.identity;
                if (cat.AnimationState == null) continue;
                cat.AnimationState.ClearTracks();
                cat.Skeleton.SetToSetupPose();
                var entry = cat.AnimationState.SetAnimation(0, "animation", false);
                entry.TimeScale = entry.Animation.Duration / (sequenceSeconds * CatJumpProfile(i).y);
                // Hold the pose below the screen until this cat actually takes off.
                cat.AnimationState.TimeScale = 0f;
            }
            for (var time = 0f; time < sequenceSeconds; time += Time.unscaledDeltaTime)
            {
                for (var i = 0; i < celebrationViews.Length; i++)
                {
                    var cat = celebrationViews[i];
                    if (cat == null) continue;
                    var profile = CatJumpProfile(i);
                    var elapsed = time - sequenceSeconds * profile.x;
                    var progress = Mathf.Clamp01(elapsed / (sequenceSeconds * profile.y));
                    if (cat.AnimationState != null) cat.AnimationState.TimeScale = elapsed < 0f ? 0f : 1f;
                    var distanceFromApex = 2f * progress - 1f;
                    var y = profile.z + (offscreenY - profile.z) * distanceFromApex * distanceFromApex;
                    var direction = i == 0 ? -1f : i == 1 ? 0f : 1f;
                    var drift = direction * 32f * Mathf.Sin(progress * Mathf.PI);
                    cat.rectTransform.anchoredPosition = new Vector2((i - 1) * CelebrationCatSpacing + drift, y);
                    cat.rectTransform.localRotation = Quaternion.Euler(0f, 0f, direction * 7f * Mathf.Sin(progress * Mathf.PI * 2f));
                }
                yield return null;
            }
            celebrationRoot.gameObject.SetActive(false);
        }

        // Delay, airtime and apex: center leads, then left, then right.
        // All three finish below the viewport within the existing celebration duration.
        private static Vector3 CatJumpProfile(int index) => index == 1
            ? new Vector3(0f, 0.74f, 0f)
            : index == 0 ? new Vector3(0.09f, 0.80f, -105f) : new Vector3(0.18f, 0.81f, -55f);

        private IEnumerator LandStar(RectTransform star, int index)
        {
            yield return new WaitForSecondsRealtime(popSeconds * 0.7f);   // đúng lúc sao nảy to nhất
            sounds.PlayStar(index);
            effects.Sparkle(star);
        }

        private IEnumerator RevealStar(int index, bool isEarned)
        {
            var rect = stars[index].rectTransform;
            // Finish the arrival pop before the idle animation takes over the transform.
            yield return Pop(rect, 0f);
            if (!isEarned) yield break;
            if (starGlints[index] == null) starGlints[index] = effects.CreateStarGlint(rect);
            var glint = starGlints[index];
            glint.gameObject.SetActive(true);
            var phase = index * 1.15f;
            for (var time = 0f; ; time += Time.unscaledDeltaTime)
            {
                var blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / 0.35f));
                var wave = Mathf.Sin(time * 2.1f + phase);
                rect.anchoredPosition = starHomes[index] + Vector2.up * (wave * 6f * blend);
                rect.localRotation = starRotations[index] * Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.7f + phase) * 2.5f * blend);
                rect.localScale = Vector3.one * (1f + wave * 0.035f * blend);
                var cycle = Mathf.Repeat(time + index * 0.85f, 3.6f);
                var shine = cycle < 0.55f ? Mathf.Sin(cycle / 0.55f * Mathf.PI) : 0f;
                glint.color = new Color(1f, 0.97f, 0.8f, shine * shine * 0.9f);
                glint.rectTransform.localScale = Vector3.one * (shine * 1.15f);
                glint.rectTransform.localRotation = Quaternion.Euler(0f, 0f, cycle * 85f);
                yield return null;
            }
        }

        private void OnCoinLanded(int landed)
        {
            coinCounter.Receive(coinsBeforeReward + (int)((long)rewardCoins * landed / flightCount));
            if (landed == flightCount)
            {
                collectionFinished = true;
                sounds.Play(SoundId.Cashout);
            }
            else sounds.PlayCoin(landed);
        }

        private IEnumerator Pop(RectTransform target, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            for (var time = 0f; time < popSeconds; time += Time.unscaledDeltaTime)
            {
                target.localScale = Vector3.one * popScale.Evaluate(time / popSeconds);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
