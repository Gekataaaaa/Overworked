using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Проигрывает галлюцинации. ЗАГЛУШКА: сразу ставит рассудок и вызывает done — заполняет P4.
    /// </summary>
    public class HallucinationManager : SingletonBehaviour<HallucinationManager>
    {
        /// <summary>Проигрывает галлюцинацию h, по окончании вызывает done.</summary>
        public void Play(HallucinationEvent h, Action done = null)
        {
            Debug.Log($"[Hallucinations] Play {h?.name}");
            if (h != null)
            {
                if (HeartbeatManager.Instance != null) HeartbeatManager.Instance.Spike(h.HeartbeatSpike, h.Duration);
                if (SanityManager.Instance != null) SanityManager.Instance.SetSanity(h.SanityAfter);
            }
            done?.Invoke();
        }
    }
}
