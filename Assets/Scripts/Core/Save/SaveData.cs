using System;
using System.Collections.Generic;

namespace DearPompom.Core
{
    /// <summary>
    /// Root of the save file (save/progress.json). Bump <see cref="CurrentSchemaVersion"/> and add
    /// a step to <see cref="SaveMigrator"/> whenever a field changes meaning or is removed.
    /// Adding a new field with a sensible default does not need a version bump.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        /// <summary>ISO timestamp of when this save was first created (or migrated from 1.0).</summary>
        public string createdAt = "";
        /// <summary>Current story chapter, 1..4.</summary>
        public int chapter = 1;
        public List<ActivityRecord> records = new List<ActivityRecord>();
        public RecommendationState recommendations = new RecommendationState();
        public SettingsData settings = new SettingsData();

        /// <summary>Repairs nulls left by hand-edited or partially written files.</summary>
        public void EnsureValid()
        {
            if (records == null) records = new List<ActivityRecord>();
            records.RemoveAll(r => r == null);
            if (recommendations == null) recommendations = new RecommendationState();
            recommendations.EnsureValid();
            if (settings == null) settings = new SettingsData();
            settings.Clamp();
            if (chapter < 1) chapter = 1;
            if (chapter > 4) chapter = 4;
        }
    }

    /// <summary>Player preferences (PRD M2, F1). Lives in the save so it travels with it.</summary>
    [Serializable]
    public class SettingsData
    {
        public float musicVolume = 0.8f;
        public float sfxVolume = 1f;
        public float ambienceVolume = 0.8f;
        public bool muted;
        public bool reduceMotion;
        /// <summary>"en" or "vi".</summary>
        public string language = "en";

        public void Clamp()
        {
            musicVolume = Clamp01(musicVolume);
            sfxVolume = Clamp01(sfxVolume);
            ambienceVolume = Clamp01(ambienceVolume);
            if (string.IsNullOrEmpty(language)) language = "en";
        }

        private static float Clamp01(float v)
        {
            if (float.IsNaN(v)) return 1f;
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }
    }

    /// <summary>Only used to peek at the version of a file before choosing how to parse it.</summary>
    [Serializable]
    public class SaveVersionProbe
    {
        public int schemaVersion;
    }
}
