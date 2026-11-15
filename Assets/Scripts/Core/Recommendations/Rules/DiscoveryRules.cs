using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>
    /// Invites the player to an activity they have never done. One instance per activity.
    /// All instances share the "untried" group, so the engine will not send two in a row
    /// when anything else applies (this was bug B1 in 1.0: the first untried activity always won).
    /// </summary>
    public sealed class UntriedActivityRule : RecommendationRule
    {
        private readonly ActivityType _type;
        private readonly RecommendationMessage[] _texts;

        public UntriedActivityRule(ActivityType type, string text)
        {
            _type = type;
            _texts = new[] { new RecommendationMessage("untried." + type.ToString().ToLowerInvariant() + ".1", text) };
            Weight = 1f;
            CooldownHours = 48f;
        }

        public override string Id { get { return "untried." + _type.ToString().ToLowerInvariant(); } }
        public override string Group { get { return "untried"; } }
        public override ActivityType Target { get { return _type; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return _texts; } }

        public override float Strength(RuleContext c)
        {
            if (c.Stats.Count == 0) return 0f;          // nothing done yet: onboarding handles that
            return c.Stats.HasTried(_type) ? 0f : 0.5f;
        }
    }

    /// <summary>Reminds the player of an activity they used to do. Grows gently with the gap, capped at a week.</summary>
    public sealed class LapsedActivityRule : RecommendationRule
    {
        private readonly ActivityType _type;
        private readonly RecommendationMessage[] _texts;
        public int MinDays = 3;

        public LapsedActivityRule(ActivityType type, string text)
        {
            _type = type;
            _texts = new[] { new RecommendationMessage("lapsed." + type.ToString().ToLowerInvariant() + ".1", text) };
            Weight = 1.2f;
            CooldownHours = 72f;
        }

        public override string Id { get { return "lapsed." + _type.ToString().ToLowerInvariant(); } }
        public override string Group { get { return "lapsed"; } }
        public override ActivityType Target { get { return _type; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return _texts; } }

        public override float Strength(RuleContext c)
        {
            int days = c.Stats.DaysSince(_type);
            if (days < MinDays) return 0f; // also covers "never" (-1): that is UntriedActivityRule's job
            return Math.Min(1f, days / 7f);
        }
    }

    /// <summary>The next chapter is close.</summary>
    public sealed class ChapterNearRule : RecommendationRule
    {
        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("chapter.near.1", "Something in the village is about to change. I can feel it. Just a little more."),
            new RecommendationMessage("chapter.near.2", "The village has been stirring since you came. Keep going at your own pace."),
        };

        public ChapterNearRule() { Weight = 1.5f; CooldownHours = 24f; }

        public override string Id { get { return "chapter.near"; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            ChapterStatus status = c.ChapterStatus;
            if (status == null || status.Next == 0 || status.CanUnlock) return 0f;
            return status.Progress >= 0.75f ? status.Progress : 0f;
        }
    }
}
