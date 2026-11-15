using System;

namespace DearPompom.Core
{
    /// <summary>
    /// One logged activity. Plain serializable fields only, so Unity's JsonUtility can save it.
    /// Create records through <see cref="ActivityLog.Add"/>, never by hand in gameplay code.
    /// </summary>
    [Serializable]
    public class ActivityRecord
    {
        /// <summary>Unique id of this record (sessionId in the PRD).</summary>
        public string id;
        public ActivityType type;
        public ActivityKind kind;
        /// <summary>ISO 8601 with offset; local wall-clock time when the activity ended.</summary>
        public string timestamp;
        /// <summary>Real minutes spent.</summary>
        public float durationMinutes;
        /// <summary>Minutes the player planned (timers); 0 when not applicable.</summary>
        public float targetMinutes;
        /// <summary>False for a timer that was stopped early. Replaces the 1.0 "...Interrupted" types.</summary>
        public bool completed = true;
        public int score;
        public string difficulty = "";
        public Mood mood = Mood.None;
        /// <summary>Story chapter the player was in.</summary>
        public int chapter = 1;
        /// <summary>Short machine context only. Journal text is never copied here.</summary>
        public string notes = "";

        [NonSerialized] private bool _timeParsed;
        [NonSerialized] private DateTimeOffset _time;

        /// <summary>Parsed timestamp; DateTimeOffset.MinValue when the stored text is unreadable.</summary>
        public DateTimeOffset Time
        {
            get
            {
                if (!_timeParsed)
                {
                    if (!Timestamp.TryParse(timestamp, out _time)) _time = DateTimeOffset.MinValue;
                    _timeParsed = true;
                }
                return _time;
            }
        }

        /// <summary>Calendar day (as the player saw it on their clock) the activity belongs to.</summary>
        public DateTime LocalDate => Time.DateTime.Date;

        public bool HasValidTime => Time != DateTimeOffset.MinValue;
    }
}
