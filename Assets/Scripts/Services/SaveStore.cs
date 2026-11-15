using System;
using System.Collections.Generic;
using System.IO;
using DearPompom.Core;
using UnityEngine;

namespace DearPompom
{
    /// <summary>
    /// All file access for save data (PRD D5). One folder, atomic writes, one backup per file.
    ///
    ///   persistentDataPath/save/progress.json   activity log, settings, recommendation state
    ///   persistentDataPath/save/journal.json    journal text (kept apart: it is the sensitive part)
    ///   *.bak                                   previous good copy of each file
    ///
    /// Nothing else in the game should call File.* for save data.
    /// </summary>
    public sealed class SaveStore
    {
        public const string FolderName = "save";
        public const string ProgressFile = "progress.json";
        public const string JournalFile = "journal.json";

        // 1.0 wrote these directly into persistentDataPath.
        public const string LegacyProgressFile = "userProgress.json";
        public const string LegacyJournalFile = "journalData.json";

        public string Folder { get; private set; }

        public SaveStore()
        {
            Folder = Path.Combine(Application.persistentDataPath, FolderName);
        }

        public string PathOf(string fileName) { return Path.Combine(Folder, fileName); }

        // ------------------------------------------------------------------ progress

        /// <summary>
        /// Loads the save, migrating a 1.0 file if that is all there is. Never throws:
        /// on any failure the player gets a fresh save and the broken file is left untouched.
        /// </summary>
        public SaveData LoadProgress(IClock clock, out string description)
        {
            SaveData save = TryParseCurrent(ReadText(ProgressFile));
            if (save != null)
            {
                description = "loaded " + ProgressFile + " (" + save.records.Count + " records)";
                return save;
            }

            save = TryParseCurrent(ReadTextRaw(PathOf(ProgressFile) + ".bak"));
            if (save != null)
            {
                description = "main save unreadable, restored from backup (" + save.records.Count + " records)";
                return save;
            }

            string legacyPath = FindLegacyFile(LegacyProgressFile);
            if (legacyPath != null)
            {
                try
                {
                    LegacySaveV1 legacy = JsonUtility.FromJson<LegacySaveV1>(ReadTextRaw(legacyPath));
                    MigrationReport report;
                    save = SaveMigrator.FromV1(legacy, LoadLegacyJournalEntries(), clock, out report);
                    save.EnsureValid();
                    description = "migrated 1.0 save from " + legacyPath + " (" + report + ")";
                    return save;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[DearPompom] could not migrate 1.0 save at " + legacyPath + ": " + e.Message);
                }
            }

            save = new SaveData { createdAt = Timestamp.Format(clock.Now) };
            save.EnsureValid();
            description = "no save found, started a new one";
            return save;
        }

        public bool SaveProgress(SaveData save)
        {
            if (save == null) return false;
            save.schemaVersion = SaveData.CurrentSchemaVersion;
            return WriteText(ProgressFile, JsonUtility.ToJson(save, true));
        }

        private static SaveData TryParseCurrent(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                SaveVersionProbe probe = JsonUtility.FromJson<SaveVersionProbe>(json);
                if (probe == null || probe.schemaVersion < SaveData.CurrentSchemaVersion) return null;
                if (probe.schemaVersion > SaveData.CurrentSchemaVersion)
                    Debug.LogWarning("[DearPompom] save was written by a newer version (schema " + probe.schemaVersion + "). Unknown fields will be dropped on the next save.");
                SaveData save = JsonUtility.FromJson<SaveData>(json);
                if (save == null) return null;
                save.EnsureValid();
                return save;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DearPompom] save file could not be parsed: " + e.Message);
                return null;
            }
        }

        // ------------------------------------------------------------------ legacy 1.0 files

        /// <summary>
        /// Looks for a 1.0 file. 1.0 was built as "DefaultCompany/My project", so on desktop its
        /// persistentDataPath is a sibling of ours; on WebGL the path does not depend on the product name.
        /// </summary>
        public string FindLegacyFile(string fileName)
        {
            var candidates = new List<string> { Path.Combine(Application.persistentDataPath, fileName) };
            try
            {
                DirectoryInfo product = new DirectoryInfo(Application.persistentDataPath);
                DirectoryInfo company = product.Parent;
                DirectoryInfo root = company != null ? company.Parent : null;
                if (root != null) candidates.Add(Path.Combine(root.FullName, "DefaultCompany", "My project", fileName));
            }
            catch (Exception)
            {
                // Path shape differs per platform; the first candidate is enough there.
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                try { if (File.Exists(candidates[i])) return candidates[i]; }
                catch (Exception) { }
            }
            return null;
        }

        [Serializable]
        private class LegacyJournalWrapper { public List<LegacyJournalEntryV1> Entries = new List<LegacyJournalEntryV1>(); }

        private List<LegacyJournalEntryV1> LoadLegacyJournalEntries()
        {
            // Prefer the already-imported journal, then the 1.0 file. Both have the same shape.
            string json = ReadText(JournalFile);
            if (string.IsNullOrEmpty(json))
            {
                string legacy = FindLegacyFile(LegacyJournalFile);
                if (legacy != null) json = ReadTextRaw(legacy);
            }
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                LegacyJournalWrapper wrapper = JsonUtility.FromJson<LegacyJournalWrapper>(json);
                return wrapper != null ? wrapper.Entries : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------ raw text

        /// <summary>Reads a file from the save folder, falling back to its backup. Null when neither is readable.</summary>
        public string ReadText(string fileName)
        {
            string path = PathOf(fileName);
            string text = ReadTextRaw(path);
            if (!string.IsNullOrEmpty(text)) return text;
            return ReadTextRaw(path + ".bak");
        }

        private static string ReadTextRaw(string path)
        {
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[DearPompom] could not read " + path + ": " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// Writes via a temp file, keeping the previous version as .bak, so a crash in the middle
        /// of a write can never leave the player with a half-written save.
        /// </summary>
        public bool WriteText(string fileName, string content)
        {
            string path = PathOf(fileName);
            string temp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Folder);
                File.WriteAllText(temp, content ?? "");
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                    File.Delete(path);
                }
                File.Move(temp, path);
                FlushToBrowser();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[DearPompom] could not write " + path + ": " + e.Message);
                return false;
            }
        }

        /// <summary>Deletes every file in the save folder (PRD D7 "delete all data"). Legacy 1.0 files are left alone.</summary>
        public bool DeleteAll()
        {
            try
            {
                if (Directory.Exists(Folder)) Directory.Delete(Folder, true);
                FlushToBrowser();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[DearPompom] could not delete save folder: " + e.Message);
                return false;
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void DearPompomSyncFiles();
#endif

        private static void FlushToBrowser()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try { DearPompomSyncFiles(); }
            catch (Exception e) { Debug.LogWarning("[DearPompom] browser file sync failed: " + e.Message); }
#endif
        }
    }
}
