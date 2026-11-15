using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Overworked
{
    /// <summary>
    /// Один на игру (префаб _Systems): каналы Music / Ambient / SFX / Heartbeat.
    /// Под каждый канал создаёт свой AudioSource; если назначены группы Audio Mixer —
    /// звук идёт через них (громкость каналов тогда удобно крутить в миксере).
    /// </summary>
    public class AudioManager : SingletonBehaviour<AudioManager>
    {
        [Header("Audio Mixer (необязательно)")]
        [Tooltip("Группа Music в Settings/MainMixer")]
        [SerializeField] AudioMixerGroup musicGroup;
        [Tooltip("Группа Ambient в Settings/MainMixer")]
        [SerializeField] AudioMixerGroup ambientGroup;
        [Tooltip("Группа SFX в Settings/MainMixer")]
        [SerializeField] AudioMixerGroup sfxGroup;
        [Tooltip("Группа Heartbeat в Settings/MainMixer")]
        [SerializeField] AudioMixerGroup heartbeatGroup;

        [Header("Громкость каналов")]
        [Tooltip("Громкость музыки 0..1")]
        [Range(0f, 1f)] [SerializeField] float musicVolume = 0.6f;
        [Tooltip("Громкость фона (гул ламп, ИВЛ) 0..1")]
        [Range(0f, 1f)] [SerializeField] float ambientVolume = 0.5f;
        [Tooltip("Громкость коротких звуков 0..1")]
        [Range(0f, 1f)] [SerializeField] float sfxVolume = 1f;
        [Tooltip("Громкость сердцебиения 0..1 (дальше умножается на громкость удара от HeartbeatManager)")]
        [Range(0f, 1f)] [SerializeField] float heartbeatVolume = 1f;

        [Tooltip("За сколько секунд фон затухает и проявляется при смене клипа")]
        [SerializeField] float ambientFade = 1f;

        AudioSource music, ambient, sfx, heartbeat;
        Coroutine ambientRoutine;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            music = CreateSource("Music", musicGroup, true);
            ambient = CreateSource("Ambient", ambientGroup, true);
            sfx = CreateSource("SFX", sfxGroup, false);
            heartbeat = CreateSource("Heartbeat", heartbeatGroup, false);
        }

        AudioSource CreateSource(string channel, AudioMixerGroup group, bool loop)
        {
            var go = new GameObject($"Audio_{channel}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f; // игра 2D: все звуки «в голове», без панорамы по позиции
            src.outputAudioMixerGroup = group;
            return src;
        }

        /// <summary>Короткий звук на канале SFX (можно одновременно несколько).</summary>
        public void PlaySfx(AudioClip c) => PlaySfx(c, 1f);

        /// <summary>Короткий звук с громкостью volume 0..1 (умножается на громкость канала).</summary>
        public void PlaySfx(AudioClip c, float volume)
        {
            if (c == null) return;
            sfx.PlayOneShot(c, volume * sfxVolume);
        }

        /// <summary>
        /// Звук на канале SFX на своём источнике: holdSeconds в полную громкость, затем затухает за fadeSeconds
        /// и источник удаляется. Источник живёт в _Systems, поэтому переживает смену сцены (шаги по лестнице).
        /// </summary>
        public void PlaySfxFading(AudioClip c, float volume, float holdSeconds, float fadeSeconds)
        {
            if (c == null) return;
            var src = CreateSource("SFX_Fading", sfxGroup, false);
            src.clip = c;
            src.volume = volume * sfxVolume;
            src.Play();
            StartCoroutine(FadeOutAndRemove(src, holdSeconds, fadeSeconds));
        }

        // Время unscaled: таймер идёт и при timeScale = 0 (переход между этажами не зависит от игрового времени).
        IEnumerator FadeOutAndRemove(AudioSource src, float hold, float fade)
        {
            for (float t = 0f; t < hold && Alive(src); t += Time.unscaledDeltaTime) yield return null;
            float start = src != null ? src.volume : 0f;
            for (float t = 0f; t < fade && Alive(src); t += Time.unscaledDeltaTime)
            {
                src.volume = start * (1f - t / fade);
                yield return null;
            }
            if (src != null) Destroy(src.gameObject);
        }

        // На паузе AudioListener.pause останавливает источник (isPlaying = false) — это не конец звука.
        static bool Alive(AudioSource src) => src != null && (src.isPlaying || AudioListener.pause);

        /// <summary>
        /// Фоновый зацикленный звук (гул ламп, ИВЛ). Старый фон плавно затухает, новый проявляется.
        /// Тот же клип повторно не перезапускается; null — плавно выключить фон.
        /// </summary>
        public void PlayAmbient(AudioClip c)
        {
            if (c != null && ambient.clip == c && ambient.isPlaying) return;
            if (ambientRoutine != null) StopCoroutine(ambientRoutine);
            ambientRoutine = StartCoroutine(SwapAmbient(c));
        }

        IEnumerator SwapAmbient(AudioClip next)
        {
            // Время без масштаба: фон должен меняться и на паузе, и в замедлении.
            if (ambient.isPlaying)
                yield return Fade(ambient, ambient.volume, 0f);
            ambient.Stop();
            ambient.clip = next;
            if (next == null) yield break;
            ambient.volume = 0f;
            ambient.Play();
            yield return Fade(ambient, 0f, ambientVolume);
            ambientRoutine = null;
        }

        IEnumerator Fade(AudioSource src, float from, float to)
        {
            for (float t = 0f; t < ambientFade; t += Time.unscaledDeltaTime)
            {
                src.volume = Mathf.Lerp(from, to, t / ambientFade);
                yield return null;
            }
            src.volume = to;
        }

        /// <summary>Музыка (меню, титры). null — выключить.</summary>
        public void PlayMusic(AudioClip c)
        {
            if (c == null) { music.Stop(); return; }
            if (music.clip == c && music.isPlaying) return;
            music.clip = c;
            music.volume = musicVolume;
            music.Play();
        }

        /// <summary>Один удар сердца на канале Heartbeat; volume 0..1 задаёт HeartbeatManager.</summary>
        public void PlayHeartbeat(AudioClip c, float volume)
        {
            if (c == null) return;
            heartbeat.PlayOneShot(c, volume * heartbeatVolume);
        }

        /// <summary>Оборвать звучащий удар сердца (выход в меню).</summary>
        public void StopHeartbeat() => heartbeat.Stop();

        /// <summary>Мгновенно остановить всё (конец скримера: «звук резко обрывается»).</summary>
        public void StopAll()
        {
            if (ambientRoutine != null) StopCoroutine(ambientRoutine);
            ambientRoutine = null;
            music.Stop();
            ambient.Stop();
            ambient.clip = null;
            sfx.Stop();
            heartbeat.Stop();
            Debug.Log("[Audio] StopAll");
        }
    }
}
