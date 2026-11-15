using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DearPompom.Core.Tests
{
    public class ProgressStatsTests
    {
        [Test]
        public void EmptyLog_GivesZerosAndNulls()
        {
            var w = new World();
            ProgressStats s = w.Stats;
            Assert.IsNull(s.LastRecord());
            Assert.IsNull(s.Last(ActivityType.Pomodoro));
            Assert.AreEqual(0, s.TotalActiveDays());
            Assert.AreEqual(0, s.DistinctTypesTried());
            Assert.AreEqual(-1, s.DaysSince(ActivityType.Journal));
            Assert.AreEqual(-1f, s.CompletionRate(ActivityType.Pomodoro, DateTime.MinValue, DateTime.MaxValue));
            Assert.AreEqual(Mood.None, s.TopMood(DateTime.MinValue, DateTime.MaxValue));
            Assert.AreEqual(7, s.DailyBuckets(7).Count);
        }

        [Test]
        public void ActiveDays_CountsDistinctDays_NotConsecutive()
        {
            var w = new World();
            w.Pomodoro(); w.Pomodoro();        // day 1, twice
            w.Clock.AdvanceDays(3); w.Walk();  // day 4 (gap must not matter)
            w.Clock.AdvanceDays(1); w.Math();  // day 5
            Assert.AreEqual(3, w.Stats.TotalActiveDays());
        }

        [Test]
        public void MinutesAndCounts_ForToday()
        {
            var w = new World();
            w.Pomodoro(25); w.Pomodoro(10, false); w.Meditation(5);
            ProgressStats s = w.Stats;
            Assert.AreEqual(35f, s.MinutesToday(ActivityType.Pomodoro));
            Assert.AreEqual(2, s.CountOn(s.Today, ActivityType.Pomodoro));
            Assert.AreEqual(1, s.CountOn(s.Today, ActivityType.Pomodoro, true));
            Assert.AreEqual(0.5f, s.CompletionRate(ActivityType.Pomodoro, s.Today, s.Today));
        }

        [Test]
        public void DailyBuckets_PlaceRecordsOnTheRightDay()
        {
            var w = new World();
            w.Pomodoro(25);
            w.Clock.AdvanceDays(2);
            w.Meditation(5); w.Pomodoro(15);
            List<DayBucket> buckets = w.Stats.DailyBuckets(7);

            Assert.AreEqual(7, buckets.Count);
            Assert.AreEqual(w.Stats.Today, buckets[6].Date);
            Assert.AreEqual(15f, buckets[6].Minutes(ActivityType.Pomodoro));
            Assert.AreEqual(5f, buckets[6].Minutes(ActivityType.Meditation));
            Assert.AreEqual(20f, buckets[6].TotalMinutes);
            Assert.AreEqual(0f, buckets[5].TotalMinutes);
            Assert.AreEqual(25f, buckets[4].Minutes(ActivityType.Pomodoro));
        }

        [Test]
        public void DailyBuckets_IgnoreRecordsOutsideTheWindow()
        {
            var w = new World();
            w.Pomodoro(25);
            w.Clock.AdvanceDays(30);
            foreach (DayBucket b in w.Stats.DailyBuckets(7)) Assert.AreEqual(0f, b.TotalMinutes);
        }

        [Test]
        public void LastRecord_IsByTime_NotListOrder()
        {
            var w = new World();
            w.Pomodoro();
            // A record appended later but stamped earlier (e.g. merged data).
            w.Save.records.Add(new ActivityRecord { id = "old", type = ActivityType.Theatre, timestamp = Timestamp.Format(w.Clock.Now.AddDays(-5)) });
            Assert.AreEqual(ActivityType.Pomodoro, w.Stats.LastRecord().type);
            Assert.AreEqual(ActivityType.Theatre, w.Stats.PreviousRecord().type);
        }

        [Test]
        public void DaysSince_CountsCalendarDays()
        {
            var w = new World(new FakeClock(2026, 11, 16, 23, 30));
            w.Meditation();
            w.Clock.AdvanceHours(1); // 00:30 next day: one calendar day later
            Assert.AreEqual(1, w.Stats.DaysSince(ActivityType.Meditation));
        }

        [Test]
        public void JournalEntriesWritten_CountsOnlyNewEntries()
        {
            var w = new World();
            w.Journal(Mood.Happy);
            w.Log.Add(ActivityType.Journal, 3, kind: ActivityKind.JournalSession);
            w.Log.Add(ActivityType.Journal, 0, mood: Mood.Sad, kind: ActivityKind.JournalUpdate);
            Assert.AreEqual(1, w.Stats.JournalEntriesWritten());
        }

        [Test]
        public void Moods_AreCountedAsCategories()
        {
            var w = new World();
            w.Journal(Mood.Happy); w.Journal(Mood.Happy); w.Journal(Mood.Tired);
            ProgressStats s = w.Stats;
            Dictionary<Mood, int> counts = s.MoodCounts(s.Today, s.Today);
            Assert.AreEqual(2, counts[Mood.Happy]);
            Assert.AreEqual(1, counts[Mood.Tired]);
            Assert.AreEqual(Mood.Happy, s.TopMood(s.Today, s.Today));
        }

        [Test]
        public void LowMoodDays_UsesTheLastMoodOfEachDay()
        {
            var w = new World();
            w.Journal(Mood.Sad); w.Clock.AdvanceHours(1); w.Journal(Mood.Happy); // day ends happy: not low
            w.Clock.AdvanceDays(1); w.Journal(Mood.Tired);
            w.Clock.AdvanceDays(1); w.Journal(Mood.Sad);
            Assert.AreEqual(2, w.Stats.LowMoodDays(5));
        }

        [Test]
        public void BestScore_CanFilterByDifficulty()
        {
            var w = new World();
            w.Cards(40, "Easy"); w.Cards(90, "Easy"); w.Cards(60, "Hard");
            Assert.AreEqual(90, w.Stats.BestScore(ActivityType.MemoryCard));
            Assert.AreEqual(60, w.Stats.BestScore(ActivityType.MemoryCard, "Hard"));
            Assert.AreEqual(0, w.Stats.BestScore(ActivityType.MathGame));
        }

        [Test]
        public void Week_ComparesRollingSevenDayWindows()
        {
            var w = new World();
            w.Pomodoro(20);                       // 8 days before "now" -> last week
            w.Clock.AdvanceDays(8);
            w.Pomodoro(25); w.Journal(Mood.Happy);
            WeekSummary thisWeek = w.Stats.Week(0);
            WeekSummary lastWeek = w.Stats.Week(1);

            Assert.AreEqual(25f, thisWeek.FocusMinutes);
            Assert.AreEqual(1, thisWeek.ActiveDays);
            Assert.AreEqual(Mood.Happy, thisWeek.TopMood);
            Assert.AreEqual(20f, lastWeek.FocusMinutes);
            Assert.AreEqual(1, lastWeek.ActiveDays);
        }

        [Test]
        public void RecordsWithBrokenTimestamps_AreIgnoredEverywhere()
        {
            var w = new World();
            w.Save.records.Add(new ActivityRecord { type = ActivityType.Pomodoro, timestamp = "???", durationMinutes = 99 });
            w.Save.records.Add(null);
            ProgressStats s = w.Stats;
            Assert.AreEqual(0, s.TotalActiveDays());
            Assert.IsFalse(s.HasTried(ActivityType.Pomodoro));
            Assert.IsNull(s.LastRecord());
        }
    }
}
