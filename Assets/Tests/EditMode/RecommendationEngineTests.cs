using System.Collections.Generic;
using NUnit.Framework;

namespace DearPompom.Core.Tests
{
    /// <summary>Engine mechanics, tested with tiny fake rules so no real rule logic is involved.</summary>
    public class RecommendationEngineTests
    {
        private sealed class FixedRule : RecommendationRule
        {
            private readonly string _id;
            private readonly string _group;
            private readonly RecommendationMessage[] _messages;
            public float Value = 1f;

            public FixedRule(string id, float weight, int messages = 2, string group = null, float cooldown = 24f)
            {
                _id = id;
                _group = group ?? id;
                Weight = weight;
                CooldownHours = cooldown;
                _messages = new RecommendationMessage[messages];
                for (int i = 0; i < messages; i++) _messages[i] = new RecommendationMessage(id + "." + i, id + " text " + i);
            }

            public override string Id { get { return _id; } }
            public override string Group { get { return _group; } }
            public override IReadOnlyList<RecommendationMessage> Messages { get { return _messages; } }
            public override float Strength(RuleContext context) { return Value; }
        }

        private static World WorldWith(params RecommendationRule[] rules)
        {
            return new World(null, new List<RecommendationRule>(rules));
        }

        [Test]
        public void HighestWeightTimesStrengthWins()
        {
            var strongButWeak = new FixedRule("a", 5f) { Value = 0.1f }; // 0.5
            var steady = new FixedRule("b", 1f) { Value = 0.9f };        // 0.9
            World w = WorldWith(strongButWeak, steady);
            Assert.AreEqual("b", w.Evaluate().RuleId);
        }

        [Test]
        public void ZeroStrength_NeverWins_AndNothingMeansNull()
        {
            World w = WorldWith(new FixedRule("a", 10f) { Value = 0f });
            Assert.IsNull(w.Evaluate());
        }

        [Test]
        public void Ties_GoToTheEarlierRule()
        {
            World w = WorldWith(new FixedRule("first", 1f), new FixedRule("second", 1f));
            Assert.AreEqual("first", w.Evaluate().RuleId);
        }

        [Test]
        public void Evaluate_HasNoSideEffects()
        {
            World w = WorldWith(new FixedRule("a", 1f));
            w.Evaluate(); w.Evaluate();
            Assert.AreEqual(0, w.Engine.ShownToday());
            Assert.AreEqual("a.0", w.Evaluate().MessageId);
        }

        [Test]
        public void Cooldown_BlocksTheRuleUntilItExpires()
        {
            World w = WorldWith(new FixedRule("a", 1f, 2, null, 24f));
            Assert.IsNotNull(w.Deliver());
            w.Clock.AdvanceHours(23);
            Assert.IsNull(w.Evaluate());
            w.Clock.AdvanceHours(2);
            Assert.IsNotNull(w.Evaluate());
        }

        [Test]
        public void SameMessage_IsNeverSentTwiceInARow()
        {
            // No cooldown, so only the no-repeat rule protects us.
            World w = WorldWith(new FixedRule("a", 1f, 2, null, 0f));
            string first = w.Deliver().MessageId;
            string second = w.Deliver().MessageId;
            string third = w.Deliver().MessageId;
            Assert.AreNotEqual(first, second);
            Assert.AreNotEqual(second, third);
        }

        [Test]
        public void RuleWithOneMessage_CannotRepeatItself()
        {
            World w = WorldWith(new FixedRule("a", 1f, 1, null, 0f));
            Assert.IsNotNull(w.Deliver());
            Assert.IsNull(w.Evaluate());
        }

        [Test]
        public void AtMostThreeLettersPerDay()
        {
            World w = WorldWith(
                new FixedRule("a", 1f, 2, null, 0f), new FixedRule("b", 1f, 2, null, 0f),
                new FixedRule("c", 1f, 2, null, 0f), new FixedRule("d", 1f, 2, null, 0f));
            Assert.IsNotNull(w.Deliver());
            Assert.IsNotNull(w.Deliver());
            Assert.IsNotNull(w.Deliver());
            Assert.IsNull(w.Deliver());
            Assert.AreEqual(3, w.Engine.ShownToday());

            w.Clock.AdvanceDays(1);
            Assert.IsNotNull(w.Deliver());
        }

        [Test]
        public void SameGroup_IsPenalisedRightAfterItWasShown()
        {
            var g1 = new FixedRule("g1", 1f, 2, "group", 0f);
            var g2 = new FixedRule("g2", 1f, 2, "group", 0f);
            var other = new FixedRule("other", 0.9f, 2, null, 0f);
            World w = WorldWith(g1, g2, other);
            Assert.AreEqual("g1", w.Deliver().RuleId);
            // g1/g2 now score 0.6, so the slightly weaker "other" wins.
            Assert.AreEqual("other", w.Deliver().RuleId);
        }

        [Test]
        public void ThreeDismissalsInARow_HalveTheRule_AndAcceptingRestoresIt()
        {
            var nagging = new FixedRule("nag", 1f, 2, null, 0f);
            var quiet = new FixedRule("quiet", 0.7f, 2, null, 0f);
            World w = WorldWith(nagging, quiet);

            Assert.AreEqual("nag", w.Evaluate().RuleId);
            w.Engine.MarkDismissed("nag"); w.Engine.MarkDismissed("nag");
            Assert.AreEqual("nag", w.Evaluate().RuleId);   // two is not enough
            w.Engine.MarkDismissed("nag");
            Assert.AreEqual("quiet", w.Evaluate().RuleId); // 0.5 < 0.7
            w.Engine.MarkAccepted("nag");
            Assert.AreEqual("nag", w.Evaluate().RuleId);
        }

        [Test]
        public void State_SurvivesANewEngineInstance()
        {
            var rule = new FixedRule("a", 1f, 2, null, 24f);
            World w = WorldWith(rule);
            w.Deliver();
            // Same state object = what a reload from the save gives us.
            var reloaded = new RecommendationEngine(new List<RecommendationRule> { rule }, w.Save.recommendations, w.Clock);
            Assert.IsNull(reloaded.Evaluate(w.Context()));
            Assert.AreEqual(1, reloaded.ShownToday());
        }

        [Test]
        public void OldShownTimestamps_ArePruned()
        {
            World w = WorldWith(new FixedRule("a", 1f, 2, null, 0f));
            w.Deliver();
            w.Clock.AdvanceDays(5);
            w.Deliver();
            Assert.AreEqual(1, w.Save.recommendations.shownAt.Count);
        }

        [Test]
        public void StrengthAboveOne_IsCapped()
        {
            var loud = new FixedRule("loud", 1f) { Value = 50f };
            var real = new FixedRule("real", 2f) { Value = 1f };
            World w = WorldWith(loud, real);
            Assert.AreEqual("real", w.Evaluate().RuleId);
        }
    }
}
