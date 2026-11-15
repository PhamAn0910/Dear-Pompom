using System.Collections.Generic;
using NUnit.Framework;

namespace DearPompom.Core.Tests
{
    /// <summary>The real starter rules, each against a small fake log and a fake clock (PRD R6).</summary>
    public class RecommendationRuleTests
    {
        private static float Strength(RecommendationRule rule, World w) { return rule.Strength(w.Context()); }

        // ---------- regressions for the two 1.0 bugs ----------

        [Test]
        public void B1_AfterMeditating_TheWalkSuggestionDoesNotLoopForever()
        {
            // 1.0: once you had meditated, every single recommendation was
            // "You haven't tried VirtualWalk yet!" because the rule looked for a name the game never logged.
            var w = new World();
            w.Meditation();
            var seen = new List<string>();
            for (int i = 0; i < 6; i++)
            {
                Recommendation r = w.Deliver();
                if (r != null) seen.Add(r.MessageId);
                w.Clock.AdvanceHours(9);
                w.Pomodoro();
            }
            Assert.Greater(seen.Count, 2);
            for (int i = 1; i < seen.Count; i++) Assert.AreNotEqual(seen[i - 1], seen[i], "same letter twice in a row");
            Assert.Greater(new HashSet<string>(seen).Count, 2, "should rotate through different letters");
        }

        [Test]
        public void B1_AWalkLoggedByTheGame_SilencesTheUntriedWalkRule()
        {
            var w = new World();
            var untriedWalk = new UntriedActivityRule(ActivityType.ForestWalk, "x");
            w.Meditation();
            Assert.Greater(Strength(untriedWalk, w), 0f);
            w.Walk(); // SceneVisitLogger records exactly this type
            Assert.AreEqual(0f, Strength(untriedWalk, w));
        }

        [Test]
        public void B1_TheatreVisit_SilencesTheUntriedTheatreRule()
        {
            var w = new World();
            var rule = new UntriedActivityRule(ActivityType.Theatre, "x");
            w.Pomodoro();
            Assert.Greater(Strength(rule, w), 0f);
            w.Theatre();
            Assert.AreEqual(0f, Strength(rule, w));
        }

        [Test]
        public void B2_AHappyJournalEntry_IsNotTreatedAsLowMood()
        {
            // 1.0 read "score", which journal logs always wrote as 0, so every entry looked sad.
            var w = new World();
            ActivityRecord entry = w.Journal(Mood.Happy);
            Assert.AreEqual(0, entry.score);
            Assert.AreEqual(0f, Strength(new LowMoodRule(), w));
        }

        [Test]
        public void B2_ProductiveMood_WhichIsIndexZero_IsNotLow()
        {
            var w = new World();
            w.Journal(Mood.Productive);
            Assert.AreEqual(0f, Strength(new LowMoodRule(), w));
        }

        [Test]
        public void B2_SadOrTired_IsLow_ForOneDay()
        {
            var w = new World();
            w.Journal(Mood.Sad);
            Assert.AreEqual(1f, Strength(new LowMoodRule(), w));
            w.Clock.AdvanceHours(25);
            Assert.AreEqual(0f, Strength(new LowMoodRule(), w));

            w.Journal(Mood.Tired);
            Assert.AreEqual(1f, Strength(new LowMoodRule(), w));
        }

        [Test]
        public void B2_ALaterHappyEntry_ClearsLowMood()
        {
            var w = new World();
            w.Journal(Mood.Sad);
            w.Clock.AdvanceHours(2);
            w.Journal(Mood.Happy);
            Assert.AreEqual(0f, Strength(new LowMoodRule(), w));
        }

        // ---------- session rules ----------

        [Test]
        public void BreakAfterPomodoros_FiresOnTheThirdFinishedSession()
        {
            var w = new World();
            var rule = new BreakAfterPomodorosRule();
            w.Pomodoro(); w.Pomodoro();
            Assert.AreEqual(0f, Strength(rule, w));
            w.Pomodoro(5, false); // interrupted ones do not count
            Assert.AreEqual(0f, Strength(rule, w));
            w.Pomodoro();
            Assert.AreEqual(1f, Strength(rule, w));
            StringAssert.Contains("3", rule.Format(rule.Messages[0].Text, w.Context()));
        }

        [Test]
        public void BreakAfterPomodoros_DoesNotCountYesterday()
        {
            var w = new World();
            w.Pomodoro(); w.Pomodoro();
            w.Clock.AdvanceDays(1);
            w.Pomodoro();
            Assert.AreEqual(0f, Strength(new BreakAfterPomodorosRule(), w));
        }

        [Test]
        public void BreakAfterPomodoros_OnlyRightAfterAPomodoro()
        {
            var w = new World();
            w.Pomodoro(); w.Pomodoro(); w.Pomodoro();
            w.Theatre();
            Assert.AreEqual(0f, Strength(new BreakAfterPomodorosRule(), w));
        }

        [Test]
        public void LongFocus_NeedsNinetyMinutesAndNoRestToday()
        {
            var w = new World();
            var rule = new LongFocusRule();
            w.Pomodoro(50);
            Assert.AreEqual(0f, Strength(rule, w));
            w.Pomodoro(45);
            Assert.AreEqual(1f, Strength(rule, w));
            w.Walk();
            Assert.AreEqual(0f, Strength(rule, w));
        }

        [Test]
        public void BalanceAfterBrainGame_FiresForAnHour()
        {
            var w = new World();
            var rule = new BalanceAfterBrainGameRule();
            w.Math();
            Assert.Greater(Strength(rule, w), 0f);
            w.Clock.AdvanceMinutes(61);
            Assert.AreEqual(0f, Strength(rule, w));
            w.Cards();
            Assert.Greater(Strength(rule, w), 0f);
        }

        [Test]
        public void InterruptedTimer_IsGentle_AndIgnoresMisclicks()
        {
            var w = new World();
            var rule = new InterruptedTimerRule();
            w.Pomodoro(0.2f, false);
            Assert.AreEqual(0f, Strength(rule, w));
            w.Meditation(4, false);
            Assert.AreEqual(1f, Strength(rule, w));
            w.Meditation(5, true);
            Assert.AreEqual(0f, Strength(rule, w));
        }

        // ---------- discovery ----------

        [Test]
        public void Untried_IsSilentOnAnEmptyLog()
        {
            var w = new World();
            Assert.AreEqual(0f, Strength(new UntriedActivityRule(ActivityType.Theatre, "x"), w));
            Assert.IsNull(w.Evaluate());
        }

        [Test]
        public void Lapsed_StartsAfterThreeDays_AndGrows()
        {
            var w = new World();
            var rule = new LapsedActivityRule(ActivityType.Meditation, "x");
            Assert.AreEqual(0f, Strength(rule, w)); // never done is not "lapsed"
            w.Meditation();
            w.Clock.AdvanceDays(2);
            Assert.AreEqual(0f, Strength(rule, w));
            w.Clock.AdvanceDays(1);
            float atThree = Strength(rule, w);
            w.Clock.AdvanceDays(10);
            float later = Strength(rule, w);
            Assert.Greater(atThree, 0f);
            Assert.Greater(later, atThree);
            Assert.AreEqual(1f, later);
        }

        [Test]
        public void ChapterNear_FiresCloseToTheNextChapter_ButNotWhenReady()
        {
            var w = new World();
            var rule = new ChapterNearRule();
            w.Pomodoro(); w.Meditation(); w.Walk(); w.Theatre();   // 4/4 activities, 1/3 days
            Assert.AreEqual(0f, Strength(rule, w));                 // progress 0.67
            w.Clock.AdvanceDays(1); w.Pomodoro();                   // 2/3 days -> 0.83
            Assert.Greater(Strength(rule, w), 0f);
            w.Clock.AdvanceDays(1); w.Pomodoro();                   // ready to unlock
            Assert.AreEqual(0f, Strength(rule, w));
        }

        // ---------- wellbeing ----------

        [Test]
        public void PersistentLowMood_NeedsThreeLowDaysInFive_AndPointsNowhere()
        {
            var w = new World();
            var rule = new PersistentLowMoodRule();
            w.Journal(Mood.Sad); w.Clock.AdvanceDays(1);
            w.Journal(Mood.Tired); w.Clock.AdvanceDays(1);
            Assert.AreEqual(0f, Strength(rule, w));
            w.Journal(Mood.Sad);
            Assert.AreEqual(1f, Strength(rule, w));
            Assert.AreEqual(ActivityType.Unknown, rule.Target);

            Recommendation r = w.Evaluate();
            Assert.AreEqual("mood.persistent", r.RuleId, "the most caring letter should win");
        }

        [Test]
        public void LateNight_OnlyWhenStillActive()
        {
            var w = new World(new FakeClock(2026, 11, 16, 23, 30));
            var rule = new LateNightRule();
            w.Pomodoro();
            Assert.AreEqual(1f, Strength(rule, w));
            w.Clock.AdvanceMinutes(45);
            Assert.AreEqual(0f, Strength(rule, w));

            var day = new World(new FakeClock(2026, 11, 16, 15, 0));
            day.Pomodoro();
            Assert.AreEqual(0f, Strength(rule, day));
        }

        [Test]
        public void WelcomeBack_AfterAGap_OnTheFirstActivity()
        {
            var w = new World();
            var rule = new WelcomeBackRule();
            w.Pomodoro();
            w.Clock.AdvanceDays(1); w.Pomodoro();
            Assert.AreEqual(0f, Strength(rule, w));
            w.Clock.AdvanceDays(4); w.Walk();
            Assert.AreEqual(1f, Strength(rule, w));
            w.Walk(); // second activity of the day: no longer "just back"
            Assert.AreEqual(0f, Strength(rule, w));
        }

        [Test]
        public void EveningJournal_OnlyInTheEvening_WhenNothingWrittenToday()
        {
            var w = new World(new FakeClock(2026, 11, 16, 20, 0));
            var rule = new EveningJournalRule();
            Assert.AreEqual(0f, Strength(rule, w)); // nothing done today
            w.Pomodoro();
            Assert.AreEqual(1f, Strength(rule, w));
            w.Journal(Mood.Happy);
            Assert.AreEqual(0f, Strength(rule, w));

            var noon = new World(new FakeClock(2026, 11, 16, 12, 0));
            noon.Pomodoro();
            Assert.AreEqual(0f, Strength(rule, noon));
        }

        // ---------- the set as a whole ----------

        [Test]
        public void DefaultRules_HaveUniqueIdsAndMessageIds_AndAtLeastTwelveKinds()
        {
            List<RecommendationRule> rules = DefaultRules.Create();
            var ids = new HashSet<string>();
            var messageIds = new HashSet<string>();
            var kinds = new HashSet<System.Type>();
            foreach (RecommendationRule rule in rules)
            {
                Assert.IsTrue(ids.Add(rule.Id), "duplicate rule id " + rule.Id);
                Assert.Greater(rule.Messages.Count, 0, rule.Id);
                Assert.Greater(rule.Weight, 0f, rule.Id);
                foreach (RecommendationMessage m in rule.Messages)
                {
                    Assert.IsTrue(messageIds.Add(m.Id), "duplicate message id " + m.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(m.Text), m.Id);
                }
                kinds.Add(rule.GetType());
            }
            Assert.GreaterOrEqual(kinds.Count, 12, "PRD R5 asks for at least 12 rules");
        }

        [Test]
        public void DefaultRules_NeverBlameThePlayer()
        {
            string[] banned = { "you should", "you failed", "you missed", "you haven't been", "lazy", "streak" };
            foreach (RecommendationRule rule in DefaultRules.Create())
                foreach (RecommendationMessage m in rule.Messages)
                    foreach (string phrase in banned)
                        StringAssert.DoesNotContain(phrase, m.Text.ToLowerInvariant(), m.Id);
        }

        [Test]
        public void AFullDay_NeverExceedsThreeLetters_AndNeverRepeats()
        {
            var w = new World(new FakeClock(2026, 11, 16, 8, 0));
            var delivered = new List<Recommendation>();
            for (int i = 0; i < 14; i++)
            {
                if (i % 3 == 0) w.Pomodoro(); else if (i % 3 == 1) w.Math(); else w.Journal(Mood.Tired);
                Recommendation r = w.Deliver();
                if (r != null) delivered.Add(r);
                w.Clock.AdvanceMinutes(55);
            }
            Assert.LessOrEqual(delivered.Count, RecommendationEngine.MaxPerDay);
            for (int i = 1; i < delivered.Count; i++) Assert.AreNotEqual(delivered[i - 1].MessageId, delivered[i].MessageId);
        }
    }
}
