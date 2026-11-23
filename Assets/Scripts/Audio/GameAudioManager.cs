using UnityEngine;

namespace HollowKeep.Audio
{
    /// <summary>
    /// Gerenciador central de áudio (trilha sonora em loop e efeitos sonoros one-shot).
    /// Carrega clipes de Assets/Resources/Audio/ e possui síntese procedural de fallback.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameAudioManager : MonoBehaviour
    {
        private static GameAudioManager _instance;
        private static bool _isQuitting;

        [Header("Fontes de Áudio")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Volumes")]
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.28f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.72f;

        [Header("Clipes (Carregados de Resources/Audio se vazios)")]
        [SerializeField] private AudioClip bgmClip;
        [SerializeField] private AudioClip arrowShootClip;
        [SerializeField] private AudioClip arrowHitClip;
        [SerializeField] private AudioClip cannonShootClip;
        [SerializeField] private AudioClip cannonExplodeClip;
        [SerializeField] private AudioClip enemyDieClip;
        [SerializeField] private AudioClip castleHitClip;
        [SerializeField] private AudioClip towerBuildClip;
        [SerializeField] private AudioClip towerUpgradeClip;
        [SerializeField] private AudioClip towerDemolishClip;
        [SerializeField] private AudioClip waveStartClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;

        public static GameAudioManager Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameAudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new("GameAudioManager");
                        _instance = go.AddComponent<GameAudioManager>();
                    }
                }
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapOnSceneLoad()
        {
            _isQuitting = false;
            _ = Instance;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureAudioSources();
            LoadOrSynthesizeClips();
            StartBackgroundMusic();
        }

        private void OnApplicationQuit()
        {
            _isQuitting = true;
        }

        private void EnsureAudioSources()
        {
            if (musicSource == null)
            {
                GameObject musicObj = new("MusicSource");
                musicObj.transform.SetParent(transform, false);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.spatialBlend = 0f;
                musicSource.volume = musicVolume;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new("SfxSource");
                sfxObj.transform.SetParent(transform, false);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
                sfxSource.volume = sfxVolume;
            }
        }

        private void LoadOrSynthesizeClips()
        {
            bgmClip = LoadClipOrFallback(bgmClip, "Audio/bgm_hollow_keep", CreateAmbientBgmFallback);
            arrowShootClip = LoadClipOrFallback(arrowShootClip, "Audio/sfx_arrow_shoot", () => CreateSweepClip("sfx_arrow_shoot", 0.12f, 680f, 240f, 0.35f, true));
            arrowHitClip = LoadClipOrFallback(arrowHitClip, "Audio/sfx_arrow_hit", () => CreateSweepClip("sfx_arrow_hit", 0.09f, 320f, 110f, 0.45f, true));
            cannonShootClip = LoadClipOrFallback(cannonShootClip, "Audio/sfx_cannon_shoot", () => CreateSweepClip("sfx_cannon_shoot", 0.26f, 180f, 55f, 0.7f, true));
            cannonExplodeClip = LoadClipOrFallback(cannonExplodeClip, "Audio/sfx_cannon_explode", () => CreateSweepClip("sfx_cannon_explode", 0.42f, 140f, 36f, 0.85f, true));
            enemyDieClip = LoadClipOrFallback(enemyDieClip, "Audio/sfx_enemy_die", () => CreateSweepClip("sfx_enemy_die", 0.22f, 300f, 95f, 0.5f, true));
            castleHitClip = LoadClipOrFallback(castleHitClip, "Audio/sfx_castle_hit", () => CreateSweepClip("sfx_castle_hit", 0.35f, 160f, 48f, 0.75f, true));
            towerBuildClip = LoadClipOrFallback(towerBuildClip, "Audio/sfx_tower_build", () => CreateArpeggioClip("sfx_tower_build", new[] { 261.6f, 329.6f, 392.0f }, 0.07f));
            towerUpgradeClip = LoadClipOrFallback(towerUpgradeClip, "Audio/sfx_tower_upgrade", () => CreateArpeggioClip("sfx_tower_upgrade", new[] { 329.6f, 392.0f, 523.25f }, 0.08f));
            towerDemolishClip = LoadClipOrFallback(towerDemolishClip, "Audio/sfx_tower_demolish", () => CreateArpeggioClip("sfx_tower_demolish", new[] { 300f, 220f, 146.8f }, 0.07f));
            waveStartClip = LoadClipOrFallback(waveStartClip, "Audio/sfx_wave_start", () => CreateArpeggioClip("sfx_wave_start", new[] { 196f, 293.66f }, 0.18f));
            uiClickClip = LoadClipOrFallback(uiClickClip, "Audio/sfx_ui_click", () => CreateSweepClip("sfx_ui_click", 0.055f, 520f, 660f, 0.1f, false));
            victoryClip = LoadClipOrFallback(victoryClip, "Audio/sfx_victory", () => CreateArpeggioClip("sfx_victory", new[] { 261.6f, 329.6f, 392.0f, 523.25f }, 0.14f));
            defeatClip = LoadClipOrFallback(defeatClip, "Audio/sfx_defeat", () => CreateArpeggioClip("sfx_defeat", new[] { 293.66f, 261.6f, 220f, 146.83f }, 0.18f));
        }

        private static AudioClip LoadClipOrFallback(AudioClip existing, string resourcePath, System.Func<AudioClip> fallbackFactory)
        {
            if (existing != null) return existing;
            AudioClip loaded = Resources.Load<AudioClip>(resourcePath);
            if (loaded != null) return loaded;
            return fallbackFactory?.Invoke();
        }

        public void StartBackgroundMusic()
        {
            if (musicSource == null || bgmClip == null) return;
            musicSource.volume = musicVolume;
            if (musicSource.clip != bgmClip)
            {
                musicSource.clip = bgmClip;
            }
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, Mathf.Clamp(volumeScale, 0.05f, 1.5f) * sfxVolume);
        }

        public static void PlayArrowShoot() => Instance?.PlayOneShot(Instance.arrowShootClip, 0.55f);
        public static void PlayArrowHit() => Instance?.PlayOneShot(Instance.arrowHitClip, 0.5f);
        public static void PlayCannonShoot() => Instance?.PlayOneShot(Instance.cannonShootClip, 0.85f);
        public static void PlayCannonExplode() => Instance?.PlayOneShot(Instance.cannonExplodeClip, 0.95f);
        public static void PlayEnemyDie() => Instance?.PlayOneShot(Instance.enemyDieClip, 0.65f);
        public static void PlayCastleHit() => Instance?.PlayOneShot(Instance.castleHitClip, 1.0f);
        public static void PlayTowerBuild() => Instance?.PlayOneShot(Instance.towerBuildClip, 0.85f);
        public static void PlayTowerUpgrade() => Instance?.PlayOneShot(Instance.towerUpgradeClip, 0.95f);
        public static void PlayTowerDemolish() => Instance?.PlayOneShot(Instance.towerDemolishClip, 0.85f);
        public static void PlayWaveStart() => Instance?.PlayOneShot(Instance.waveStartClip, 0.9f);
        public static void PlayUiClick() => Instance?.PlayOneShot(Instance.uiClickClip, 0.65f);
        public static void PlayVictory() => Instance?.PlayOneShot(Instance.victoryClip, 1.0f);
        public static void PlayDefeat() => Instance?.PlayOneShot(Instance.defeatClip, 1.0f);

        private static AudioClip CreateSweepClip(string clipName, float duration, float startFreq, float endFreq, float noiseMix, bool exponentialDecay)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(256, Mathf.RoundToInt(duration * sampleRate));
            float[] data = new float[sampleCount];
            float phase = 0f;
            uint rngState = 0x12345678u;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += (2f * Mathf.PI * freq) / sampleRate;

                rngState = rngState * 1664525u + 1013904223u;
                float noise = ((rngState & 0xFFFF) / 32768f) - 1f;
                float tone = Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2f);
                float mixed = Mathf.Lerp(tone, noise, noiseMix);
                float env = exponentialDecay ? Mathf.Pow(1f - t, 2.1f) : Mathf.Sin(t * Mathf.PI);
                data[i] = Mathf.Clamp(mixed * env * 0.6f, -0.95f, 0.95f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateArpeggioClip(string clipName, float[] notes, float noteDuration)
        {
            const int sampleRate = 44100;
            int samplesPerNote = Mathf.Max(256, Mathf.RoundToInt(noteDuration * sampleRate));
            int totalSamples = samplesPerNote * notes.Length;
            float[] data = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                float phase = 0f;
                for (int i = 0; i < samplesPerNote; i++)
                {
                    float t = (float)i / samplesPerNote;
                    phase += (2f * Mathf.PI * freq) / sampleRate;
                    float env = Mathf.Sin(Mathf.Min(1f, t * 8f) * Mathf.PI * 0.5f) * (1f - t * 0.7f);
                    float sample = (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f)) * env * 0.45f;
                    data[n * samplesPerNote + i] = Mathf.Clamp(sample, -0.95f, 0.95f);
                }
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateAmbientBgmFallback()
        {
            const int sampleRate = 22050;
            const float duration = 8f;
            int totalSamples = Mathf.RoundToInt(duration * sampleRate);
            float[] data = new float[totalSamples];
            float[] chordRoots = { 146.83f, 130.81f, 116.54f, 110.00f }; // D minor dark progression

            for (int i = 0; i < totalSamples; i++)
            {
                float time = (float)i / sampleRate;
                int bar = Mathf.FloorToInt(time / 2f) % chordRoots.Length;
                float root = chordRoots[bar];
                float fifth = root * 1.4983f;
                float barPhase = (time % 2f) / 2f;
                float env = Mathf.Sin(barPhase * Mathf.PI);

                float drone = 0.32f * Mathf.Sin(2f * Mathf.PI * (root * 0.5f) * time)
                            + 0.22f * Mathf.Sin(2f * Mathf.PI * root * time)
                            + 0.14f * Mathf.Sin(2f * Mathf.PI * fifth * time) * env;
                data[i] = Mathf.Clamp(drone * 0.55f, -0.9f, 0.9f);
            }

            AudioClip clip = AudioClip.Create("bgm_hollow_keep", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
