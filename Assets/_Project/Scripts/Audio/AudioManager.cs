using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Один на игру: каналы Music / Ambient / SFX / Heartbeat. ЗАГЛУШКА — заполняет P4.
    /// </summary>
    public class AudioManager : SingletonBehaviour<AudioManager>
    {
        /// <summary>Короткий звук.</summary>
        public void PlaySfx(AudioClip c) => Debug.Log($"[Audio] Sfx {c?.name}");

        /// <summary>Фоновый зацикленный звук (гул ламп, ИВЛ).</summary>
        public void PlayAmbient(AudioClip c) => Debug.Log($"[Audio] Ambient {c?.name}");

        /// <summary>Остановить всё (конец скримера).</summary>
        public void StopAll() => Debug.Log("[Audio] StopAll");
    }
}
