using System.Collections.Generic;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Шкафчик в рост Дэнни: по E Дэнни залезает внутрь, повторное E — вылезает.
    /// Внутри Дэнни рисуется между задней стенкой и дверцей (его спрайты опускаются на hiddenOrderShift),
    /// поэтому через щель в дверце видно его голову. Пока сидит — управление заблокировано, фонарик выключен.
    /// Собирается меню Overworked → P1 → Add Locker In Open Scene.
    /// </summary>
    public class HideLocker : InteractableBase
    {
        [Tooltip("На сколько опустить sortingOrder спрайтов Дэнни внутри: ниже дверцы, выше задней стенки")]
        [SerializeField] int hiddenOrderShift = 15;

        [Tooltip("Подсказка, пока Дэнни снаружи")]
        [SerializeField] string hidePrompt = "Hide";

        [Tooltip("Подсказка, пока Дэнни внутри")]
        [SerializeField] string leavePrompt = "Leave";

        /// <summary>Шкафчик, в котором сейчас сидит Дэнни (InteractionSystem держит его в фокусе). Нет — null.</summary>
        public static HideLocker Occupied { get; private set; }

        // Сброс при входе в Play: при выключенном Domain Reload статика пережила бы прошлый запуск.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOccupied() => Occupied = null;

        /// <summary>Дэнни сейчас в шкафчике.</summary>
        public bool IsHidden { get; private set; }

        readonly List<(SpriteRenderer sr, int order)> shifted = new List<(SpriteRenderer, int)>();
        bool flashlightWasOn;

        public override string Prompt => IsHidden ? leavePrompt : hidePrompt;

        protected override void OnInteract()
        {
            if (IsHidden) { Exit(); return; }
            var player = PlayerController.Instance;
            if (player == null) return;
            IsHidden = true;
            Occupied = this;

            var fl = FlashlightController.Instance;
            flashlightWasOn = fl != null && fl.IsOn;
            if (flashlightWasOn) fl.Toggle();

            // Встаём по центру шкафчика и уходим за дверцу.
            var p = player.transform.position;
            player.Teleport(new Vector3(transform.position.x, p.y, p.z));
            LockPlayer(true);
            shifted.Clear();
            foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                shifted.Add((sr, sr.sortingOrder));
                sr.sortingOrder -= hiddenOrderShift;
            }
        }

        /// <summary>Вылезти из шкафчика (E или из сценария).</summary>
        public void Exit()
        {
            if (!IsHidden) return;
            IsHidden = false;
            if (Occupied == this) Occupied = null;
            foreach (var (sr, order) in shifted)
                if (sr != null) sr.sortingOrder = order;
            shifted.Clear();
            LockPlayer(false);
            if (flashlightWasOn && FlashlightController.Instance != null) FlashlightController.Instance.Toggle();
            Finish();
        }

        void OnDisable() => Exit();
    }
}
