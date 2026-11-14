using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Пульс: базовый BPM от рассудка, спайки на галлюцинациях, сценарный режим в WhiteRoom.
    /// ЗАГЛУШКА — заполняет P4.
    /// </summary>
    public class HeartbeatManager : SingletonBehaviour<HeartbeatManager>
    {
        /// <summary>Текущий BPM для UI.</summary>
        public float Bpm { get; private set; } = 70f;

        /// <summary>Временно поднять пульс на addBpm на seconds секунд.</summary>
        public void Spike(float addBpm, float seconds) => Debug.Log($"[Heartbeat] Spike +{addBpm} for {seconds}s");

        /// <summary>Сценарный режим: BPM задаётся только через SetBpm (WhiteRoom).</summary>
        public void SetScripted(bool on) => Debug.Log($"[Heartbeat] Scripted={on}");

        /// <summary>Задать BPM вручную (работает в сценарном режиме).</summary>
        public void SetBpm(float bpm) { Bpm = bpm; Debug.Log($"[Heartbeat] Bpm={bpm}"); }
    }
}
