using System.Collections.Generic;
using System.Globalization;

namespace DearPompom.Core
{
    /// <summary>After N finished Pomodoros today, suggest a real break. Fires right after a Pomodoro.</summary>
    public sealed class BreakAfterPomodorosRule : RecommendationRule
    {
        public int Threshold = 3;

        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("break.pomodoros.1", "That's {count} focus sessions today. The forest is quiet right now. Come sit for a few minutes?"),
            new RecommendationMessage("break.pomodoros.2", "{count} sessions done. Your mind has earned a rest. A short meditation would be a kind thing to do."),
        };

        public BreakAfterPomodorosRule() { Weight = 3f; CooldownHours = 4f; }

        public override string Id { get { return "break.pomodoros"; } }
        public override string Group { get { return "rest"; } }
        public override ActivityType Target { get { return ActivityType.Meditation; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            if (c.LastRecord == null || c.LastRecord.type != ActivityType.Pomodoro || !c.LastRecord.completed) return 0f;
            return Count(c) >= Threshold ? 1f : 0f;
        }

        public override string Format(string text, RuleContext c)
        {
            return text.Replace("{count}", Count(c).ToString(CultureInfo.InvariantCulture));
        }

        private static int Count(RuleContext c)
        {
            return c.Stats.CountOn(c.Stats.Today, ActivityType.Pomodoro, true);
        }
    }

    /// <summary>A lot of focus time today and no walk or meditation yet.</summary>
    public sealed class LongFocusRule : RecommendationRule
    {
        public float FocusMinutes = 90f;

        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("focus.long.1", "You've been studying for a good while today. A walk under the trees might feel nice."),
            new RecommendationMessage("focus.long.2", "Long day at the desk. Stretch your legs in the forest with me?"),
        };

        public LongFocusRule() { Weight = 2.6f; CooldownHours = 6f; }

        public override string Id { get { return "focus.long"; } }
        public override string Group { get { return "rest"; } }
        public override ActivityType Target { get { return ActivityType.ForestWalk; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            if (c.Stats.MinutesToday(ActivityType.Pomodoro) < FocusMinutes) return 0f;
            if (c.Stats.CountOn(c.Stats.Today, ActivityType.ForestWalk) > 0) return 0f;
            if (c.Stats.CountOn(c.Stats.Today, ActivityType.Meditation) > 0) return 0f;
            return 1f;
        }
    }

    /// <summary>A small acknowledgement right after a finished Pomodoro.</summary>
    public sealed class PomodoroDoneRule : RecommendationRule
    {
        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("pomodoro.done.1", "One focus session finished. I saw the lamp on the whole time. Well done."),
            new RecommendationMessage("pomodoro.done.2", "Session complete. Want to write me a line about what you worked on?"),
        };

        public PomodoroDoneRule() { Weight = 1f; CooldownHours = 6f; }

        public override string Id { get { return "pomodoro.done"; } }
        public override ActivityType Target { get { return ActivityType.Journal; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            if (c.LastRecord == null || c.LastRecord.type != ActivityType.Pomodoro || !c.LastRecord.completed) return 0f;
            return c.MinutesSinceLast <= 10 ? 0.4f : 0f;
        }
    }

    /// <summary>After a memory or math game, suggest something that rests the head.</summary>
    public sealed class BalanceAfterBrainGameRule : RecommendationRule
    {
        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("balance.braingame.1", "Good brain workout. A walk in the forest could help it settle."),
            new RecommendationMessage("balance.braingame.2", "The librarian says you did well. Let your head rest a little. The trees are waiting."),
        };

        public BalanceAfterBrainGameRule() { Weight = 2f; CooldownHours = 6f; }

        public override string Id { get { return "balance.braingame"; } }
        public override ActivityType Target { get { return ActivityType.ForestWalk; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            if (c.LastRecord == null || !ActivityTypes.IsBrainGame(c.LastRecord.type)) return 0f;
            return c.MinutesSinceLast <= 60 ? 0.8f : 0f;
        }
    }

    /// <summary>A timer was stopped early. Never a reproach.</summary>
    public sealed class InterruptedTimerRule : RecommendationRule
    {
        private static readonly RecommendationMessage[] Texts =
        {
            new RecommendationMessage("timer.interrupted.1", "Stopping early is fine. The minutes you did still count."),
            new RecommendationMessage("timer.interrupted.2", "Some days a shorter session is the right one. You still showed up."),
        };

        public InterruptedTimerRule() { Weight = 1.4f; CooldownHours = 12f; }

        public override string Id { get { return "timer.interrupted"; } }
        public override IReadOnlyList<RecommendationMessage> Messages { get { return Texts; } }

        public override float Strength(RuleContext c)
        {
            ActivityRecord last = c.LastRecord;
            if (last == null || last.completed) return 0f;
            if (last.type != ActivityType.Pomodoro && last.type != ActivityType.Meditation) return 0f;
            if (last.durationMinutes < 1f) return 0f; // a mis-click, not a session
            return c.MinutesSinceLast <= 10 ? 1f : 0f;
        }
    }
}
