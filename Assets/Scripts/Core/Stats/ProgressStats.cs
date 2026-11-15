using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>Minutes per activity type for one calendar day.</summary>
    public sealed class DayBucket
    {
        public DateTime Date;
        /// <summary>Indexed by (int)ActivityType.</summary>
        public readonly float[] MinutesByType = new float[ProgressStats.TypeSlots];
        public int RecordCount;

        public float TotalMinutes
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < MinutesByType.Length; i++) sum += MinutesByType[i];
                return sum;
            }
        }

        public float Minutes(ActivityType type) { return MinutesByType[(int)type]; }
    }

    /// <summary>The numbers behind the weekly summary card (PRD P1).</summary>
    public sealed class WeekSummary
    {
        public DateTime From;
        public DateTime To;
        public float FocusMinutes;
        public float TotalMinutes;
        public int ActiveDays;
        public Mood TopMood = Mood.None;
    }

    /// <summary>
    /// Read-only queries over the activity log (PRD D6). Charts, letters, chapters and
    /// recommendations all read from here so they can never disagree about the numbers.
    /// Dates are calendar days on the player's clock; ranges are inclusive on both ends.
    /// Records whose timestamp cannot be read are ignored.
    /// </summary>
    public sealed class ProgressStats
    {
        public const int TypeSlots = 8; // (int)ActivityType.Theatre + 1

        private readonly IReadOnlyList<ActivityRecord> _records;
        private readonly IClock _clock;

        public ProgressStats(IReadOnlyList<ActivityRecord> records, IClock clock)
        {
            _records = records ?? new List<ActivityRecord>();
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public DateTimeOffset Now => _clock.Now;
        public DateTime Today => _clock.Now.DateTime.Date;
        public int Count => _records.Count;

        // ---------- single records ----------

        /// <summary>Most recent record by time (not by list position), or null.</summary>
        public ActivityRecord LastRecord()
        {
            return Latest(null, false, 0);
        }

        /// <summary>Most recent record of a type, or null.</summary>
        public ActivityRecord Last(ActivityType type)
        {
            return Latest(type, false, 0);
        }

        /// <summary>The record just before the latest one, or null.</summary>
        public ActivityRecord PreviousRecord()
        {
            return Latest(null, false, 1);
        }

        /// <summary>Most recent journal entry or update that carries a mood, or null.</summary>
        public ActivityRecord LastMoodRecord()
        {
            ActivityRecord best = null;
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (!Usable(r) || r.type != ActivityType.Journal || r.mood == Mood.None) continue;
                if (best == null || r.Time >= best.Time) best = r;
            }
            return best;
        }

        private ActivityRecord Latest(ActivityType? type, bool completedOnly, int skip)
        {
            ActivityRecord first = null, second = null;
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (!Usable(r)) continue;
                if (type.HasValue && r.type != type.Value) continue;
                if (completedOnly && !r.completed) continue;
                if (first == null || r.Time >= first.Time) { second = first; first = r; }
                else if (second == null || r.Time >= second.Time) { second = r; }
            }
            return skip == 0 ? first : second;
        }

        // ---------- counts ----------

        public bool HasTried(ActivityType type)
        {
            for (int i = 0; i < _records.Count; i++)
                if (Usable(_records[i]) && _records[i].type == type) return true;
            return false;
        }

        public int DistinctTypesTried()
        {
            int n = 0;
            for (int i = 0; i < ActivityTypes.All.Length; i++)
                if (HasTried(ActivityTypes.All[i])) n++;
            return n;
        }

        /// <summary>How many records of a type fall on a given day.</summary>
        public int CountOn(DateTime day, ActivityType type, bool completedOnly = false)
        {
            DateTime d = day.Date;
            int n = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (!Usable(r) || r.type != type || r.LocalDate != d) continue;
                if (completedOnly && !r.completed) continue;
                n++;
            }
            return n;
        }

        /// <summary>Journal entries written (new entries only; sessions and edits do not count).</summary>
        public int JournalEntriesWritten()
        {
            int n = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (Usable(r) && r.type == ActivityType.Journal && r.kind == ActivityKind.JournalEntry) n++;
            }
            return n;
        }

        /// <summary>Whole days since the activity was last done; -1 if never.</summary>
        public int DaysSince(ActivityType type)
        {
            ActivityRecord last = Last(type);
            if (last == null) return -1;
            int days = (Today - last.LocalDate).Days;
            return days < 0 ? 0 : days;
        }

        // ---------- days ----------

        /// <summary>Number of distinct days with at least one record, over the whole log.</summary>
        public int TotalActiveDays()
        {
            return ActiveDays(DateTime.MinValue, DateTime.MaxValue);
        }

        public int ActiveDays(DateTime from, DateTime to)
        {
            var days = new HashSet<DateTime>();
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (Usable(r) && InRange(r, from, to)) days.Add(r.LocalDate);
            }
            return days.Count;
        }

        /// <summary>
        /// One bucket per day for the last <paramref name="days"/> days ending today,
        /// oldest first. Days without activity are present with zero minutes.
        /// </summary>
        public List<DayBucket> DailyBuckets(int days)
        {
            if (days < 1) days = 1;
            var result = new List<DayBucket>(days);
            DateTime start = Today.AddDays(-(days - 1));
            for (int i = 0; i < days; i++) result.Add(new DayBucket { Date = start.AddDays(i) });

            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (!Usable(r)) continue;
                int index = (r.LocalDate - start).Days;
                if (index < 0 || index >= days) continue;
                int slot = (int)r.type;
                if (slot <= 0 || slot >= TypeSlots) continue;
                result[index].MinutesByType[slot] += r.durationMinutes;
                result[index].RecordCount++;
            }
            return result;
        }

        // ---------- minutes ----------

        public float TotalMinutes(ActivityType type, DateTime from, DateTime to)
        {
            float sum = 0f;
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (Usable(r) && r.type == type && InRange(r, from, to)) sum += r.durationMinutes;
            }
            return sum;
        }

        public float TotalMinutes(DateTime from, DateTime to)
        {
            float sum = 0f;
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (Usable(r) && InRange(r, from, to)) sum += r.durationMinutes;
            }
            return sum;
        }

        public float MinutesToday(ActivityType type)
        {
            return TotalMinutes(type, Today, Today);
        }

        // ---------- quality ----------

        /// <summary>Share of sessions of a type that were finished, 0..1; -1 when there were none.</summary>
        public float CompletionRate(ActivityType type, DateTime from, DateTime to)
        {
            int total = 0, done = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (!Usable(r) || r.type != type || !InRange(r, from, to)) continue;
                total++;
                if (r.completed) done++;
            }
            return total == 0 ? -1f : (float)done / total;
        }

        /// <summary>Highest score for a game; pass null difficulty for "any". 0 when never played.</summary>
        public int BestScore(ActivityType type, string difficulty = null)
        {
            int best = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (!Usable(r) || r.type != type) continue;
                if (difficulty != null && r.difficulty != difficulty) continue;
                if (r.score > best) best = r.score;
            }
            return best;
        }

        // ---------- mood ----------

        /// <summary>How often each mood was picked in the range. Moods are categories, so there is no average.</summary>
        public Dictionary<Mood, int> MoodCounts(DateTime from, DateTime to)
        {
            var counts = new Dictionary<Mood, int>();
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (!Usable(r) || r.type != ActivityType.Journal || r.mood == Mood.None || !InRange(r, from, to)) continue;
                int c;
                counts.TryGetValue(r.mood, out c);
                counts[r.mood] = c + 1;
            }
            return counts;
        }

        /// <summary>Most picked mood in the range; ties go to the lower enum value. None when no mood was logged.</summary>
        public Mood TopMood(DateTime from, DateTime to)
        {
            Dictionary<Mood, int> counts = MoodCounts(from, to);
            Mood top = Mood.None;
            int best = 0;
            foreach (KeyValuePair<Mood, int> pair in counts)
            {
                if (pair.Value > best || (pair.Value == best && top != Mood.None && pair.Key < top))
                {
                    best = pair.Value;
                    top = pair.Key;
                }
            }
            return top;
        }

        /// <summary>Distinct days, within the last <paramref name="days"/> days, whose LAST logged mood was low.</summary>
        public int LowMoodDays(int days)
        {
            DateTime from = Today.AddDays(-(days - 1));
            var lastOfDay = new Dictionary<DateTime, ActivityRecord>();
            for (int i = 0; i < _records.Count; i++)
            {
                ActivityRecord r = _records[i];
                if (!Usable(r) || r.type != ActivityType.Journal || r.mood == Mood.None || !InRange(r, from, Today)) continue;
                ActivityRecord existing;
                if (!lastOfDay.TryGetValue(r.LocalDate, out existing) || r.Time >= existing.Time) lastOfDay[r.LocalDate] = r;
            }
            int low = 0;
            foreach (ActivityRecord r in lastOfDay.Values)
                if (Moods.IsLow(r.mood)) low++;
            return low;
        }

        // ---------- summaries ----------

        /// <summary>
        /// Rolling 7-day window. weeksAgo = 0 is today and the 6 days before it;
        /// weeksAgo = 1 is the 7 days before that.
        /// </summary>
        public WeekSummary Week(int weeksAgo = 0)
        {
            DateTime to = Today.AddDays(-7 * weeksAgo);
            DateTime from = to.AddDays(-6);
            return new WeekSummary
            {
                From = from,
                To = to,
                FocusMinutes = TotalMinutes(ActivityType.Pomodoro, from, to),
                TotalMinutes = TotalMinutes(from, to),
                ActiveDays = ActiveDays(from, to),
                TopMood = TopMood(from, to),
            };
        }

        // ---------- helpers ----------

        private static bool Usable(ActivityRecord r)
        {
            return r != null && r.HasValidTime;
        }

        private static bool InRange(ActivityRecord r, DateTime from, DateTime to)
        {
            DateTime d = r.LocalDate;
            return d >= from.Date && d <= to.Date;
        }
    }
}
