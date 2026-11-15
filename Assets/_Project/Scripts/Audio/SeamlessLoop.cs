using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Бесшовный фоновый цикл на канале SFX (гул ламп, гул компьютеров): две копии клипа по очереди
    /// с перекрёстным затуханием на стыке; тишина кодировщика в начале mp3 пропускается. Не MonoBehaviour —
    /// его держит и каждый кадр вызывает владелец (LampHum, ComputerHumPlayer).
    /// </summary>
    public class SeamlessLoop
    {
        readonly AudioClip clip;
        readonly float skipStart, crossfade;
        AudioSource current, previous;

        /// <param name="skipStart">Сколько секунд пропустить в начале клипа (тишина кодировщика mp3).</param>
        /// <param name="crossfade">Длина перекрёстного затухания на стыке, секунд.</param>
        public SeamlessLoop(AudioClip clip, float skipStart = 0.1f, float crossfade = 0.5f)
        {
            this.clip = clip;
            this.skipStart = skipStart;
            this.crossfade = crossfade;
        }

        /// <summary>Вызывать каждый кадр: volume 0..1 (умножается на канал SFX); 0 — источники отпускаются.</summary>
        public void Update(float volume)
        {
            var audio = AudioManager.Instance;
            if (clip == null || audio == null) return;
            if (volume <= 0f)
            {
                Stop();
                return;
            }

            // Новая копия — когда текущая подошла к концу (или её погасила смена сцены — AudioManager).
            if (current == null || current.time >= clip.length - crossfade - 0.05f)
            {
                Release(ref previous);
                previous = current;
                current = audio.PlaySfxHeld(clip, 0f);
                if (current != null) current.time = Mathf.Min(skipStart, clip.length * 0.5f);
            }

            // Затухание по времени самих клипов: на паузе (AudioListener.pause) оно стоит вместе со звуком.
            float k = current != null ? Mathf.Clamp01((current.time - skipStart) / crossfade) : 1f;
            audio.SetSfxVolume(current, volume * k);
            if (previous != null)
            {
                audio.SetSfxVolume(previous, volume * (1f - k));
                if (k >= 1f) Release(ref previous);
            }
        }

        /// <summary>Сразу заглушить и отпустить источники.</summary>
        public void Stop()
        {
            Release(ref current);
            Release(ref previous);
        }

        static void Release(ref AudioSource src)
        {
            if (src != null && AudioManager.Instance != null) AudioManager.Instance.StopSfx(src, 0f);
            src = null;
        }
    }
}
