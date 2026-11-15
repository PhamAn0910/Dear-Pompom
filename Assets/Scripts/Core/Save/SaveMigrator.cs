using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>Summary of a migration, for the log and for tests.</summary>
    public sealed class MigrationReport
    {
        public int Migrated;
        public int SkippedUnknownType;
        public int SkippedBadTimestamp;
        public int MoodsBackfilled;
        public readonly List<string> UnknownTypeNames = new List<string>();

        public override string ToString()
        {
            return "migrated=" + Migrated + " skippedUnknownType=" + SkippedUnknownType +
                   " skippedBadTimestamp=" + SkippedBadTimestamp + " moodsBackfilled=" + MoodsBackfilled;
        }
    }

    /// <summary>
    /// Converts older save shapes into the current <see cref="SaveData"/> (PRD D3).
    /// Pure: no file access, so it is fully covered by tests.
    /// </summary>
    public static class SaveMigrator
    {
        /// <summary>
        /// Migrates a 1.0 save. Journal text that 1.0 copied into the log's notes is dropped
        /// (it stays only in the journal file); the mood is recovered from the journal when possible.
        /// </summary>
        public static SaveData FromV1(
            LegacySaveV1 legacy,
            IList<LegacyJournalEntryV1> journal,
            IClock clock,
            out MigrationReport report)
        {
            report = new MigrationReport();
            var save = new SaveData
            {
                schemaVersion = SaveData.CurrentSchemaVersion,
                createdAt = Timestamp.Format(clock.Now),
                chapter = 1,
            };
            if (legacy == null || legacy.allActivityLogs == null) return save;

            for (int i = 0; i < legacy.allActivityLogs.Count; i++)
            {
                LegacyLogV1 old = legacy.allActivityLogs[i];
                if (old == null) continue;

                ActivityType type;
                ActivityKind kind;
                bool completed;
                if (!TryMapType(old.activityType, out type, out kind, out completed))
                {
                    report.SkippedUnknownType++;
                    if (!report.UnknownTypeNames.Contains(old.activityType ?? "")) report.UnknownTypeNames.Add(old.activityType ?? "");
                    continue;
                }

                DateTimeOffset time;
                if (!Timestamp.TryParse(old.timestamp, out time))
                {
                    report.SkippedBadTimestamp++;
                    continue;
                }

                var record = new ActivityRecord
                {
                    // Deterministic id so migrating the same file twice gives the same result.
                    id = "v1-" + i.ToString("D5"),
                    type = type,
                    kind = kind,
                    timestamp = Timestamp.Format(time),
                    durationMinutes = old.durationMinutes < 0f || float.IsNaN(old.durationMinutes) ? 0f : old.durationMinutes,
                    targetMinutes = 0f,
                    completed = completed,
                    score = old.score,
                    difficulty = old.difficulty ?? "",
                    mood = Mood.None,
                    chapter = 1,
                    notes = "",
                };

                if (type == ActivityType.Journal && kind != ActivityKind.JournalSession)
                {
                    record.mood = Moods.FromIndex(old.mood);
                    if (record.mood == Mood.None)
                    {
                        record.mood = FindJournalMood(journal, old.notes);
                        if (record.mood != Mood.None) report.MoodsBackfilled++;
                    }
                    // old.notes held the full journal text: deliberately not carried over.
                }
                else if (type != ActivityType.Journal)
                {
                    record.notes = old.notes ?? "";
                }

                save.records.Add(record);
                report.Migrated++;
            }

            return save;
        }

        /// <summary>Maps a 1.0 activity name to the 2.0 enum. Returns false for names 1.0 never wrote.</summary>
        public static bool TryMapType(string legacyName, out ActivityType type, out ActivityKind kind, out bool completed)
        {
            kind = ActivityKind.None;
            completed = true;
            switch (legacyName)
            {
                case LegacyActivityNames.Pomodoro: type = ActivityType.Pomodoro; return true;
                case LegacyActivityNames.PomodoroInterrupted: type = ActivityType.Pomodoro; completed = false; return true;
                case LegacyActivityNames.Meditation: type = ActivityType.Meditation; return true;
                case LegacyActivityNames.MeditationInterrupted: type = ActivityType.Meditation; completed = false; return true;
                case LegacyActivityNames.ForestWalk: type = ActivityType.ForestWalk; return true;
                case LegacyActivityNames.TheatreVisit: type = ActivityType.Theatre; return true;
                case LegacyActivityNames.MemoryCard: type = ActivityType.MemoryCard; return true;
                case LegacyActivityNames.MathGame: type = ActivityType.MathGame; return true;
                case LegacyActivityNames.JournalSession: type = ActivityType.Journal; kind = ActivityKind.JournalSession; return true;
                case LegacyActivityNames.JournalEntry: type = ActivityType.Journal; kind = ActivityKind.JournalEntry; return true;
                case LegacyActivityNames.JournalUpdate: type = ActivityType.Journal; kind = ActivityKind.JournalUpdate; return true;
                default: type = ActivityType.Unknown; return false;
            }
        }

        private static Mood FindJournalMood(IList<LegacyJournalEntryV1> journal, string text)
        {
            if (journal == null || string.IsNullOrEmpty(text)) return Mood.None;
            for (int i = journal.Count - 1; i >= 0; i--)
            {
                LegacyJournalEntryV1 entry = journal[i];
                if (entry != null && entry.text == text) return Moods.FromIndex(entry.mood);
            }
            return Mood.None;
        }
    }
}
