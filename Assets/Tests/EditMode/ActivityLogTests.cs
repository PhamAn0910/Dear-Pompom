using System;
using NUnit.Framework;

namespace DearPompom.Core.Tests
{
    public class ActivityLogTests
    {
        [Test]
        public void Add_StampsIdTimeAndChapter()
        {
            var w = new World();
            w.Save.chapter = 2;
            ActivityRecord r = w.Pomodoro(25);

            Assert.IsFalse(string.IsNullOrEmpty(r.id));
            Assert.AreEqual(w.Clock.Now, r.Time);
            Assert.AreEqual(2, r.chapter);
            Assert.AreEqual(25f, r.targetMinutes);
            Assert.IsTrue(r.completed);
            Assert.AreEqual(1, w.Save.records.Count);
        }

        [Test]
        public void Add_GivesUniqueIds()
        {
            var w = new World();
            Assert.AreNotEqual(w.Pomodoro().id, w.Pomodoro().id);
        }

        [Test]
        public void Add_RaisesRecorded()
        {
            var w = new World();
            ActivityRecord seen = null;
            w.Log.Recorded += r => seen = r;
            ActivityRecord added = w.Walk();
            Assert.AreSame(added, seen);
        }

        [Test]
        public void Add_ClampsBadDurations()
        {
            var w = new World();
            Assert.AreEqual(0f, w.Log.Add(ActivityType.Meditation, -5f).durationMinutes);
            Assert.AreEqual(0f, w.Log.Add(ActivityType.Meditation, float.NaN).durationMinutes);
            Assert.AreEqual(0f, w.Log.Add(ActivityType.Meditation, float.PositiveInfinity).durationMinutes);
        }

        [Test]
        public void Add_RejectsUnknownType()
        {
            var w = new World();
            Assert.Throws<ArgumentException>(() => w.Log.Add(ActivityType.Unknown, 1f));
        }

        [Test]
        public void Add_KeepsMoodOnlyOnJournal()
        {
            var w = new World();
            Assert.AreEqual(Mood.Sad, w.Journal(Mood.Sad).mood);
            Assert.AreEqual(Mood.None, w.Log.Add(ActivityType.Pomodoro, 1, mood: Mood.Sad).mood);
        }

        [Test]
        public void Record_WithUnreadableTimestamp_IsFlagged()
        {
            var r = new ActivityRecord { timestamp = "not a date" };
            Assert.IsFalse(r.HasValidTime);
        }

        [Test]
        public void Timestamp_RoundTripsWithOffset()
        {
            DateTimeOffset original = FakeClock.At(2026, 3, 1, 23, 59);
            DateTimeOffset parsed;
            Assert.IsTrue(Timestamp.TryParse(Timestamp.Format(original), out parsed));
            Assert.AreEqual(original, parsed);
            Assert.AreEqual(original.Offset, parsed.Offset);
        }

        [Test]
        public void LocalDate_UsesTheRecordedWallClock_NotUtc()
        {
            // 00:30 at +07:00 is still "yesterday" in UTC; the player's day must win.
            var r = new ActivityRecord { timestamp = "2026-11-16T00:30:00.0000000+07:00" };
            Assert.AreEqual(new DateTime(2026, 11, 16), r.LocalDate);
        }
    }
}
