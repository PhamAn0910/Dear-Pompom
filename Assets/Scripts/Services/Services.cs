using DearPompom.Core;
using UnityEngine;

namespace DearPompom
{
    /// <summary>
    /// Creates and holds the long-lived services (PRD 8.2 "Bootstrap").
    ///
    /// The PRD describes a Bootstrap scene. This project is currently edited without the Unity
    /// Editor, so scenes cannot be authored; instead Unity calls <see cref="Boot"/> before the
    /// first scene loads, whichever scene that is. The guarantee is the same: by the time any
    /// scene script runs Awake, every service exists.
    ///
    /// Do not create services lazily from gameplay code and do not look them up with
    /// FindFirstObjectByType. Use Services.Progress, Services.Audio, Services.Events.
    /// </summary>
    public static class Services
    {
        public const string RootName = "[DearPompom Services]";

        public static IClock Clock { get; private set; }
        public static EventBus Events { get; private set; }
        public static SaveStore Store { get; private set; }
        public static UserProgressManager Progress { get; private set; }
        public static RecommendationService Recommendations { get; private set; }
        public static AudioManager Audio { get; private set; }

        public static bool IsBooted { get; private set; }

        // Runs even when "Enter Play Mode Options" skips the domain reload in the editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsBooted = false;
            Clock = null;
            Events = null;
            Store = null;
            Progress = null;
            Recommendations = null;
            Audio = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootBeforeFirstScene()
        {
            EnsureBooted();
        }

        /// <summary>Idempotent. Safe to call from anywhere on the main thread.</summary>
        public static void EnsureBooted()
        {
            if (IsBooted) return;
            IsBooted = true; // set first: the components created below read Services in Awake

            Clock = new SystemClock();
            Events = new EventBus();
            Events.OnHandlerError = Debug.LogException;
            Store = new SaveStore();

            var root = new GameObject(RootName);
            Object.DontDestroyOnLoad(root);

            // Order matters: progress loads the save that audio and recommendations read.
            Progress = root.AddComponent<UserProgressManager>();
            Audio = root.AddComponent<AudioManager>();
            Recommendations = new RecommendationService(Progress, Events, Clock);
            root.AddComponent<SceneVisitLogger>();

            Debug.Log("[DearPompom] services ready. Save folder: " + Store.Folder);
        }
    }
}
