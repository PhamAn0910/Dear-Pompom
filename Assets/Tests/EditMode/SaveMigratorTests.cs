using System.Collections.Generic;
using NUnit.Framework;

namespace DearPompom.Core.Tests
{
    public class SaveMigratorTests
    {
        private static LegacyLogV1 Old(string type, string time, float minutes = 0, int score = 0, string notes = "", string difficulty = "", int mood = -1)
        {
            return new LegacyLogV1 { activityType = type, timestamp = time, durationMinutes = minutes, score = score, notes = notes, difficulty = difficulty, mood = mood };
        }

        private static SaveData Migrate(LegacySaveV1 legacy, IList<LegacyJournalEntryV1> journal, out MigrationReport report)
        {
            return SaveMigrator.FromV1(legacy, journal, new FakeClock(2026, 11, 16), out report);
        }

        [Test]
        public void EveryNameThatV1Wrote_MapsToAnEnum()
        {
            // These are exactly the 11 activityType strings found in a real 1.0 save.
            string[] names =
            {
                "MemoryCard", "JournalSession", "TheatreVisit", "ForestWalk", "Pomodoro", "JournalUpdate",
                "Math Game", "JournalEntry", "MeditationInterrupted", "PomodoroInterrupted", "Meditation",
            };
            foreach (string name in names)
            {
                ActivityType type; ActivityKind kind; bool completed;
                Assert.IsTrue(SaveMigrator.TryMapType(name, out type, out kind, out completed), name);
                Assert.AreNotEqual(ActivityType.Unknown, type, name);
            }
        }

        [Test]
        public void InterruptedTypes_BecomeNotCompleted()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("PomodoroInterrupted", "2025-07-28T22:32:37.5069530+07:00", 4.5f));
            legacy.allActivityLogs.Add(Old("Pomodoro", "2025-07-28T23:00:00.0000000+07:00", 25f));
            MigrationReport report;
            SaveData save = Migrate(legacy, null, out report);

            Assert.AreEqual(2, save.records.Count);
            Assert.AreEqual(ActivityType.Pomodoro, save.records[0].type);
            Assert.IsFalse(save.records[0].completed);
            Assert.AreEqual(4.5f, save.records[0].durationMinutes);
            Assert.IsTrue(save.records[1].completed);
        }

        [Test]
        public void TheNamesBehindBugB1_AreMigrated()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("ForestWalk", "2025-08-01T10:00:00.0000000+07:00", 3f));
            legacy.allActivityLogs.Add(Old("TheatreVisit", "2025-08-01T11:00:00.0000000+07:00", 4f));
            legacy.allActivityLogs.Add(Old("Math Game", "2025-08-01T12:00:00.0000000+07:00", 2f, 70, "", "Standard"));
            MigrationReport report;
            SaveData save = Migrate(legacy, null, out report);

            Assert.AreEqual(ActivityType.ForestWalk, save.records[0].type);
            Assert.AreEqual(ActivityType.Theatre, save.records[1].type);
            Assert.AreEqual(ActivityType.MathGame, save.records[2].type);
            Assert.AreEqual(70, save.records[2].score);
            Assert.AreEqual("Standard", save.records[2].difficulty);
        }

        [Test]
        public void JournalText_IsNotCopiedIntoTheLog()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("JournalEntry", "2025-08-01T10:00:00.0000000+07:00", 0, 0, "a private thought"));
            MigrationReport report;
            SaveData save = Migrate(legacy, null, out report);

            Assert.AreEqual("", save.records[0].notes);
            Assert.AreEqual(ActivityKind.JournalEntry, save.records[0].kind);
        }

        [Test]
        public void Mood_IsBackfilledFromTheJournalFile()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("JournalEntry", "2025-08-01T10:00:00.0000000+07:00", 0, 0, "today was rough"));
            legacy.allActivityLogs.Add(Old("JournalEntry", "2025-08-02T10:00:00.0000000+07:00", 0, 0, "no match in journal"));
            var journal = new List<LegacyJournalEntryV1>
            {
                new LegacyJournalEntryV1 { id = "a", text = "today was rough", mood = 3 },
            };
            MigrationReport report;
            SaveData save = Migrate(legacy, journal, out report);

            Assert.AreEqual(Mood.Sad, save.records[0].mood);
            Assert.AreEqual(Mood.None, save.records[1].mood);
            Assert.AreEqual(1, report.MoodsBackfilled);
        }

        [Test]
        public void Mood_FromHotfixedSave_WinsOverBackfill()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("JournalEntry", "2025-08-01T10:00:00.0000000+07:00", 0, 0, "x", "", 2));
            var journal = new List<LegacyJournalEntryV1> { new LegacyJournalEntryV1 { text = "x", mood = 3 } };
            MigrationReport report;
            SaveData save = Migrate(legacy, journal, out report);
            Assert.AreEqual(Mood.Happy, save.records[0].mood);
        }

        [Test]
        public void JournalSession_NeverGetsAMood()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("JournalSession", "2025-08-01T10:00:00.0000000+07:00", 2f, 0, "Time spent in journal UI", "", 0));
            MigrationReport report;
            SaveData save = Migrate(legacy, null, out report);
            Assert.AreEqual(Mood.None, save.records[0].mood);
            Assert.AreEqual(2f, save.records[0].durationMinutes);
        }

        [Test]
        public void UnknownTypesAndBadTimestamps_AreSkippedAndReported()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("VirtualWalk", "2025-08-01T10:00:00.0000000+07:00"));
            legacy.allActivityLogs.Add(Old("Pomodoro", "garbage"));
            legacy.allActivityLogs.Add(null);
            legacy.allActivityLogs.Add(Old("Pomodoro", "2025-08-01T10:00:00.0000000+07:00", 25));
            MigrationReport report;
            SaveData save = Migrate(legacy, null, out report);

            Assert.AreEqual(1, save.records.Count);
            Assert.AreEqual(1, report.Migrated);
            Assert.AreEqual(1, report.SkippedUnknownType);
            Assert.AreEqual(1, report.SkippedBadTimestamp);
            CollectionAssert.Contains(report.UnknownTypeNames, "VirtualWalk");
        }

        [Test]
        public void EmptyOrNullLegacySave_GivesAValidEmptySave()
        {
            MigrationReport report;
            SaveData a = Migrate(null, null, out report);
            SaveData b = Migrate(new LegacySaveV1 { allActivityLogs = null }, null, out report);
            Assert.AreEqual(SaveData.CurrentSchemaVersion, a.schemaVersion);
            Assert.AreEqual(0, a.records.Count);
            Assert.AreEqual(0, b.records.Count);
        }

        [Test]
        public void Migration_IsDeterministic()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("Pomodoro", "2025-08-01T10:00:00.0000000+07:00", 25));
            MigrationReport report;
            Assert.AreEqual(Migrate(legacy, null, out report).records[0].id, Migrate(legacy, null, out report).records[0].id);
        }

        [Test]
        public void MigratedTimestamps_KeepTheOriginalDay()
        {
            var legacy = new LegacySaveV1();
            legacy.allActivityLogs.Add(Old("Pomodoro", "2025-07-28T00:15:00.0000000+07:00", 25));
            MigrationReport report;
            SaveData save = Migrate(legacy, null, out report);
            Assert.AreEqual(new System.DateTime(2025, 7, 28), save.records[0].LocalDate);
        }

        [Test]
        public void EnsureValid_RepairsNullsAndClampsSettings()
        {
            var save = new SaveData { records = null, recommendations = null, settings = null, chapter = 99 };
            save.EnsureValid();
            Assert.IsNotNull(save.records);
            Assert.IsNotNull(save.recommendations);
            Assert.IsNotNull(save.settings);
            Assert.AreEqual(4, save.chapter);

            save.settings.musicVolume = 7f;
            save.settings.sfxVolume = -1f;
            save.settings.Clamp();
            Assert.AreEqual(1f, save.settings.musicVolume);
            Assert.AreEqual(0f, save.settings.sfxVolume);
        }

        [Test]
        public void LegacyNames_RoundTripThroughTheProjection()
        {
            // The 1.0 charts still read legacy names; make sure 2.0 records map back to what they expect.
            var w = new World();
            Assert.AreEqual("Pomodoro", LegacyActivityNames.For(w.Pomodoro()));
            Assert.AreEqual("PomodoroInterrupted", LegacyActivityNames.For(w.Pomodoro(3, false)));
            Assert.AreEqual("Math Game", LegacyActivityNames.For(w.Math()));
            Assert.AreEqual("MemoryCard", LegacyActivityNames.For(w.Cards()));
            Assert.AreEqual("TheatreVisit", LegacyActivityNames.For(w.Theatre()));
            Assert.AreEqual("ForestWalk", LegacyActivityNames.For(w.Walk()));
            Assert.AreEqual("JournalEntry", LegacyActivityNames.For(w.Journal(Mood.Happy)));
        }
    }
}
