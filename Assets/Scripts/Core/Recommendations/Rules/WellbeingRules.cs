using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>The latest journal mood (within a day) was Tired or Sad. Reads the mood field, not the score (bug B2).</summary>
    public sealed class LowMoodRule : RecommendationRule
    {
        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("mood.low.1", "Thank you for telling me how today felt. The theatre is playing softly tonight, if you want company."),
            new RecommendationMessage("mood.low.2", "Heavy days happen. No need to do much. Some music might sit well with you."),
        };

        public LowMoodRule() { Weight = 2.5f; CooldownHours = 12f; }

        public override string Id { get { return "mood.low"; } }
        public override string Group { get { return "mood"; } }
        public override ActivityType Target { get { return ActivityType.Theatre; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            ActivityRecord mood = c.Stats.LastMoodRecord();
            if (mood == null || !Moods.IsLow(mood.mood)) return 0f;
            return (c.Now - mood.Time).TotalHours <= 24 ? 1f : 0f;
        }
    }

    /// <summary>
    /// Low mood on several recent days. Pompom is a friend, not a therapist (PRD 3.3, 4.4):
    /// the letter only suggests rest and talking to someone close. No diagnosis, no target activity.
    /// </summary>
    public sealed class PersistentLowMoodRule : RecommendationRule
    {
        public int WindowDays = 5;
        public int LowDays = 3;

        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("mood.persistent.1", "These last few days sound hard. Please rest when you can. Is there someone close to you that you could talk to? I'll be here too."),
        };

        public PersistentLowMoodRule() { Weight = 4f; CooldownHours = 48f; }

        public override string Id { get { return "mood.persistent"; } }
        public override string Group { get { return "mood"; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            return c.Stats.LowMoodDays(WindowDays) >= LowDays ? 1f : 0f;
        }
    }

    /// <summary>The player is still active very late.</summary>
    public sealed class LateNightRule : RecommendationRule
    {
        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("night.late.1", "It's late in the village. The work will still be here tomorrow. Sleep well."),
            new RecommendationMessage("night.late.2", "The lamps are going out one by one. Time to rest?"),
        };

        public LateNightRule() { Weight = 2.2f; CooldownHours = 20f; }

        public override string Id { get { return "night.late"; } }
        public override string Group { get { return "rest"; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            int hour = c.Now.Hour;
            bool late = hour >= 23 || hour < 5;
            return late && c.MinutesSinceLast <= 30 ? 1f : 0f;
        }
    }

    /// <summary>First activity after several days away. Never mentions the absence as a fault.</summary>
    public sealed class WelcomeBackRule : RecommendationRule
    {
        public int GapDays = 3;

        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("welcome.back.1", "You're here. I kept your letters safe. It's good to see you."),
            new RecommendationMessage("welcome.back.2", "Welcome back. Nothing was lost while you were away."),
        };

        public WelcomeBackRule() { Weight = 2.8f; CooldownHours = 72f; }

        public override string Id { get { return "welcome.back"; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            if (c.LastRecord == null || c.PreviousRecord == null) return 0f;
            if (c.MinutesSinceLast > 60) return 0f;
            int gap = (c.LastRecord.LocalDate - c.PreviousRecord.LocalDate).Days;
            return gap >= GapDays ? 1f : 0f;
        }
    }

    /// <summary>Evening, something was done today, nothing written yet.</summary>
    public sealed class EveningJournalRule : RecommendationRule
    {
        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("journal.evening.1", "The day is winding down. Would you write me a few lines about it?"),
            new RecommendationMessage("journal.evening.2", "Before the day ends: one small thing you noticed today? I'd like to read it."),
        };

        public EveningJournalRule() { Weight = 1.3f; CooldownHours = 20f; }

        public override string Id { get { return "journal.evening"; } }
        public override ActivityType Target { get { return ActivityType.Journal; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            int hour = c.Now.Hour;
            if (hour < 19 || hour >= 23) return 0f;
            if (c.LastRecord == null || c.LastRecord.LocalDate != c.Stats.Today) return 0f;
            return c.Stats.CountOn(c.Stats.Today, ActivityType.Journal) == 0 ? 1f : 0f;
        }
    }
}
