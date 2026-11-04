using Meowtosis.Gameplay;
using NUnit.Framework;

namespace Meowtosis.Tests
{
    public class AmbientScheduleTests
    {
        [Test]
        public void FiresOnlyAfterTheChosenWait()
        {
            var schedule = new AmbientSchedule(60f, 120f, () => 0.5f);
            Assert.That(schedule.Tick(89f, true), Is.False);
            Assert.That(schedule.Tick(1f, true), Is.True, "chờ đúng 90 giây: giữa 60 và 120");
        }

        [Test]
        public void WaitStaysWithinTheRange()
        {
            var shortest = new AmbientSchedule(60f, 120f, () => 0f);
            Assert.That(shortest.Tick(59f, true), Is.False);
            Assert.That(shortest.Tick(1f, true), Is.True);

            var longest = new AmbientSchedule(60f, 120f, () => 1f);
            Assert.That(longest.Tick(119f, true), Is.False);
            Assert.That(longest.Tick(1f, true), Is.True);
        }

        [Test]
        public void BusyTimeDoesNotCount()
        {
            var schedule = new AmbientSchedule(60f, 60f, () => 0f);
            Assert.That(schedule.Tick(1000f, false), Is.False);
            Assert.That(schedule.Tick(59f, true), Is.False, "lúc bận không được tính vào thời gian chờ");
            Assert.That(schedule.Tick(1f, true), Is.True);
        }

        [Test]
        public void FiresOnceThenWaitsAgain()
        {
            var schedule = new AmbientSchedule(60f, 60f, () => 0f);
            Assert.That(schedule.Tick(60f, true), Is.True);
            Assert.That(schedule.Tick(1f, true), Is.False);
            Assert.That(schedule.Tick(59f, true), Is.True);
        }
    }
}
