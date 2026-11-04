using System;

namespace Meowtosis.Gameplay
{
    // Lịch các sự kiện vui trong màn chơi: cứ một khoảng ngẫu nhiên lại đến lượt một sự kiện.
    // Chỉ đếm giờ lúc người chơi đang rảnh, nên sự kiện không bao giờ chen vào giữa một thao tác.
    public sealed class AmbientSchedule
    {
        private readonly float minimumSeconds;
        private readonly float maximumSeconds;
        private readonly Func<float> randomValue;
        private float secondsLeft;

        // `randomValue` trả một số từ 0 tới 1.
        public AmbientSchedule(float minimumSeconds, float maximumSeconds, Func<float> randomValue)
        {
            this.minimumSeconds = minimumSeconds;
            this.maximumSeconds = maximumSeconds;
            this.randomValue = randomValue;
            Restart();
        }

        // Trả true đúng một lần mỗi khi đến giờ, rồi tự đặt lại khoảng chờ mới.
        public bool Tick(float deltaSeconds, bool isIdle)
        {
            if (!isIdle) return false;
            secondsLeft -= deltaSeconds;
            if (secondsLeft > 0f) return false;
            Restart();
            return true;
        }

        public void Restart() => secondsLeft = minimumSeconds + (maximumSeconds - minimumSeconds) * randomValue();
    }
}
