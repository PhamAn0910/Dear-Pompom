using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>
    /// The single write path for activity records (PRD D4). Stamps id, time and chapter,
    /// sanitises input, appends to the save and raises <see cref="Recorded"/>.
    /// </summary>
    public sealed class ActivityLog
    {
        private readonly SaveData _save;
        private readonly IClock _clock;

        public ActivityLog(SaveData save, IClock clock)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (_save.records == null) _save.records = new List<ActivityRecord>();
        }

        /// <summary>Raised after a record has been appended.</summary>
        public event Action<ActivityRecord> Recorded;

        public IReadOnlyList<ActivityRecord> Records => _save.records;

        public ActivityRecord Add(
            ActivityType type,
            float durationMinutes,
            bool completed = true,
            float targetMinutes = 0f,
            int score = 0,
            string difficulty = "",
            Mood mood = Mood.None,
            ActivityKind kind = ActivityKind.None,
            string notes = "")
        {
            if (type == ActivityType.Unknown)
                throw new ArgumentException("ActivityType.Unknown cannot be recorded.", nameof(type));

            var record = new ActivityRecord
            {
                id = Guid.NewGuid().ToString("N"),
                type = type,
                kind = kind,
                timestamp = Timestamp.Format(_clock.Now),
                durationMinutes = Sanitise(durationMinutes),
                targetMinutes = Sanitise(targetMinutes),
                completed = completed,
                score = score,
                difficulty = difficulty ?? "",
                // Mood only means something on journal records.
                mood = type == ActivityType.Journal ? mood : Mood.None,
                chapter = _save.chapter < 1 ? 1 : _save.chapter,
                notes = notes ?? "",
            };

            _save.records.Add(record);
            Recorded?.Invoke(record);
            return record;
        }

        private static float Sanitise(float minutes)
        {
            if (float.IsNaN(minutes) || float.IsInfinity(minutes) || minutes < 0f) return 0f;
            return minutes;
        }
    }
}
