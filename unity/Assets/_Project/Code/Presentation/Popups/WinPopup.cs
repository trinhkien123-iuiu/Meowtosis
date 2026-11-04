using System;
using System.Collections;
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

    // Popup thắng: pháo giấy, tiêu đề nảy vào, từng ngôi sao đáp xuống toé sáng và kêu cao dần, rồi ô tổng xu bật ra,
    // icon xu với số thưởng nảy lên một nhịp, sau đó xu bay nối đuôi về ô tổng xu trong khi số thưởng mờ dần và trôi lên.
    // Hai nút bấm được ngay từ đầu, không phải chờ diễn xong.
    public sealed class WinPopup : PopupView, IWinPopup
    {
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

        [Header("Nhịp diễn")]
        [SerializeField] private float titleDelay = 0.1f;
        [SerializeField] private float firstStarDelay = 0.35f;
        [SerializeField] private float starStep = 0.26f;
        [SerializeField] private float popSeconds = 0.4f;
        [SerializeField] private float counterDelay = 0.2f;
        [Tooltip("Số thưởng nảy lên xong giữ bao lâu thì xu bắt đầu bay.")]
        [SerializeField] private float coinDelay = 0.45f;
        [SerializeField] private float rewardLeaveSeconds = 0.45f;
        [Tooltip("Số thưởng trôi lên bao xa trong lúc mờ dần.")]
        [SerializeField] private float rewardRise = 70f;
        [SerializeField] private float coinStagger = 0.06f;
        [SerializeField, Range(4, 30)] private int flyingCoinCount = 12;
        [SerializeField] private AnimationCurve popScale = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.7f, 1.25f), new Keyframe(1f, 1f));

        public event Action ContinueClicked;

        public event Action BackClicked;

        private Vector2 rewardHome;

        public void Show(int earnedStars, int coinsEarned, int totalCoins)
        {
            effects.Clear();
            coinCounter.Hide();
            coinsText.text = GameTexts.CoinReward(coinsEarned);
            title.localScale = Vector3.zero;
            reward.localScale = Vector3.zero;
            reward.anchoredPosition = rewardHome;
            rewardGroup.alpha = 1f;
            foreach (var star in stars) star.rectTransform.localScale = Vector3.zero;
            Open();
            StartCoroutine(Pop(title, titleDelay));
            StartCoroutine(PlaySequence(earnedStars, coinsEarned, totalCoins));
        }

        private void Awake()
        {
            rewardHome = reward.anchoredPosition;
            continueButton.onClick.AddListener(() => ContinueClicked?.Invoke());
            backButton.onClick.AddListener(() => BackClicked?.Invoke());
        }

        private IEnumerator PlaySequence(int earnedStars, int coinsEarned, int totalCoins)
        {
            effects.Confetti();
            yield return new WaitForSecondsRealtime(firstStarDelay);
            for (var index = 0; index < stars.Length; index++)
            {
                var isEarned = index < earnedStars;
                stars[index].sprite = isEarned ? litStar : emptyStar;
                StartCoroutine(Pop(stars[index].rectTransform, 0f));
                if (isEarned) StartCoroutine(LandStar(stars[index].rectTransform, index));
                yield return new WaitForSecondsRealtime(starStep);
            }

            yield return new WaitForSecondsRealtime(counterDelay);
            var startCoins = totalCoins - coinsEarned;
            coinCounter.Appear(startCoins);
            if (coinsEarned <= 0) yield break;

            sounds.PlayCoin(0);
            yield return Pop(reward, 0f);
            yield return new WaitForSecondsRealtime(coinDelay);
            effects.CoinFlight(coinSource, coinCounter.Icon, flyingCoinCount, coinStagger,
                landed => OnCoinLanded(landed, startCoins, coinsEarned));
            yield return RewardLeave();
        }

        private IEnumerator RewardLeave()
        {
            for (var time = 0f; time < rewardLeaveSeconds; time += Time.unscaledDeltaTime)
            {
                var progress = time / rewardLeaveSeconds;
                rewardGroup.alpha = 1f - progress;
                reward.anchoredPosition = rewardHome + Vector2.up * (rewardRise * progress);
                yield return null;
            }
            rewardGroup.alpha = 0f;
        }

        private IEnumerator LandStar(RectTransform star, int index)
        {
            yield return new WaitForSecondsRealtime(popSeconds * 0.7f);   // đúng lúc sao nảy to nhất
            sounds.PlayStar(index);
            effects.Sparkle(star);
        }

        private void OnCoinLanded(int landed, int startCoins, int coinsEarned)
        {
            coinCounter.Receive(startCoins + coinsEarned * landed / flyingCoinCount);
            if (landed == flyingCoinCount) sounds.Play(SoundId.Cashout);
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
