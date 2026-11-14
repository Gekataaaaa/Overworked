using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Цепочка шагов сценарной сцены (реплика, сообщение, блок управления, галлюцинация…),
    /// настраивается в инспекторе. ЗАГЛУШКА — заполняет P3 (паспорт, раздел 17).
    /// </summary>
    public class ScriptedSequence : MonoBehaviour
    {
        /// <summary>Запускает цепочку с первого шага.</summary>
        public void Play() => Debug.Log($"[Story] Play {name}");
    }
}
