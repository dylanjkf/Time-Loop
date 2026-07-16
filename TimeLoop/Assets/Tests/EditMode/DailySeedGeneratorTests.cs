using System;
using NUnit.Framework;
using TimeLoop.Daily;

namespace TimeLoop.Tests.EditMode
{
    [TestFixture]
    public class DailySeedGeneratorTests
    {
        [Test]
        public void SeedForDate_SameCalendarDate_AlwaysYieldsTheSameSeed()
        {
            var date = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc);

            var seedA = DailySeedGenerator.SeedForDate(date);
            var seedB = DailySeedGenerator.SeedForDate(date);

            Assert.AreEqual(seedA, seedB);

            // SeedForDate uses utcDate.Date internally, so a different time-of-day on the same
            // calendar date must still produce the identical seed.
            var laterSameDay = new DateTime(2026, 7, 16, 23, 59, 59, DateTimeKind.Utc);
            Assert.AreEqual(seedA, DailySeedGenerator.SeedForDate(laterSameDay));
        }

        [Test]
        public void SeedForDate_DatesOneDayApart_YieldSeedsExactlyOneApart()
        {
            var day1 = new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc);
            var day2 = day1.AddDays(1);

            var seed1 = DailySeedGenerator.SeedForDate(day1);
            var seed2 = DailySeedGenerator.SeedForDate(day2);

            Assert.AreEqual(1, seed2 - seed1);
        }

        [Test]
        public void SeedForDate_EpochDate_IsZero_AndNextDayIsOne()
        {
            // DailySeedGenerator's Epoch is 2024-01-01 UTC, and SeedForDate returns the whole
            // number of days between utcDate.Date and Epoch, so the epoch date itself is seed 0
            // and the following day is seed 1.
            var epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var epochPlusOneDay = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);

            Assert.AreEqual(0, DailySeedGenerator.SeedForDate(epoch));
            Assert.AreEqual(1, DailySeedGenerator.SeedForDate(epochPlusOneDay));
        }

        [Test]
        public void PuzzleNumberForDate_IsAlwaysSeedForDatePlusOne()
        {
            var date1 = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);
            var date2 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var date3 = new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            foreach (var date in new[] { date1, date2, date3 })
            {
                Assert.AreEqual(DailySeedGenerator.SeedForDate(date) + 1, DailySeedGenerator.PuzzleNumberForDate(date));
            }
        }
    }
}
