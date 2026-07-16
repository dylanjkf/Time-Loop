using System.IO;
using System.Linq;
using NUnit.Framework;
using TimeLoop.Puzzle;
using TimeLoop.Save;
using UnityEngine;

namespace TimeLoop.Tests.EditMode
{
    /// <summary>
    /// Exercises TimeLoop.Save.SaveSystem / SaveData / PlayerProfile. SaveSystem persists to a real
    /// file under Application.persistentDataPath, so these tests back up whatever is already there
    /// in [SetUp] and restore it in [TearDown] to avoid clobbering the developer's own save data.
    /// </summary>
    [TestFixture]
    public class SaveSystemTests
    {
        private string _filePath;
        private bool _hadExistingFile;
        private string _backupJson;

        [SetUp]
        public void SetUp()
        {
            _filePath = Path.Combine(Application.persistentDataPath, "timeloop_save.json");
            _hadExistingFile = File.Exists(_filePath);
            if (_hadExistingFile)
            {
                _backupJson = File.ReadAllText(_filePath);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_hadExistingFile)
            {
                File.WriteAllText(_filePath, _backupJson);
            }
            else if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }

        [Test]
        public void SaveThenLoad_RoundTripsLevelProgressData()
        {
            var data = new SaveData();
            data.LevelProgress.Add(new LevelProgressRecord
            {
                LevelId = "1-01",
                BestStars = 3,
                FewestLoops = 2,
                FewestTicks = 40,
            });
            data.MusicEnabled = false;
            data.SelectedTheme = "Retro";
            data.GhostCooperationCount = 5;

            SaveSystem.Save(data);
            var loaded = SaveSystem.Load();

            Assert.AreEqual(1, loaded.LevelProgress.Count);
            Assert.AreEqual("1-01", loaded.LevelProgress[0].LevelId);
            Assert.AreEqual(3, loaded.LevelProgress[0].BestStars);
            Assert.AreEqual(2, loaded.LevelProgress[0].FewestLoops);
            Assert.AreEqual(40, loaded.LevelProgress[0].FewestTicks);
            Assert.IsFalse(loaded.MusicEnabled);
            Assert.AreEqual("Retro", loaded.SelectedTheme);
            Assert.AreEqual(5, loaded.GhostCooperationCount);
        }

        [Test]
        public void Load_WhenNoSaveFileExists_ReturnsFreshSaveDataWithoutThrowing()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            SaveData loaded = null;
            Assert.DoesNotThrow(() => loaded = SaveSystem.Load());

            Assert.IsNotNull(loaded);
            Assert.AreEqual(0, loaded.LevelProgress.Count);
            Assert.IsTrue(loaded.MusicEnabled);
            Assert.AreEqual("Default", loaded.SelectedTheme);
        }

        [Test]
        public void RecordLevelResult_NeverRegressesAnExistingBetterStarRating()
        {
            // Start from a guaranteed-clean SaveData on disk so PlayerProfile.LoadFromDisk() isn't
            // influenced by whatever the developer's own save happens to contain.
            SaveSystem.Save(new SaveData());
            var profile = PlayerProfile.LoadFromDisk();

            var threeStarResult = new LevelResult("1-01", loopsUsed: 2, ticksElapsed: 40, usedHint: false, stars: StarRating.Three);
            profile.RecordLevelResult(threeStarResult);

            var recordAfterThreeStars = profile.Raw.LevelProgress.First(r => r.LevelId == "1-01");
            Assert.AreEqual(3, recordAfterThreeStars.BestStars);

            // A subsequent, worse (1-star) result for the same level must not regress BestStars.
            var oneStarResult = new LevelResult("1-01", loopsUsed: 5, ticksElapsed: 90, usedHint: true, stars: StarRating.One);
            profile.RecordLevelResult(oneStarResult);

            var recordAfterOneStar = profile.Raw.LevelProgress.First(r => r.LevelId == "1-01");
            Assert.AreEqual(3, recordAfterOneStar.BestStars,
                "RecordLevelResult must never lower BestStars below a previously recorded better result.");
        }

        [Test]
        public void RecordLevelResult_TracksFewestLoopsAndTicksIndependentlyOfBestStars()
        {
            SaveSystem.Save(new SaveData());
            var profile = PlayerProfile.LoadFromDisk();

            // First attempt: 3 stars but takes 5 loops / 90 ticks.
            profile.RecordLevelResult(new LevelResult("1-02", loopsUsed: 5, ticksElapsed: 90, usedHint: false, stars: StarRating.Three));
            // Second attempt: fewer loops/ticks but only 1 star (e.g. used a hint) — FewestLoops/FewestTicks
            // are tracked independently of BestStars per PlayerProfile's own design-choice doc comment.
            profile.RecordLevelResult(new LevelResult("1-02", loopsUsed: 2, ticksElapsed: 30, usedHint: true, stars: StarRating.One));

            var record = profile.Raw.LevelProgress.First(r => r.LevelId == "1-02");
            Assert.AreEqual(3, record.BestStars, "Best star rating must still stay at its highest-ever value.");
            Assert.AreEqual(2, record.FewestLoops, "Fewest loops should update independently, even from a lower-star run.");
            Assert.AreEqual(30, record.FewestTicks, "Fewest ticks should update independently, even from a lower-star run.");
        }

        [Test]
        public void PlayerProfile_TotalStarsAndIsLevelCompleted_ReflectRecordedResults()
        {
            SaveSystem.Save(new SaveData());
            var profile = PlayerProfile.LoadFromDisk();

            Assert.IsFalse(profile.IsLevelCompleted("1-01"));

            profile.RecordLevelResult(new LevelResult("1-01", loopsUsed: 1, ticksElapsed: 10, usedHint: false, stars: StarRating.Two));
            profile.RecordLevelResult(new LevelResult("1-02", loopsUsed: 1, ticksElapsed: 10, usedHint: false, stars: StarRating.Three));

            Assert.IsTrue(profile.IsLevelCompleted("1-01"));
            // TotalStars sums BestStars across every recorded level: 2 (1-01) + 3 (1-02) = 5.
            Assert.AreEqual(5, profile.TotalStars());
        }
    }
}
