using System;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Ищет ближайший IInteractable рядом с игроком и по E вызывает Interact().
    /// Подсказку «[E] …» над объектом и жёлтый контур рисует InteractionHighlight (добавляется сюда же).
    /// Создаёт себя сам при старте игры (DontDestroyOnLoad), поэтому в сцены и в _Systems его класть не нужно.
    /// Объекту для взаимодействия нужен Collider2D (обычно триггер) и компонент с IInteractable.
    /// </summary>
    public class InteractionSystem : SingletonBehaviour<InteractionSystem>
    {
        [Tooltip("Радиус поиска объектов вокруг игрока, юнитов")]
        [SerializeField] float radius = 1.2f;

        /// <summary>Объект, на который сейчас указывает подсказка, или null.</summary>
        public IInteractable Focused { get; private set; }

        /// <summary>Сменился объект под подсказкой (null — подсказки нет).</summary>
        public event Action<IInteractable> OnFocusChanged;

        InteractionHighlight highlight;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (Instance != null) return;
            var go = new GameObject("InteractionSystem");
            DontDestroyOnLoad(go);
            go.AddComponent<InteractionSystem>();
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            if (!TryGetComponent(out highlight)) highlight = gameObject.AddComponent<InteractionHighlight>();
        }

        void Update()
        {
            var best = FindBest();
            if (!ReferenceEquals(best, Focused))
            {
                Focused = best;
                highlight.SetTarget(best);
                OnFocusChanged?.Invoke(best);
            }

            if (best != null && GameInput.Interact != null && GameInput.Interact.WasPressedThisFrame())
                best.Interact();
        }

        IInteractable FindBest()
        {
            var player = PlayerController.Instance;
            // Ни подсказки, ни контура, пока Дэнни заблокирован: телефон, мини-игра, катсцена, пауза.
            if (player == null || player.State != PlayerState.Normal || Time.timeScale == 0f) return null;

            Vector2 from = player.transform.position;
            IInteractable best = null;
            float bestDist = float.MaxValue;
            foreach (var col in Physics2D.OverlapCircleAll(from, radius))
            {
                foreach (var candidate in col.GetComponents<IInteractable>())
                {
                    if (candidate is Behaviour b && !b.isActiveAndEnabled) continue;
                    // Запертая лестница/дверь (CanInteract == false) не в фокусе — не подсвечивается.
                    if (!candidate.CanInteract) continue;
                    // Расстояние до края коллайдера, а не до центра: широкий стол и узкая дверь сравниваются честно.
                    float dist = (col.ClosestPoint(from) - from).sqrMagnitude;
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        best = candidate;
                    }
                }
            }
            return best;
        }
    }
}
