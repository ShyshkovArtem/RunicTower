using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RunicTower.Audio
{
    [System.Serializable]
    public sealed class KeyedAudioCue
    {
        [SerializeField] private string key;
        [SerializeField] private string[] additionalKeys;
        [SerializeField] private AudioCue cue;

        public string Key => key;
        public IEnumerable<string> Keys => EnumerateKeys();
        public AudioCue Cue => cue;

        public bool Matches(string requestedKey)
        {
            if (string.IsNullOrWhiteSpace(requestedKey))
            {
                return false;
            }

            return Keys.Any(candidate =>
                string.Equals(candidate, requestedKey.Trim(), System.StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerable<string> EnumerateKeys()
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                yield return key;
            }

            if (additionalKeys == null)
            {
                yield break;
            }

            foreach (string alias in additionalKeys)
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    yield return alias;
                }
            }
        }
    }

    [System.Serializable]
    public sealed class AudioCue
    {
        [SerializeField] private AudioClip[] clips;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = Vector2.one;

        public bool HasClips => clips != null && clips.Length > 0;
        public float Volume => volume;
        public Vector2 PitchRange => GetSafePitchRange();

        public AudioClip GetRandomClip()
        {
            if (!HasClips)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }

        private Vector2 GetSafePitchRange()
        {
            if (pitchRange.x <= 0f && pitchRange.y <= 0f)
            {
                return Vector2.one;
            }

            float min = pitchRange.x <= 0f ? 1f : pitchRange.x;
            float max = pitchRange.y <= 0f ? min : pitchRange.y;
            return min <= max ? new Vector2(min, max) : new Vector2(max, min);
        }
    }

    public sealed class BattleAudioController : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private int sfxVoiceCount = 6;
        [SerializeField] private bool playMusicOnEnable = true;

        [Header("Music")]
        [SerializeField] private AudioCue backgroundMusic;

        [Header("Battle SFX")]
        [SerializeField] private AudioCue runeFlyCue;
        [SerializeField] private AudioCue runePlaceCue;
        [SerializeField] private AudioCue ritualCastCue;
        [SerializeField] private AudioCue damageCue;

        [Header("Book SFX")]
        [SerializeField] private AudioCue bookFlightCue;
        [SerializeField] private AudioCue bookOpenCue;
        [SerializeField] private AudioCue bookFlipCue;
        [SerializeField] private AudioCue bookCloseCue;

        [Header("Table SFX")]
        [SerializeField] private KeyedAudioCue[] keyedSfxCues;

        private readonly List<AudioSource> _sfxVoices = new();
        private int _nextSfxVoiceIndex;

        private void Awake()
        {
            EnsureMusicSource();
            EnsureSfxVoices();
        }

        private void OnEnable()
        {
            if (playMusicOnEnable)
            {
                PlayBackgroundMusic();
            }
        }

        public void PlayBackgroundMusic()
        {
            if (musicSource == null || !backgroundMusic.HasClips)
            {
                return;
            }

            if (musicSource.isPlaying)
            {
                return;
            }

            AudioClip clip = backgroundMusic.GetRandomClip();
            if (clip == null)
            {
                return;
            }

            ConfigureSource(musicSource, backgroundMusic, Vector3.zero);
            musicSource.loop = true;
            musicSource.clip = clip;
            musicSource.Play();
        }

        public void PlayRuneFly(Vector3 worldPosition)
        {
            PlaySfx(runeFlyCue, worldPosition);
        }

        public void PlayRunePlace(Vector3 worldPosition)
        {
            PlaySfx(runePlaceCue, worldPosition);
        }

        public void PlayRitualCast(Vector3 worldPosition)
        {
            PlaySfx(ritualCastCue, worldPosition);
        }

        public void PlayDamage(Vector3 worldPosition)
        {
            PlaySfx(damageCue, worldPosition);
        }

        public void PlayBookOpen(Vector3 worldPosition)
        {
            PlaySfx(bookOpenCue, worldPosition);
        }

        public void PlayBookFlight(Vector3 worldPosition)
        {
            PlaySfx(bookFlightCue, worldPosition);
        }

        public void PlayBookFlip(Vector3 worldPosition)
        {
            PlaySfx(bookFlipCue, worldPosition);
        }

        public void PlayBookClose(Vector3 worldPosition)
        {
            PlaySfx(bookCloseCue, worldPosition);
        }

        public void PlaySfxKey(string key, Vector3 worldPosition)
        {
            AudioCue cue = FindKeyedCue(key);
            if (cue != null)
            {
                PlaySfx(cue, worldPosition);
            }
        }

        private AudioCue FindKeyedCue(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || keyedSfxCues == null)
            {
                return null;
            }

            foreach (KeyedAudioCue entry in keyedSfxCues)
            {
                if (entry?.Cue != null &&
                    entry.Matches(key))
                {
                    return entry.Cue;
                }
            }

            return null;
        }

        private void PlaySfx(AudioCue cue, Vector3 worldPosition)
        {
            if (cue == null || !cue.HasClips)
            {
                return;
            }

            AudioSource source = GetNextSfxVoice();
            if (source == null)
            {
                return;
            }

            AudioClip clip = cue.GetRandomClip();
            if (clip == null)
            {
                return;
            }

            ConfigureSource(source, cue, worldPosition);
            source.loop = false;
            source.clip = clip;
            source.Play();
        }

        private void EnsureMusicSource()
        {
            if (musicSource != null)
            {
                return;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }

        private void EnsureSfxVoices()
        {
            if (_sfxVoices.Count > 0)
            {
                return;
            }

            int voiceCount = Mathf.Max(1, sfxVoiceCount);
            for (int index = 0; index < voiceCount; index++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                _sfxVoices.Add(source);
            }
        }

        private AudioSource GetNextSfxVoice()
        {
            if (_sfxVoices.Count == 0)
            {
                EnsureSfxVoices();
            }

            if (_sfxVoices.Count == 0)
            {
                return null;
            }

            AudioSource source = _sfxVoices[_nextSfxVoiceIndex];
            _nextSfxVoiceIndex = (_nextSfxVoiceIndex + 1) % _sfxVoices.Count;
            return source;
        }

        private static void ConfigureSource(AudioSource source, AudioCue cue, Vector3 worldPosition)
        {
            source.transform.position = worldPosition;
            source.volume = cue.Volume;
            source.pitch = Random.Range(cue.PitchRange.x, cue.PitchRange.y);
            source.spatialBlend = 0f;
        }
    }
}
