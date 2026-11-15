using System.Collections;
using System.Collections.Generic;
using DearPompom.Core;
using UnityEngine;

namespace DearPompom
{
    public enum AudioChannel
    {
        Music = 0,
        Sfx = 1,
        Ambience = 2,
    }

    /// <summary>
    /// The one place that controls loudness (PRD M2, M3). Volumes live in SaveData.settings.
    ///
    /// API:  PlayMusic(clip, fade)  StopMusic(fade)  PlaySfx(clip)  PlaySfx(id)  SetAmbience(clip, fade)
    ///       SetVolume(channel, 0..1)  SetMuted(bool)  DuckBackground(bool)
    ///
    /// AudioSources that live in scenes (theatre stems, the 1.0 music prefab) call
    /// <see cref="Register"/> so the sliders reach them too.
    ///
    /// No script may touch AudioListener.volume: it silences everything, including sources
    /// that belong to other systems. Use SetMuted or a channel volume.
    ///
    /// PRD M1 asks for an AudioMixer asset. Mixer assets can only be created in the Unity Editor,
    /// which this project currently does without, so channels are applied in code. Swapping in a
    /// real mixer later only changes <see cref="Apply"/>.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public const float DefaultMusicFade = 1.5f;

        private sealed class Tracked
        {
            public AudioSource Source;
            public AudioChannel Channel;
            public float BaseVolume;
            public bool Background;
            public float Fade = 1f; // 0..1, driven by crossfades
        }

        private readonly List<Tracked> _tracked = new List<Tracked>();
        private readonly Dictionary<string, AudioClip> _sfxById = new Dictionary<string, AudioClip>();

        private AudioSource _musicA, _musicB, _sfx, _ambience;
        private bool _musicAIsCurrent = true;
        private float _duck = 1f;          // multiplies background music only
        private Coroutine _duckRoutine;

        private SettingsData Settings
        {
            get
            {
                UserProgressManager progress = Services.Progress;
                return progress != null && progress.Save != null ? progress.Save.settings : null;
            }
        }

        void Awake()
        {
            _musicA = CreateSource("Music A", true);
            _musicB = CreateSource("Music B", true);
            _ambience = CreateSource("Ambience", true);
            _sfx = CreateSource("Sfx", false);

            Register(_musicA, AudioChannel.Music, true);
            Register(_musicB, AudioChannel.Music, true);
            Register(_ambience, AudioChannel.Ambience, false);
            Register(_sfx, AudioChannel.Sfx, false);
            Apply();
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.volume = 1f;
            return source;
        }

        // ------------------------------------------------------------------ volumes

        public float GetVolume(AudioChannel channel)
        {
            SettingsData s = Settings;
            if (s == null) return 1f;
            switch (channel)
            {
                case AudioChannel.Music: return s.musicVolume;
                case AudioChannel.Sfx: return s.sfxVolume;
                default: return s.ambienceVolume;
            }
        }

        /// <summary>Sets a channel volume (0..1), applies it and saves it.</summary>
        public void SetVolume(AudioChannel channel, float volume)
        {
            SettingsData s = Settings;
            if (s == null) return;
            volume = Mathf.Clamp01(volume);
            switch (channel)
            {
                case AudioChannel.Music: s.musicVolume = volume; break;
                case AudioChannel.Sfx: s.sfxVolume = volume; break;
                default: s.ambienceVolume = volume; break;
            }
            OnSettingsChanged();
        }

        public bool Muted
        {
            get { SettingsData s = Settings; return s != null && s.muted; }
        }

        public void SetMuted(bool muted)
        {
            SettingsData s = Settings;
            if (s == null) return;
            s.muted = muted;
            OnSettingsChanged();
        }

        private void OnSettingsChanged()
        {
            Apply();
            Services.Progress.SaveProgress();
            Services.Events.Publish(new SettingsChanged(Settings));
        }

        // ------------------------------------------------------------------ external sources

        /// <summary>
        /// Puts a scene AudioSource under a channel. Its current volume becomes its base level.
        /// <paramref name="background"/> marks area music that should fade out while the theatre plays.
        /// </summary>
        public void Register(AudioSource source, AudioChannel channel, bool background = false)
        {
            if (source == null) return;
            for (int i = 0; i < _tracked.Count; i++)
                if (_tracked[i].Source == source) return;
            _tracked.Add(new Tracked { Source = source, Channel = channel, BaseVolume = source.volume, Background = background });
            Apply();
        }

        public void Unregister(AudioSource source)
        {
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                if (_tracked[i].Source != source) continue;
                if (source != null) source.volume = _tracked[i].BaseVolume;
                _tracked.RemoveAt(i);
            }
        }

        /// <summary>Pushes the current settings to every tracked source. Destroyed sources are dropped.</summary>
        private void Apply()
        {
            bool muted = Muted;
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                Tracked t = _tracked[i];
                if (t.Source == null)
                {
                    _tracked.RemoveAt(i);
                    continue;
                }
                float volume = muted ? 0f : t.BaseVolume * GetVolume(t.Channel) * t.Fade;
                if (t.Background) volume *= _duck;
                t.Source.volume = volume;
            }
        }

        private Tracked Find(AudioSource source)
        {
            for (int i = 0; i < _tracked.Count; i++)
                if (_tracked[i].Source == source) return _tracked[i];
            return null;
        }

        // ------------------------------------------------------------------ music

        /// <summary>Crossfades to a new looping track. Asking for the track that is already playing does nothing.</summary>
        public void PlayMusic(AudioClip clip, float fadeSeconds = DefaultMusicFade)
        {
            if (clip == null)
            {
                StopMusic(fadeSeconds);
                return;
            }
            AudioSource current = _musicAIsCurrent ? _musicA : _musicB;
            if (current.clip == clip && current.isPlaying) return;

            AudioSource next = _musicAIsCurrent ? _musicB : _musicA;
            _musicAIsCurrent = !_musicAIsCurrent;

            next.clip = clip;
            next.loop = true;
            Find(next).Fade = 0f;
            Apply();
            next.Play();

            StartCoroutine(FadeSource(next, 1f, fadeSeconds, false));
            StartCoroutine(FadeSource(current, 0f, fadeSeconds, true));
        }

        public void StopMusic(float fadeSeconds = DefaultMusicFade)
        {
            StartCoroutine(FadeSource(_musicA, 0f, fadeSeconds, true));
            StartCoroutine(FadeSource(_musicB, 0f, fadeSeconds, true));
        }

        /// <summary>
        /// Fades area music down (true) or back up (false) without stopping it, e.g. while the
        /// theatre plays its own song. Affects only sources registered as background.
        /// </summary>
        public void DuckBackground(bool ducked, float fadeSeconds = DefaultMusicFade)
        {
            if (_duckRoutine != null) StopCoroutine(_duckRoutine);
            _duckRoutine = StartCoroutine(FadeDuck(ducked ? 0f : 1f, fadeSeconds));
        }

        // ------------------------------------------------------------------ ambience

        public void SetAmbience(AudioClip clip, float fadeSeconds = DefaultMusicFade)
        {
            if (clip == null)
            {
                StartCoroutine(FadeSource(_ambience, 0f, fadeSeconds, true));
                return;
            }
            if (_ambience.clip == clip && _ambience.isPlaying) return;
            _ambience.clip = clip;
            Find(_ambience).Fade = 0f;
            Apply();
            _ambience.Play();
            StartCoroutine(FadeSource(_ambience, 1f, fadeSeconds, false));
        }

        // ------------------------------------------------------------------ sfx

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || _sfx == null) return;
            _sfx.PlayOneShot(clip, volumeScale);
        }

        /// <summary>Makes a clip playable by id, so callers do not need a reference to the asset.</summary>
        public void RegisterSfx(string id, AudioClip clip)
        {
            if (string.IsNullOrEmpty(id) || clip == null) return;
            _sfxById[id] = clip;
        }

        /// <summary>Plays a registered effect. Unknown ids are ignored (a missing sound must never break gameplay).</summary>
        public void PlaySfx(string id)
        {
            AudioClip clip;
            if (!string.IsNullOrEmpty(id) && _sfxById.TryGetValue(id, out clip)) PlaySfx(clip);
        }

        // ------------------------------------------------------------------ fades

        private IEnumerator FadeSource(AudioSource source, float target, float seconds, bool stopAtEnd)
        {
            Tracked tracked = Find(source);
            if (tracked == null) yield break;
            float start = tracked.Fade;
            float elapsed = 0f;
            while (seconds > 0f && elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                tracked.Fade = Mathf.Lerp(start, target, elapsed / seconds);
                Apply();
                yield return null;
            }
            tracked.Fade = target;
            Apply();
            if (stopAtEnd && target <= 0f && source != null) source.Stop();
        }

        private IEnumerator FadeDuck(float target, float seconds)
        {
            float start = _duck;
            float elapsed = 0f;
            while (seconds > 0f && elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _duck = Mathf.Lerp(start, target, elapsed / seconds);
                Apply();
                yield return null;
            }
            _duck = target;
            Apply();
            _duckRoutine = null;
        }
    }
}
