using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>
    /// The starter rule set (PRD R5). Order only matters for exact ties.
    /// To tune: change Weight / CooldownHours here. To add a rule: write a class in Rules/,
    /// add it here, and add a test in RecommendationRuleTests.
    /// </summary>
    public static class DefaultRules
    {
        public static List<RecommendationRule> Create()
        {
            return new List<RecommendationRule>
            {
                // wellbeing first: these should win ties
                new PersistentLowMoodRule(),
                new LowMoodRule(),
                new LateNightRule(),
                new WelcomeBackRule(),

                // session flow
                new BreakAfterPomodorosRule(),
                new LongFocusRule(),
                new BalanceAfterBrainGameRule(),
                new InterruptedTimerRule(),
                new PomodoroDoneRule(),

                // rhythm and story
                new EveningJournalRule(),
                new ChapterNearRule(),

                // activities lapsed
                new LapsedActivityRule(ActivityType.Meditation, "It's been a little while since we sat in the forest. A few quiet minutes?"),
                new LapsedActivityRule(ActivityType.ForestWalk, "The forest path has new leaves on it. Walk with me?"),
                new LapsedActivityRule(ActivityType.Journal, "I haven't had a letter from you in a few days. No rush. I'm happy to read whenever you write."),
                new LapsedActivityRule(ActivityType.Theatre, "The cat has been practising a lot. Come listen for a bit?"),

                // activities never tried
                new UntriedActivityRule(ActivityType.Meditation, "There's a quiet spot in the forest where you can just sit and breathe. Want to see it?"),
                new UntriedActivityRule(ActivityType.ForestWalk, "Have you walked the forest path yet? It's a gentle one."),
                new UntriedActivityRule(ActivityType.Journal, "You can write to me any time, from the desk at home. I read every letter."),
                new UntriedActivityRule(ActivityType.Theatre, "The theatre has two musicians rehearsing. They'd love an audience."),
                new UntriedActivityRule(ActivityType.Pomodoro, "If you have something to study, the desk at home has a timer. I'll keep you company."),
                new UntriedActivityRule(ActivityType.MemoryCard, "The librarian has a card game at the school. It's a nice little puzzle."),
                new UntriedActivityRule(ActivityType.MathGame, "The school has quick number rounds, if you feel like a small challenge."),
            };
        }
    }
}
