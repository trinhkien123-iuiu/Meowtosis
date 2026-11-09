using System;
using System.Collections.Generic;
using System.Linq;

namespace Meowtosis.Gameplay
{
    // Tài nguyên và tiến độ của người chơi: tim, xu, booster, sao tốt nhất mỗi level, cơ chế đã học, cài đặt.
    // Số liệu theo prototype (docs/unity-handoff.md §5).
    public sealed class PlayerProgress
    {
        public const int MaximumHearts = 3;
        public const int StartingBoosters = 3;
        public const int CoinsPerStar = 10;
        // Mua booster theo gói: BoosterPrice xu được BoosterPackSize lượt.
        public const int BoosterPrice = 150;
        public const int BoosterPackSize = 3;
        public static readonly TimeSpan HeartRecoveryTime = TimeSpan.FromMinutes(10);

        private static readonly BoosterKind[] BoosterKinds = { BoosterKind.Mutate, BoosterKind.Transplant, BoosterKind.Revive, BoosterKind.Hint };

        private readonly Dictionary<BoosterKind, int> boosters;
        private readonly Dictionary<string, int> bestStars;
        private readonly HashSet<string> seenMechanics;

        private PlayerProgress(int hearts, DateTime? nextHeartAt, int coins, Dictionary<BoosterKind, int> boosters,
            Dictionary<string, int> bestStars, HashSet<string> seenMechanics)
        {
            Hearts = hearts;
            NextHeartAt = nextHeartAt;
            Coins = coins;
            this.boosters = boosters;
            this.bestStars = bestStars;
            this.seenMechanics = seenMechanics;
        }

        public int Hearts { get; private set; }

        // Lúc hồi thêm một tim (null khi tim đầy).
        public DateTime? NextHeartAt { get; private set; }

        public int Coins { get; private set; }

        public bool SoundOn { get; set; } = true;

        public bool MusicOn { get; set; } = true;

        // Level mở gần nhất, để lần sau vào thẳng level đó.
        public int LastLevelIndex { get; set; }

        // Đã đi hết màn hướng dẫn đầu game (Levels/Onboarding.txt) thì không chạy lại.
        public bool HasFinishedOnboarding { get; set; }

        // Avatar người chơi chọn (id trong danh mục avatar của giao diện); rỗng là avatar mặc định.
        public string AvatarId { get; set; } = "";

        public IReadOnlyDictionary<BoosterKind, int> Boosters => boosters;

        public IReadOnlyDictionary<string, int> BestStars => bestStars;

        public IEnumerable<string> SeenMechanics => seenMechanics;

        public static PlayerProgress CreateNew() => new PlayerProgress(MaximumHearts, null, 0,
            BoosterKinds.ToDictionary(kind => kind, kind => StartingBoosters), new Dictionary<string, int>(), new HashSet<string>());

        // Dựng lại từ dữ liệu đã lưu (IProgressStore).
        public static PlayerProgress Restore(int hearts, DateTime? nextHeartAt, int coins, IDictionary<BoosterKind, int> boosters,
            IDictionary<string, int> bestStars, IEnumerable<string> seenMechanics)
        {
            var counts = BoosterKinds.ToDictionary(kind => kind, kind => boosters.TryGetValue(kind, out var count) ? count : StartingBoosters);
            return new PlayerProgress(Math.Max(0, Math.Min(MaximumHearts, hearts)), nextHeartAt, Math.Max(0, coins), counts,
                new Dictionary<string, int>(bestStars), new HashSet<string>(seenMechanics));
        }

        // Hồi tim theo thời gian thực: mỗi HeartRecoveryTime thêm một tim cho tới khi đầy. Trả true nếu có thay đổi.
        public bool RecoverHearts(DateTime now)
        {
            var changed = false;
            while (Hearts < MaximumHearts && NextHeartAt.HasValue && now >= NextHeartAt.Value)
            {
                Hearts++;
                NextHeartAt = Hearts < MaximumHearts ? NextHeartAt.Value + HeartRecoveryTime : (DateTime?)null;
                changed = true;
            }
            return changed;
        }

        public TimeSpan? TimeToNextHeart(DateTime now) => NextHeartAt.HasValue ? NextHeartAt.Value - now : (TimeSpan?)null;

        public void LoseHeart(DateTime now)
        {
            if (Hearts == 0) return;
            if (Hearts == MaximumHearts) NextHeartAt = now + HeartRecoveryTime;
            Hearts--;
        }

        public void GainHeart()
        {
            Hearts = Math.Min(MaximumHearts, Hearts + 1);
            if (Hearts == MaximumHearts) NextHeartAt = null;
        }

        public void RefillHearts()
        {
            Hearts = MaximumHearts;
            NextHeartAt = null;
        }

        public int BoosterCount(BoosterKind kind) => boosters.TryGetValue(kind, out var count) ? count : 0;

        public bool TrySpendBooster(BoosterKind kind)
        {
            if (kind == BoosterKind.None) return true;
            if (BoosterCount(kind) == 0) return false;
            boosters[kind]--;
            return true;
        }

        public void RefundBooster(BoosterKind kind)
        {
            if (kind != BoosterKind.None) boosters[kind] = BoosterCount(kind) + 1;
        }

        public bool TryBuyBooster(BoosterKind kind)
        {
            if (kind == BoosterKind.None || Coins < BoosterPrice) return false;
            Coins -= BoosterPrice;
            boosters[kind] = BoosterCount(kind) + BoosterPackSize;
            return true;
        }

        public void RefillBoosters()
        {
            foreach (var kind in BoosterKinds) boosters[kind] = StartingBoosters;
        }

        public int BestStarsFor(string levelId) => bestStars.TryGetValue(levelId, out var stars) ? stars : 0;

        // Ghi một lần thắng: cộng xu theo số sao (chơi lại vẫn được thưởng đủ), giữ số sao cao nhất. Trả số xu được cộng.
        public int RecordWin(string levelId, int stars)
        {
            if (stars > BestStarsFor(levelId)) bestStars[levelId] = stars;
            var reward = stars * CoinsPerStar;
            Coins += reward;
            return reward;
        }

        // Bản thử: cộng xu.
        public void AddCoins(int coins) => Coins = Math.Max(0, Coins + coins);

        // Bản thử: coi như đã qua level (1 sao) để mở khoá level sau, không cộng xu. Level đã có sao thì giữ nguyên.
        public void MarkPassed(string levelId)
        {
            if (BestStarsFor(levelId) == 0) bestStars[levelId] = 1;
        }

        // Đánh dấu đã xem thẻ hướng dẫn của một cơ chế; trả true nếu đây là lần đầu.
        public bool MarkMechanicSeen(string mechanic) => seenMechanics.Add(mechanic);

        public bool HasSeenMechanic(string mechanic) => seenMechanics.Contains(mechanic);
    }
}
