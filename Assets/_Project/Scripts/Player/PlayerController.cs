using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Управление Дэнни: только влево/вправо, без прыжка и бега.
    /// ЗАГЛУШКА: методы только логируют, движение пишет P1 на этапе 1.
    /// </summary>
    public class PlayerController : SingletonBehaviour<PlayerController>
    {
        [Tooltip("Скорость ходьбы, юнитов в секунду")]
        [SerializeField] float speed = 3f;

        /// <summary>Текущий режим управления.</summary>
        public PlayerState State { get; private set; } = PlayerState.Normal;

        /// <summary>Блокирует/возвращает управление (телефон, мини-игры, сцены).</summary>
        public void SetInputLocked(bool locked)
        {
            State = locked ? PlayerState.Locked : PlayerState.Normal;
            Debug.Log($"[Player] SetInputLocked({locked})");
        }

        /// <summary>WhiteRoom: можно идти только вправо.</summary>
        public void SetOnlyRight(bool value) => Debug.Log($"[Player] SetOnlyRight({value})");

        /// <summary>Сбой ввода на Floor_02: персонаж идёт дальше после отпускания клавиши.</summary>
        public void StartInputDrift(float seconds) => Debug.Log($"[Player] StartInputDrift({seconds})");

        /// <summary>Мгновенно ставит игрока в точку (вход/выход из комнаты, под затемнением).</summary>
        public void Teleport(Transform point)
        {
            transform.position = point.position;
            Debug.Log($"[Player] Teleport → {point.name}");
        }

        /// <summary>Анимация по имени: "LookAtScreen", "Fall", "LieDown", "StandUp".</summary>
        public void PlayAnim(string animName) => Debug.Log($"[Player] PlayAnim({animName})");
    }
}
