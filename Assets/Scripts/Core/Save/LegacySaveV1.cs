using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>Shape of userProgress.json written by Dear Pompom 1.0 (no schemaVersion field).</summary>
    [Serializable]
    public class LegacySaveV1
    {
        public List<LegacyLogV1> allActivityLogs = new List<LegacyLogV1>();
    }

    [Serializable]
    public class LegacyLogV1
    {
        public string activityType = "";
        public string timestamp = "";
        public float durationMinutes;
        public int score;
        public string notes = "";
        public string difficulty = "";
        /// <summary>Only present in saves written after the B2 hotfix; -1 means "not recorded".</summary>
        public int mood = -1;
    }

    /// <summary>One entry of the 1.0 journalData.json, used to back-fill moods during migration.</summary>
    [Serializable]
    public class LegacyJournalEntryV1
    {
        public string id = "";
        public string date = "";
        public string text = "";
        public int mood = -1;
    }

    /// <summary>The hand-typed activity names 1.0 wrote. Kept in one place so nothing else spells them.</summary>
    public static class LegacyActivityNames
    {
        public const string Pomodoro = "Pomodoro";
        public const string PomodoroInterrupted = "PomodoroInterrupted";
        public const string Meditation = "Meditation";
        public const string MeditationInterrupted = "MeditationInterrupted";
        public const string ForestWalk = "ForestWalk";
        public const string TheatreVisit = "TheatreVisit";
        public const string JournalSession = "JournalSession";
        public const string JournalEntry = "JournalEntry";
        public const string JournalUpdate = "JournalUpdate";
        public const string MemoryCard = "MemoryCard";
        public const string MathGame = "Math Game";

        /// <summary>Name the 1.0 charts expect for a 2.0 record (see LegacyProjection in the Unity layer).</summary>
        public static string For(ActivityRecord record)
        {
            switch (record.type)
            {
                case ActivityType.Pomodoro: return record.completed ? Pomodoro : PomodoroInterrupted;
                case ActivityType.Meditation: return record.completed ? Meditation : MeditationInterrupted;
                case ActivityType.ForestWalk: return ForestWalk;
                case ActivityType.Theatre: return TheatreVisit;
                case ActivityType.MemoryCard: return MemoryCard;
                case ActivityType.MathGame: return MathGame;
                case ActivityType.Journal:
                    if (record.kind == ActivityKind.JournalSession) return JournalSession;
                    if (record.kind == ActivityKind.JournalUpdate) return JournalUpdate;
                    return JournalEntry;
                default: return "Unknown";
            }
        }
    }
}
