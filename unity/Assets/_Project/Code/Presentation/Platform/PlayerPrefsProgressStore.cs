using System;
using System.Collections.Generic;
using System.Linq;
using Meowtosis.Gameplay;
using UnityEngine;

namespace Meowtosis.Presentation
{
    // Lưu tiến độ người chơi vào PlayerPrefs dạng JSON. Dữ liệu hỏng hoặc chưa có thì bắt đầu mới.
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        private const string Key = "meowtosis-progress-v1";

        public PlayerProgress Load()
        {
            var json = PlayerPrefs.GetString(Key, "");
            if (json.Length == 0) return PlayerProgress.CreateNew();
            try
            {
                return JsonUtility.FromJson<SavedProgress>(json).ToProgress();
            }
            catch (ArgumentException)
            {
                return PlayerProgress.CreateNew();
            }
        }

        public void Save(PlayerProgress progress)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(SavedProgress.From(progress)));
            PlayerPrefs.Save();
        }

        // Dạng lưu của PlayerProgress. JsonUtility chỉ đọc ghi field public nên lớp này dùng field public.
        [Serializable]
        private sealed class SavedProgress
        {
            public int hearts;
            public long nextHeartAtTicks;
            public int coins;
            public int mutate;
            public int transplant;
            public int revive;
            public int hint = PlayerProgress.StartingBoosters;   // save cũ chưa có trường này thì được đủ lượt ban đầu
            public List<string> starLevels = new List<string>();
            public List<int> starCounts = new List<int>();
            public List<string> seenMechanics = new List<string>();
            public bool soundOn = true;
            public bool musicOn = true;
            public int lastLevelIndex;
            public bool finishedOnboarding;   // save cũ chưa có trường này thì chạy hướng dẫn đầu game một lần

            public static SavedProgress From(PlayerProgress progress) => new SavedProgress
            {
                hearts = progress.Hearts,
                nextHeartAtTicks = progress.NextHeartAt?.Ticks ?? 0,
                coins = progress.Coins,
                mutate = progress.BoosterCount(BoosterKind.Mutate),
                transplant = progress.BoosterCount(BoosterKind.Transplant),
                revive = progress.BoosterCount(BoosterKind.Revive),
                hint = progress.BoosterCount(BoosterKind.Hint),
                starLevels = progress.BestStars.Keys.ToList(),
                starCounts = progress.BestStars.Values.ToList(),
                seenMechanics = progress.SeenMechanics.ToList(),
                soundOn = progress.SoundOn,
                musicOn = progress.MusicOn,
                lastLevelIndex = progress.LastLevelIndex,
                finishedOnboarding = progress.HasFinishedOnboarding,
            };

            public PlayerProgress ToProgress()
            {
                var boosters = new Dictionary<BoosterKind, int>
                {
                    [BoosterKind.Mutate] = mutate,
                    [BoosterKind.Transplant] = transplant,
                    [BoosterKind.Revive] = revive,
                    [BoosterKind.Hint] = hint,
                };
                var stars = new Dictionary<string, int>();
                for (var index = 0; index < Math.Min(starLevels.Count, starCounts.Count); index++) stars[starLevels[index]] = starCounts[index];
                var nextHeartAt = nextHeartAtTicks > 0 ? new DateTime(nextHeartAtTicks, DateTimeKind.Utc) : (DateTime?)null;
                var progress = PlayerProgress.Restore(hearts, nextHeartAt, coins, boosters, stars, seenMechanics);
                progress.SoundOn = soundOn;
                progress.MusicOn = musicOn;
                progress.LastLevelIndex = lastLevelIndex;
                progress.HasFinishedOnboarding = finishedOnboarding;
                return progress;
            }
        }
    }
}
