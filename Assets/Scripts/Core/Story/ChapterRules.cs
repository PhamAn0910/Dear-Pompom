using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    public sealed class ChapterRequirement
    {
        public string Id;
        public int Current;
        public int Target;
        public bool Met => Current >= Target;
        public float Fraction => Target <= 0 ? 1f : Math.Min(1f, (float)Current / Target);
    }

    public sealed class ChapterStatus
    {
        public int Current;
        /// <summary>0 when the player is already in the last chapter.</summary>
        public int Next;
        public readonly List<ChapterRequirement> Requirements = new List<ChapterRequirement>();

        public bool CanUnlock
        {
            get
            {
                if (Next == 0) return false;
                for (int i = 0; i < Requirements.Count; i++)
                    if (!Requirements[i].Met) return false;
                return true;
            }
        }

        /// <summary>Average of the requirement fractions, 0..1. 1 when there is no next chapter.</summary>
        public float Progress
        {
            get
            {
                if (Next == 0 || Requirements.Count == 0) return 1f;
                float sum = 0f;
                for (int i = 0; i < Requirements.Count; i++) sum += Requirements[i].Fraction;
                return sum / Requirements.Count;
            }
        }
    }

    /// <summary>
    /// Unlock conditions for the four chapters (PRD 4.3). Conditions count "days with
    /// activity", never consecutive days: skipping a day must not cost progress.
    /// </summary>
    public static class ChapterRules
    {
        public const int LastChapter = 4;

        public const string ActiveDays = "activeDays";
        public const string ActivitiesTried = "activitiesTried";
        public const string LettersWritten = "lettersWritten";
        public const string WeeklyGoals = "weeklyGoalsCompleted";

        public static ChapterStatus Evaluate(ProgressStats stats, int currentChapter, int weeklyGoalsCompleted = 0)
        {
            if (currentChapter < 1) currentChapter = 1;
            if (currentChapter > LastChapter) currentChapter = LastChapter;

            var status = new ChapterStatus { Current = currentChapter, Next = currentChapter >= LastChapter ? 0 : currentChapter + 1 };
            int activeDays = stats.TotalActiveDays();

            switch (status.Next)
            {
                case 2:
                    Add(status, ActiveDays, activeDays, 3);
                    Add(status, ActivitiesTried, stats.DistinctTypesTried(), 4);
                    break;
                case 3:
                    Add(status, ActiveDays, activeDays, 7);
                    Add(status, LettersWritten, stats.JournalEntriesWritten(), 10);
                    break;
                case 4:
                    Add(status, ActiveDays, activeDays, 14);
                    Add(status, WeeklyGoals, weeklyGoalsCompleted, 1);
                    break;
            }
            return status;
        }

        private static void Add(ChapterStatus status, string id, int current, int target)
        {
            status.Requirements.Add(new ChapterRequirement { Id = id, Current = current, Target = target });
        }
    }
}
