using DearPompom.Core;

namespace DearPompom
{
    /// <summary>
    /// Glue between the activity log and the recommendation engine. Listens for
    /// <see cref="ActivityRecorded"/>, asks the engine, and publishes <see cref="RecommendationReady"/>.
    /// It does not know how a letter is shown: a presenter (today RecommendationSystem's popup,
    /// later the mailbox) listens for RecommendationReady and reports back with MarkShown/Accepted/Dismissed.
    /// </summary>
    public sealed class RecommendationService
    {
        private readonly UserProgressManager _progress;
        private readonly EventBus _events;
        private readonly RecommendationEngine _engine;

        public RecommendationService(UserProgressManager progress, EventBus events, IClock clock)
        {
            _progress = progress;
            _events = events;
            _engine = new RecommendationEngine(DefaultRules.Create(), progress.Save.recommendations, clock);
            _events.Subscribe<ActivityRecorded>(OnActivityRecorded);
        }

        /// <summary>What the engine would send right now, or null. No side effects.</summary>
        public Recommendation Peek()
        {
            return _engine.Evaluate(RuleContext.Build(_progress.Stats, _progress.Save.chapter));
        }

        private void OnActivityRecorded(ActivityRecorded message)
        {
            Recommendation recommendation = Peek();
            if (recommendation != null) _events.Publish(new RecommendationReady(recommendation));
        }

        /// <summary>Call when the letter is actually on screen: starts cooldowns and the daily count.</summary>
        public void MarkShown(Recommendation recommendation)
        {
            if (recommendation == null) return;
            _engine.MarkShown(recommendation);
            _progress.SaveProgress();
        }

        public void MarkAccepted(Recommendation recommendation)
        {
            if (recommendation == null) return;
            _engine.MarkAccepted(recommendation.RuleId);
            _progress.SaveProgress();
        }

        public void MarkDismissed(Recommendation recommendation)
        {
            if (recommendation == null) return;
            _engine.MarkDismissed(recommendation.RuleId);
            _progress.SaveProgress();
        }
    }
}
