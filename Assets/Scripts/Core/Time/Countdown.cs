using System;

namespace DearPompom.Core
{
    /// <summary>
    /// A countdown that is measured against the wall clock, not accumulated from frame deltas.
    /// A Pomodoro or meditation timer built on this stays correct when the browser tab is in the
    /// background (WebGL stops sending frames there), when the window is minimised, and across a
    /// scene change (PRD F3). The timer object holds no Unity state, so a service can keep it alive.
    /// </summary>
    public sealed class Countdown
    {
        private readonly IClock _clock;
        private DateTimeOffset _endsAt;
        private TimeSpan _remainingWhenPaused;
        private TimeSpan _total;

        public Countdown(IClock clock)
        {
            if (clock == null) throw new ArgumentNullException("clock");
            _clock = clock;
        }

        public bool IsRunning { get; private set; }
        public bool IsPaused { get; private set; }
        public TimeSpan Total { get { return _total; } }

        public void Start(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero) duration = TimeSpan.Zero;
            _total = duration;
            _endsAt = _clock.Now + duration;
            IsRunning = true;
            IsPaused = false;
        }

        public void Pause()
        {
            if (!IsRunning || IsPaused) return;
            _remainingWhenPaused = Remaining;
            IsPaused = true;
        }

        public void Resume()
        {
            if (!IsRunning || !IsPaused) return;
            _endsAt = _clock.Now + _remainingWhenPaused;
            IsPaused = false;
        }

        public void Stop()
        {
            IsRunning = false;
            IsPaused = false;
        }

        /// <summary>Time left; zero when finished or not started. Never negative.</summary>
        public TimeSpan Remaining
        {
            get
            {
                if (!IsRunning) return TimeSpan.Zero;
                if (IsPaused) return _remainingWhenPaused;
                TimeSpan left = _endsAt - _clock.Now;
                return left < TimeSpan.Zero ? TimeSpan.Zero : left;
            }
        }

        /// <summary>Time actually counted so far (excludes pauses). Use this for the activity record.</summary>
        public TimeSpan Elapsed
        {
            get { return IsRunning ? _total - Remaining : TimeSpan.Zero; }
        }

        /// <summary>0 at the start, 1 when finished. For progress rings.</summary>
        public float Fraction
        {
            get
            {
                if (!IsRunning || _total <= TimeSpan.Zero) return IsRunning ? 1f : 0f;
                return (float)(Elapsed.TotalSeconds / _total.TotalSeconds);
            }
        }

        /// <summary>True when the countdown is running, not paused, and has reached zero.</summary>
        public bool IsDue
        {
            get { return IsRunning && !IsPaused && Remaining <= TimeSpan.Zero; }
        }

        /// <summary>"MM:SS", rounded up so the display never shows 00:00 while time is left.</summary>
        public string Format()
        {
            long seconds = (long)Math.Ceiling(Remaining.TotalSeconds - 1e-6);
            if (seconds < 0) seconds = 0;
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
    }
}
