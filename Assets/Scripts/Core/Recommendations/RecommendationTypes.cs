using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>One piece of text a rule can send. The id is stable so text can move to a localization table.</summary>
    public sealed class RecommendationMessage
    {
        public readonly string Id;
        public readonly string Text;

        public RecommendationMessage(string id, string text)
        {
            Id = id;
            Text = text;
        }
    }

    /// <summary>The engine's pick: which rule fired, which text, and where "Go now" should lead.</summary>
    public sealed class Recommendation
    {
        public string RuleId;
        public string Group;
        public string MessageId;
        public string Text;
        /// <summary>ActivityType.Unknown when the letter does not point anywhere.</summary>
        public ActivityType Target;
        public float Score;
    }

    /// <summary>Everything a rule may look at. Built fresh for each evaluation.</summary>
    public sealed class RuleContext
    {
        public ProgressStats Stats;
        public DateTimeOffset Now;
        public ActivityRecord LastRecord;
        public ActivityRecord PreviousRecord;
        public int Chapter = 1;
        public ChapterStatus ChapterStatus;

        public static RuleContext Build(ProgressStats stats, int chapter, int weeklyGoalsCompleted = 0)
        {
            return new RuleContext
            {
                Stats = stats,
                Now = stats.Now,
                LastRecord = stats.LastRecord(),
                PreviousRecord = stats.PreviousRecord(),
                Chapter = chapter,
                ChapterStatus = ChapterRules.Evaluate(stats, chapter, weeklyGoalsCompleted),
            };
        }

        /// <summary>Minutes since the latest record; a very large number when there is none.</summary>
        public double MinutesSinceLast
        {
            get { return LastRecord == null ? double.MaxValue : (Now - LastRecord.Time).TotalMinutes; }
        }
    }

    /// <summary>
    /// A recommendation rule scores itself (PRD 7.3): <see cref="Strength"/> says how well it
    /// fits right now (0 = not at all, 1 = fully), <see cref="Weight"/> says how much it matters.
    /// The engine multiplies the two and the highest product wins.
    /// </summary>
    public abstract class RecommendationRule
    {
        public abstract string Id { get; }
        /// <summary>Rules in the same group say similar things; the engine avoids the same group twice in a row.</summary>
        public virtual string Group { get { return Id; } }
        public float Weight = 1f;
        /// <summary>Hours before this rule may fire again (PRD R2, default 24).</summary>
        public float CooldownHours = 24f;
        public virtual ActivityType Target { get { return ActivityType.Unknown; } }
        public abstract IReadOnlyList<RecommendationMessage> Messages { get; }

        /// <summary>0 when the rule does not apply; otherwise up to 1.</summary>
        public abstract float Strength(RuleContext context);

        /// <summary>Fills placeholders such as {count}. Default: text unchanged.</summary>
        public virtual string Format(string text, RuleContext context) { return text; }
    }

    /// <summary>Per-rule memory, persisted in the save.</summary>
    [Serializable]
    public class RuleState
    {
        public string ruleId = "";
        public string lastShownAt = "";
        public int timesShown;
        /// <summary>Consecutive dismissals; reset when the player follows the suggestion.</summary>
        public int dismissStreak;
    }

    /// <summary>Engine memory, persisted in the save (cooldowns, daily cap, no-repeat).</summary>
    [Serializable]
    public class RecommendationState
    {
        public List<RuleState> rules = new List<RuleState>();
        public string lastMessageId = "";
        public string lastGroup = "";
        /// <summary>Timestamps of recently shown letters, for the per-day cap.</summary>
        public List<string> shownAt = new List<string>();

        public void EnsureValid()
        {
            if (rules == null) rules = new List<RuleState>();
            rules.RemoveAll(r => r == null);
            if (shownAt == null) shownAt = new List<string>();
            if (lastMessageId == null) lastMessageId = "";
            if (lastGroup == null) lastGroup = "";
        }

        public RuleState Find(string ruleId)
        {
            for (int i = 0; i < rules.Count; i++)
                if (rules[i].ruleId == ruleId) return rules[i];
            return null;
        }

        public RuleState GetOrCreate(string ruleId)
        {
            RuleState state = Find(ruleId);
            if (state == null)
            {
                state = new RuleState { ruleId = ruleId };
                rules.Add(state);
            }
            return state;
        }
    }
}
