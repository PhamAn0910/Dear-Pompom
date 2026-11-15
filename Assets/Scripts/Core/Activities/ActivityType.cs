namespace DearPompom.Core
{
    /// <summary>
    /// Every loggable activity. The numeric values are written to the save file,
    /// so NEVER renumber or reuse a value; only append new ones.
    /// </summary>
    public enum ActivityType
    {
        Unknown = 0,
        Pomodoro = 1,
        Meditation = 2,
        ForestWalk = 3,
        Journal = 4,
        MemoryCard = 5,
        MathGame = 6,
        Theatre = 7,
    }

    /// <summary>
    /// Sub-kind of a record, for activities that log more than one sort of event.
    /// Values are persisted: append only.
    /// </summary>
    public enum ActivityKind
    {
        None = 0,
        /// <summary>Time spent with the journal open.</summary>
        JournalSession = 1,
        /// <summary>A new letter / journal entry was written.</summary>
        JournalEntry = 2,
        /// <summary>An existing entry was edited.</summary>
        JournalUpdate = 3,
    }

    public static class ActivityTypes
    {
        /// <summary>The activities a player can choose to do (excludes Unknown).</summary>
        public static readonly ActivityType[] All =
        {
            ActivityType.Pomodoro,
            ActivityType.Meditation,
            ActivityType.ForestWalk,
            ActivityType.Journal,
            ActivityType.MemoryCard,
            ActivityType.MathGame,
            ActivityType.Theatre,
        };

        /// <summary>Scene that hosts the activity; used by "Go now" buttons.</summary>
        public static string SceneName(ActivityType type)
        {
            switch (type)
            {
                case ActivityType.Pomodoro: return "Home";
                case ActivityType.Journal: return "Home";
                case ActivityType.Meditation: return "ForestSitting";
                case ActivityType.ForestWalk: return "ForestWalking";
                case ActivityType.MemoryCard: return "SchoolCard";
                case ActivityType.MathGame: return "SchoolMath";
                case ActivityType.Theatre: return "Theatre";
                default: return "World";
            }
        }

        public static bool IsBrainGame(ActivityType type)
        {
            return type == ActivityType.MemoryCard || type == ActivityType.MathGame;
        }
    }
}
