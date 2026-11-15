using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NUnit.Framework;

namespace DearPompom.Core.Tests.LocalOnly
{
    /// <summary>
    /// Migrates the real Dear Pompom 1.0 save found on this machine, if there is one.
    /// The file is read in place and never copied into the repository (it contains journal text).
    /// Skipped on machines without a 1.0 save. Not compiled by Unity: lives outside Assets/.
    /// </summary>
    public class RealSaveSmokeTests
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };

        private static string LegacyFolder()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "DefaultCompany", "My project");
        }

        private sealed class JournalWrapper { public List<LegacyJournalEntryV1> Entries = new List<LegacyJournalEntryV1>(); }

        [Test]
        public void RealV1Save_MigratesWithoutLoss()
        {
            string progressPath = Path.Combine(LegacyFolder(), "userProgress.json");
            if (!File.Exists(progressPath)) Assert.Ignore("No 1.0 save on this machine.");

            LegacySaveV1 legacy = JsonSerializer.Deserialize<LegacySaveV1>(File.ReadAllText(progressPath), Options);
            List<LegacyJournalEntryV1> journal = null;
            string journalPath = Path.Combine(LegacyFolder(), "journalData.json");
            if (File.Exists(journalPath))
                journal = JsonSerializer.Deserialize<JournalWrapper>(File.ReadAllText(journalPath), Options).Entries;

            MigrationReport report;
            SaveData save = SaveMigrator.FromV1(legacy, journal, new SystemClock(), out report);
            TestContext.Out.WriteLine("legacy logs: " + legacy.allActivityLogs.Count + " -> " + report);

            Assert.AreEqual(legacy.allActivityLogs.Count, report.Migrated, "every 1.0 log should migrate");
            Assert.AreEqual(0, report.SkippedUnknownType, string.Join(",", report.UnknownTypeNames));
            Assert.AreEqual(0, report.SkippedBadTimestamp);

            float before = 0f, after = 0f;
            foreach (LegacyLogV1 l in legacy.allActivityLogs) before += l.durationMinutes;
            foreach (ActivityRecord r in save.records)
            {
                after += r.durationMinutes;
                Assert.IsTrue(r.HasValidTime);
                if (r.type == ActivityType.Journal) Assert.AreEqual("", r.notes, "journal text must not be in the log");
            }
            Assert.AreEqual(before, after, 0.01f, "total minutes must be preserved");

            var stats = new ProgressStats(save.records, new SystemClock());
            TestContext.Out.WriteLine("active days: " + stats.TotalActiveDays() + ", activities tried: " + stats.DistinctTypesTried() +
                                      ", letters: " + stats.JournalEntriesWritten());
            Assert.Greater(stats.TotalActiveDays(), 0);
        }
    }
}
