namespace DearPompom.Core
{
    /// <summary>
    /// The time-keeping half of a tween: delay, duration, easing, optional ping-pong repeats.
    /// It knows nothing about what is being animated. The Unity layer (Motion/Tweener) feeds it
    /// delta time and applies <see cref="Progress"/> to a transform, colour or alpha.
    /// </summary>
    public struct TweenState
    {
        public float Duration;
        public float Delay;
        public EaseType Ease;
        /// <summary>Extra plays after the first. -1 repeats forever.</summary>
        public int Repeats;
        /// <summary>When repeating: go back and forth instead of restarting from 0.</summary>
        public bool PingPong;

        private float _elapsed;
        private int _playsDone;
        private bool _complete;

        public TweenState(float duration, EaseType ease, float delay = 0f, int repeats = 0, bool pingPong = false)
        {
            Duration = duration;
            Ease = ease;
            Delay = delay;
            Repeats = repeats;
            PingPong = pingPong;
            _elapsed = 0f;
            _playsDone = 0;
            _complete = false;
        }

        public bool IsComplete { get { return _complete; } }

        /// <summary>Eased progress. 0 before the delay ends, exactly the end value once complete.</summary>
        public float Progress
        {
            get
            {
                float linear = Duration > 0f ? (_elapsed - Delay) / Duration : 1f;
                if (_complete) linear = 1f;
                bool backwards = PingPong && (_playsDone % 2 == 1);
                float eased = Core.Ease.Evaluate(Ease, linear);
                return backwards ? 1f - eased : eased;
            }
        }

        /// <summary>
        /// Moves time forward and returns <see cref="Progress"/>. A delta longer than the whole
        /// tween lands exactly on the end value (it never overshoots in time).
        /// </summary>
        public float Advance(float deltaTime)
        {
            if (_complete) return Progress;
            if (deltaTime > 0f) _elapsed += deltaTime;
            // Finish as many plays as the elapsed time covers. The loop is bounded: a long frame
            // on an endless tween only needs the remainder, not every skipped play.
            while (_elapsed - Delay >= Duration)
            {
                bool more = Repeats < 0 || _playsDone < Repeats;
                if (!more || Duration <= 0f)
                {
                    _complete = true;
                    break;
                }
                if (Repeats < 0 && Duration > 0f)
                {
                    float over = _elapsed - Delay;
                    int whole = (int)(over / Duration);
                    _playsDone += whole;
                    _elapsed -= whole * Duration;
                    break;
                }
                _playsDone++;
                _elapsed -= Duration;
            }
            return Progress;
        }

        /// <summary>Jumps to the end value. Used by "reduce motion" and when a panel is closed mid-animation.</summary>
        public void Finish()
        {
            if (PingPong && Repeats > 0 && Repeats % 2 == 1) _playsDone = Repeats; // ends where it started
            else if (PingPong && _playsDone % 2 == 1) _playsDone++;
            _complete = true;
        }
    }
}
