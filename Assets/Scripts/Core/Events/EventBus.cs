using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>
    /// Minimal typed publish/subscribe (PRD 8.2 "events instead of cross-calls").
    /// A subscriber that throws does not stop the others; the error goes to <see cref="OnHandlerError"/>.
    /// Not thread-safe: use from the main thread only.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

        /// <summary>Set by the host (Unity layer) to log handler exceptions.</summary>
        public Action<Exception> OnHandlerError;

        public void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            List<Delegate> list;
            if (!_handlers.TryGetValue(typeof(T), out list))
            {
                list = new List<Delegate>();
                _handlers[typeof(T)] = list;
            }
            if (!list.Contains(handler)) list.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            List<Delegate> list;
            if (handler != null && _handlers.TryGetValue(typeof(T), out list)) list.Remove(handler);
        }

        public void Publish<T>(T message)
        {
            List<Delegate> list;
            if (!_handlers.TryGetValue(typeof(T), out list) || list.Count == 0) return;
            // Copy so handlers may subscribe/unsubscribe while we iterate.
            Delegate[] snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                try
                {
                    ((Action<T>)snapshot[i])(message);
                }
                catch (Exception e)
                {
                    if (OnHandlerError != null) OnHandlerError(e);
                }
            }
        }
    }

    /// <summary>An activity was appended to the log.</summary>
    public sealed class ActivityRecorded
    {
        public readonly ActivityRecord Record;
        public ActivityRecorded(ActivityRecord record) { Record = record; }
    }

    /// <summary>The recommendation engine picked a letter to show.</summary>
    public sealed class RecommendationReady
    {
        public readonly Recommendation Recommendation;
        public RecommendationReady(Recommendation recommendation) { Recommendation = recommendation; }
    }

    /// <summary>The story moved to a new chapter.</summary>
    public sealed class ChapterUnlocked
    {
        public readonly int Chapter;
        public ChapterUnlocked(int chapter) { Chapter = chapter; }
    }

    /// <summary>Player settings changed (volumes, reduce motion, language).</summary>
    public sealed class SettingsChanged
    {
        public readonly SettingsData Settings;
        public SettingsChanged(SettingsData settings) { Settings = settings; }
    }
}
