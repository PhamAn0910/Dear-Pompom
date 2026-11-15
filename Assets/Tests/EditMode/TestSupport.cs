using System;
using System.Collections.Generic;

namespace DearPompom.Core.Tests
{
    /// <summary>A clock tests can set and move.</summary>
    public sealed class FakeClock : IClock
    {
        public DateTimeOffset Now { get; set; }

        public FakeClock(int year, int month, int day, int hour = 12, int minute = 0)
        {
            Now = At(year, month, day, hour, minute);
        }

        public static DateTimeOffset At(int year, int month, int day, int hour = 12, int minute = 0)
        {
            return new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.FromHours(7));
        }

        public void Advance(TimeSpan by) { Now = Now + by; }
        public void AdvanceMinutes(double minutes) { Now = Now.AddMinutes(minutes); }
        public void AdvanceHours(double hours) { Now = Now.AddHours(hours); }
        public void AdvanceDays(double days) { Now = Now.AddDays(days); }
    }

    /// <summary>A save, a log and a clock wired together the way the game wires them.</summary>
    public sealed class World
    {
        public readonly FakeClock Clock;
        public readonly SaveData Save = new SaveData();
        public readonly ActivityLog Log;
        public readonly List<RecommendationRule> Rules;
        public readonly RecommendationEngine Engine;

        public World(FakeClock clock = null, List<RecommendationRule> rules = null)
        {
            Clock = clock ?? new FakeClock(2026, 11, 16, 12, 0); // a Monday, midday
            Log = new ActivityLog(Save, Clock);
            Rules = rules ?? DefaultRules.Create();
            Engine = new RecommendationEngine(Rules, Save.recommendations, Clock);
        }

        public ProgressStats Stats { get { return new ProgressStats(Save.records, Clock); } }

        public RuleContext Context(int weeklyGoalsCompleted = 0)
        {
            return RuleContext.Build(Stats, Save.chapter, weeklyGoalsCompleted);
        }

        public Recommendation Evaluate() { return Engine.Evaluate(Context()); }

        /// <summary>Evaluate and, if something was picked, deliver it.</summary>
        public Recommendation Deliver()
        {
            Recommendation r = Evaluate();
            if (r != null) Engine.MarkShown(r);
            return r;
        }

        public ActivityRecord Pomodoro(float minutes = 25, bool completed = true)
        {
            return Log.Add(ActivityType.Pomodoro, minutes, completed, 25);
        }

        public ActivityRecord Meditation(float minutes = 5, bool completed = true)
        {
            return Log.Add(ActivityType.Meditation, minutes, completed, 5);
        }

        public ActivityRecord Walk(float minutes = 3) { return Log.Add(ActivityType.ForestWalk, minutes); }
        public ActivityRecord Theatre(float minutes = 4) { return Log.Add(ActivityType.Theatre, minutes); }
        public ActivityRecord Math(int score = 50) { return Log.Add(ActivityType.MathGame, 2, true, 0, score, "Standard"); }
        public ActivityRecord Cards(int score = 80, string difficulty = "Easy") { return Log.Add(ActivityType.MemoryCard, 2, true, 0, score, difficulty); }

        public ActivityRecord Journal(Mood mood)
        {
            return Log.Add(ActivityType.Journal, 0, true, 0, 0, "", mood, ActivityKind.JournalEntry);
        }

        /// <summary>Does every activity once so "untried" rules stay quiet.</summary>
        public void TryEverything()
        {
            Pomodoro(); Meditation(); Walk(); Theatre(); Math(); Cards(); Journal(Mood.Happy);
        }
    }
}
