using Meowtosis.Domain;

namespace Meowtosis.Gameplay
{
    // Kết quả một nước đi của người chơi, kèm thay đổi tài nguyên (tim, xu) để giao diện hiện ra.
    public sealed class PlayOutcome
    {
        public PlayOutcome(MoveResult move, bool lostHeart, bool sprungTrap, int stars, int coinsEarned)
        {
            Move = move;
            LostHeart = lostHeart;
            SprungTrap = sprungTrap;
            Stars = stars;
            CoinsEarned = coinsEarned;
        }

        public MoveResult Move { get; }

        // Nước này làm thua nên mất một tim.
        public bool LostHeart { get; }

        // Nước này là bẫy dạy luật: thua nhưng không mất tim, xong thì gọi LevelRun.FinishTrap.
        public bool SprungTrap { get; }

        // Số sao nếu nước này thắng, 0 nếu chưa.
        public int Stars { get; }

        public int CoinsEarned { get; }
    }

    // Một lượt chơi level có tính tài nguyên: tiêu booster, thua mất tim, thắng được xu và sao, hoàn tác trả booster,
    // hồi sinh quay lại trước nước thua và trả tim. Lưu tiến độ sau mỗi thay đổi.
    public sealed class LevelRun
    {
        // Bẫy của level dạy cơ chế này; đã biết cơ chế rồi thì bỏ qua bẫy, kể cả khi chơi lại level.
        public const Mechanic TrapMechanic = Mechanic.Clear;

        private readonly IProgressStore store;
        private readonly IClock clock;
        private bool isTrapPending;

        public LevelRun(LevelDefinition definition, PlayerProgress progress, IProgressStore store, IClock clock)
        {
            Session = new GameSession(definition);
            Progress = progress;
            this.store = store;
            this.clock = clock;
            // Chốt ngay từ đầu vì thẻ cơ chế hiện trước khi chơi cũng ghi nhận là đã biết cơ chế.
            isTrapPending = definition.Trap != null && !progress.HasSeenMechanic(TrapMechanic.ToString());
        }

        public GameSession Session { get; }

        public PlayerProgress Progress { get; }

        // Sao chỉ tính theo số lần vuốt: nước dùng booster không bị trừ bước.
        public StarMeter Stars => StarMeter.For(Session.SwipesMade, Session.Definition.Par);

        public bool CanUndo => Session.Status == SessionStatus.Playing && Session.CanUndo;

        public bool CanRevive => Session.Status == SessionStatus.Lost && Session.CanUndo && Progress.BoosterCount(BoosterKind.Revive) > 0;

        public bool IsTrapActive => isTrapPending;

        public void EnableTrap() => isTrapPending = Session.Definition.Trap != null;

        // Bước người chơi phải làm theo lúc này: bẫy trước, rồi các bước hướng dẫn. Null nếu được đi tự do.
        public TutorialStep GuidedStep
        {
            get
            {
                if (Session.Status != SessionStatus.Playing) return null;
                if (IsTrapActive) return Session.MovesMade == 0 ? Session.Definition.Trap : null;
                var tutorial = Session.Definition.Tutorial;
                return Session.MovesMade < tutorial.Count ? tutorial[Session.MovesMade] : null;
            }
        }

        public bool TryPlay(IGameCommand command, out PlayOutcome outcome)
        {
            outcome = null;
            if (Progress.BoosterCount(command.Booster) == 0 && command.Booster != BoosterKind.None) return false;
            if (!Session.TryPlay(command, out var move)) return false;
            Progress.TrySpendBooster(command.Booster);

            var lostHeart = false;
            var sprungTrap = false;
            var stars = 0;
            var coins = 0;
            if (Session.Status == SessionStatus.Lost && IsTrapActive)
            {
                sprungTrap = true;
            }
            else if (Session.Status == SessionStatus.Lost)
            {
                Progress.LoseHeart(clock.UtcNow);
                lostHeart = true;
            }
            else if (Session.Status == SessionStatus.Won)
            {
                stars = StarRating.For(Session.SwipesMade, Session.Definition.Par);
                coins = Progress.RecordWin(Session.Definition.Id, stars);
            }
            store.Save(Progress);
            outcome = new PlayOutcome(move, lostHeart, sprungTrap, stars, coins);
            return true;
        }

        // Lùi một nước khi đang chơi; trả lại booster nước đó đã tiêu (refunded, None nếu là vuốt thường).
        public bool TryUndo(out BoosterKind refunded)
        {
            refunded = BoosterKind.None;
            if (!CanUndo || !Session.TryUndo(out refunded)) return false;
            Progress.RefundBooster(refunded);
            store.Save(Progress);
            return true;
        }

        // Hồi sinh khi vừa thua: tiêu một lượt Hồi sinh, quay lại trước nước thua, trả booster của nước đó và trả tim.
        public bool TryRevive()
        {
            if (!CanRevive || !Session.TryUndo(out var refunded)) return false;
            Progress.TrySpendBooster(BoosterKind.Revive);
            Progress.RefundBooster(refunded);
            Progress.GainHeart();
            store.Save(Progress);
            return true;
        }

        public void Restart()
        {
            Session.Restart();
            if (Session.Definition.Trap != null && Progress.BestStarsFor(Session.Definition.Id) == 0)
                isTrapPending = true;
        }

        // Người chơi đã thấy bẫy: ghi nhận đã biết cơ chế và đưa bàn về đầu để chơi thật.
        public void FinishTrap()
        {
            isTrapPending = false;
            Progress.MarkMechanicSeen(TrapMechanic.ToString());
            Session.Restart();
            store.Save(Progress);
        }
    }
}
