using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>
    /// Picks at most one letter for the current situation (PRD 7.3).
    /// <list type="bullet">
    /// <item>Every rule scores itself; the highest weight x strength wins (ties: earlier rule in the list).</item>
    /// <item>A rule waits out its cooldown before firing again.</item>
    /// <item>The same message is never sent twice in a row.</item>
    /// <item>At most <see cref="MaxPerDay"/> letters per calendar day.</item>
    /// <item>A rule dismissed <see cref="DismissalsBeforePenalty"/> times in a row counts for half.</item>
    /// </list>
    /// <see cref="Evaluate"/> has no side effects; call <see cref="MarkShown"/> once the letter is actually delivered.
    /// </summary>
    public sealed class RecommendationEngine
    {
        public const int MaxPerDay = 3;
        public const int DismissalsBeforePenalty = 3;
        public const float DismissedPenalty = 0.5f;
        public const float SameGroupPenalty = 0.6f;

        private readonly IList<RecommendationRule> _rules;
        private readonly RecommendationState _state;
        private readonly IClock _clock;

        public RecommendationEngine(IList<RecommendationRule> rules, RecommendationState state, IClock clock)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _state.EnsureValid();
        }

        public RecommendationState State { get { return _state; } }

        /// <summary>Number of letters already shown on today's date.</summary>
        public int ShownToday()
        {
            DateTime today = _clock.Now.DateTime.Date;
            int n = 0;
            for (int i = 0; i < _state.shownAt.Count; i++)
            {
                DateTimeOffset t;
                if (Timestamp.TryParse(_state.shownAt[i], out t) && t.DateTime.Date == today) n++;
            }
            return n;
        }

        /// <summary>Returns the best letter for the context, or null when nothing should be sent.</summary>
        public Recommendation Evaluate(RuleContext context)
        {
            if (context == null) return null;
            if (ShownToday() >= MaxPerDay) return null;

            Recommendation best = null;
            DateTimeOffset now = _clock.Now;

            for (int i = 0; i < _rules.Count; i++)
            {
                RecommendationRule rule = _rules[i];
                if (rule == null || rule.Messages == null || rule.Messages.Count == 0) continue;

                RuleState ruleState = _state.Find(rule.Id);
                if (OnCooldown(rule, ruleState, now)) continue;

                float strength = rule.Strength(context);
                if (float.IsNaN(strength) || strength <= 0f) continue;
                if (strength > 1f) strength = 1f;

                RecommendationMessage message = PickMessage(rule, ruleState);
                if (message == null) continue; // only choice would repeat the last letter

                float score = rule.Weight * strength;
                if (ruleState != null && ruleState.dismissStreak >= DismissalsBeforePenalty) score *= DismissedPenalty;
                if (!string.IsNullOrEmpty(_state.lastGroup) && rule.Group == _state.lastGroup) score *= SameGroupPenalty;

                if (best == null || score > best.Score)
                {
                    best = new Recommendation
                    {
                        RuleId = rule.Id,
                        Group = rule.Group,
                        MessageId = message.Id,
                        Text = rule.Format(message.Text, context),
                        Target = rule.Target,
                        Score = score,
                    };
                }
            }
            return best;
        }

        /// <summary>Records that a letter was delivered: starts the cooldown and counts toward the daily cap.</summary>
        public void MarkShown(Recommendation recommendation)
        {
            if (recommendation == null) return;
            DateTimeOffset now = _clock.Now;
            RuleState ruleState = _state.GetOrCreate(recommendation.RuleId);
            ruleState.lastShownAt = Timestamp.Format(now);
            ruleState.timesShown++;
            _state.lastMessageId = recommendation.MessageId ?? "";
            _state.lastGroup = recommendation.Group ?? "";
            _state.shownAt.Add(Timestamp.Format(now));
            PruneShownAt(now);
        }

        /// <summary>The player followed the suggestion ("Go now").</summary>
        public void MarkAccepted(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId)) return;
            _state.GetOrCreate(ruleId).dismissStreak = 0;
        }

        /// <summary>The player closed the letter without following it.</summary>
        public void MarkDismissed(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId)) return;
            _state.GetOrCreate(ruleId).dismissStreak++;
        }

        private static bool OnCooldown(RecommendationRule rule, RuleState ruleState, DateTimeOffset now)
        {
            if (ruleState == null || rule.CooldownHours <= 0f) return false;
            DateTimeOffset last;
            if (!Timestamp.TryParse(ruleState.lastShownAt, out last)) return false;
            return (now - last).TotalHours < rule.CooldownHours;
        }

        /// <summary>Rotates through the rule's messages, skipping the one that was sent last.</summary>
        private RecommendationMessage PickMessage(RecommendationRule rule, RuleState ruleState)
        {
            int count = rule.Messages.Count;
            int start = ruleState == null ? 0 : ruleState.timesShown % count;
            if (start < 0) start = 0;
            for (int offset = 0; offset < count; offset++)
            {
                RecommendationMessage candidate = rule.Messages[(start + offset) % count];
                if (candidate != null && candidate.Id != _state.lastMessageId) return candidate;
            }
            return null;
        }

        private void PruneShownAt(DateTimeOffset now)
        {
            DateTime keepFrom = now.DateTime.Date.AddDays(-1);
            _state.shownAt.RemoveAll(text =>
            {
                DateTimeOffset t;
                return !Timestamp.TryParse(text, out t) || t.DateTime.Date < keepFrom;
            });
        }
    }
}
