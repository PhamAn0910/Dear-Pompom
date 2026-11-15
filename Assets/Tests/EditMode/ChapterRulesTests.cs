using NUnit.Framework;

namespace DearPompom.Core.Tests
{
    public class ChapterRulesTests
    {
        [Test]
        public void Chapter2_NeedsThreeActiveDaysAndFourActivities()
        {
            var w = new World();
            w.Pomodoro(); w.Meditation(); w.Walk(); w.Theatre(); // 4 activities, 1 day
            ChapterStatus s = ChapterRules.Evaluate(w.Stats, 1);
            Assert.AreEqual(2, s.Next);
            Assert.IsFalse(s.CanUnlock);

            w.Clock.AdvanceDays(2); w.Pomodoro();   // gap day in between is fine
            w.Clock.AdvanceDays(1); w.Pomodoro();
            Assert.IsTrue(ChapterRules.Evaluate(w.Stats, 1).CanUnlock);
        }

        [Test]
        public void Chapter2_ThreeDaysOfOneActivity_IsNotEnough()
        {
            var w = new World();
            for (int i = 0; i < 3; i++) { w.Pomodoro(); w.Clock.AdvanceDays(1); }
            ChapterStatus s = ChapterRules.Evaluate(w.Stats, 1);
            Assert.IsFalse(s.CanUnlock);
            Assert.Greater(s.Progress, 0.5f);
            Assert.Less(s.Progress, 1f);
        }

        [Test]
        public void Chapter3_NeedsSevenDaysAndTenLetters()
        {
            var w = new World();
            for (int i = 0; i < 7; i++) { w.Journal(Mood.Happy); w.Clock.AdvanceDays(1); }
            Assert.IsFalse(ChapterRules.Evaluate(w.Stats, 2).CanUnlock); // 7 letters only
            w.Journal(Mood.Happy); w.Journal(Mood.Happy); w.Journal(Mood.Happy);
            Assert.IsTrue(ChapterRules.Evaluate(w.Stats, 2).CanUnlock);
        }

        [Test]
        public void Chapter4_NeedsAWeeklyGoal()
        {
            var w = new World();
            for (int i = 0; i < 14; i++) { w.Pomodoro(); w.Clock.AdvanceDays(1); }
            Assert.IsFalse(ChapterRules.Evaluate(w.Stats, 3, 0).CanUnlock);
            Assert.IsTrue(ChapterRules.Evaluate(w.Stats, 3, 1).CanUnlock);
        }

        [Test]
        public void LastChapter_HasNoNext()
        {
            var w = new World();
            ChapterStatus s = ChapterRules.Evaluate(w.Stats, 4);
            Assert.AreEqual(0, s.Next);
            Assert.IsFalse(s.CanUnlock);
            Assert.AreEqual(1f, s.Progress);
        }

        [Test]
        public void OutOfRangeChapter_IsClamped()
        {
            var w = new World();
            Assert.AreEqual(1, ChapterRules.Evaluate(w.Stats, -3).Current);
            Assert.AreEqual(4, ChapterRules.Evaluate(w.Stats, 12).Current);
        }
    }
}
