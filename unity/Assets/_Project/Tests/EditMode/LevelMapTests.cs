using System.Collections.Generic;
using System.Linq;
using Meowtosis.Domain;
using Meowtosis.Gameplay;
using NUnit.Framework;

namespace Meowtosis.Tests
{
    public class LevelMapTests
    {
        private static List<LevelDefinition> Levels(int count)
        {
            var levels = new List<LevelDefinition>();
            for (var index = 0; index < count; index++)
            {
                var level = new LevelDefinition { Id = "level-" + index };
                level.Rows.Add("A.");
                levels.Add(level);
            }
            return levels;
        }

        [Test]
        public void NewPlayerOnlyHasTheFirstLevelOpen()
        {
            var entries = LevelMap.Build(Levels(3), PlayerProgress.CreateNew());
            Assert.That(entries.Select(entry => entry.IsUnlocked), Is.EqualTo(new[] { true, false, false }));
            Assert.That(entries[0].IsCurrent, Is.True);
            Assert.That(entries[0].Stars, Is.EqualTo(0));
        }

        [Test]
        public void WinningOpensTheNextLevelAndKeepsWonLevelsReplayable()
        {
            var levels = Levels(3);
            var progress = PlayerProgress.CreateNew();
            progress.RecordWin("level-0", 2);
            var entries = LevelMap.Build(levels, progress);
            Assert.That(entries.Select(entry => entry.IsUnlocked), Is.EqualTo(new[] { true, true, false }));
            Assert.That(entries[0].Stars, Is.EqualTo(2));
            Assert.That(entries[0].IsCurrent, Is.False);
            Assert.That(entries[1].IsCurrent, Is.True);
        }

        [Test]
        public void ReplayingAnOldLevelDoesNotMoveTheCurrentLevelBack()
        {
            var levels = Levels(3);
            var progress = PlayerProgress.CreateNew();
            progress.RecordWin("level-0", 3);
            progress.RecordWin("level-1", 1);
            progress.LastLevelIndex = 0;
            progress.RecordWin("level-0", 1);
            Assert.That(LevelMap.CurrentIndex(levels, progress), Is.EqualTo(2));
            Assert.That(LevelMap.Build(levels, progress)[0].Stars, Is.EqualTo(3), "giữ số sao cao nhất");
        }

        [Test]
        public void WinningEveryLevelKeepsTheLastLevelAsCurrent()
        {
            var levels = Levels(2);
            var progress = PlayerProgress.CreateNew();
            progress.RecordWin("level-0", 3);
            progress.RecordWin("level-1", 3);
            Assert.That(LevelMap.CurrentIndex(levels, progress), Is.EqualTo(1));
            Assert.That(LevelMap.Build(levels, progress).All(entry => entry.IsUnlocked), Is.True);
        }

        [Test]
        public void OnlyTheFirstLevelWithAMechanicIsMarkedNew()
        {
            var levels = Levels(6);
            levels[2].Goal.Add("A.");
            levels[4].Goal.Add("A.");
            levels[5].Goal.Add("A.");
            levels[5].Membranes.Add(new Membrane(0, 0, Direction.Right));
            var entries = LevelMap.Build(levels, PlayerProgress.CreateNew());
            // Level đầu không đánh dấu: cơ chế cơ bản đã dạy ở màn hướng dẫn. Hộp mới ở level 3, màng mới ở level 6.
            Assert.That(entries.Select(entry => entry.IntroducesMechanic), Is.EqualTo(new[] { false, false, true, false, false, true }));
        }
    }
}
