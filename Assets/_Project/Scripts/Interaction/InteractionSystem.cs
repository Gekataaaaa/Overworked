using System;
using TMPro;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Ищет ближайший IInteractable рядом с игроком, показывает подсказку «[E] …», по E вызывает Interact().
    /// Создаёт себя сам при старте игры (DontDestroyOnLoad), поэтому в сцены и в _Systems его класть не нужно.
    /// Объекту для взаимодействия нужен Collider2D (обычно триггер) и компонент с IInteractable.
    /// </summary>
    public class InteractionSystem : SingletonBehaviour<InteractionSystem>
    {
        [Tooltip("Радиус поиска объектов вокруг игрока, юнитов")]
        [SerializeField] float radius = 1.2f;

        [Tooltip("Текст подсказки в HUD. Пусто — система рисует свою подсказку внизу по центру")]
        [SerializeField] TMP_Text promptText;

        /// <summary>Объект, на который сейчас указывает подсказка, или null.</summary>
        public IInteractable Focused { get; private set; }

        /// <summary>Сменился объект под подсказкой (null — подсказки нет).</summary>
        public event Action<IInteractable> OnFocusChanged;

        string shownPrompt;

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
            if (promptText == null)
            {
                // Своя подсказка: над субтитрами P3, чтобы не перекрывать реплики
                var canvas = OverlayUi.CreateCanvas(transform, "PromptCanvas", 50);
                promptText = OverlayUi.Label(canvas.transform, "InteractionPrompt", "", 34,
                    new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(900f, 60f));
            }
            ShowPrompt(null);
        }

        void Update()
        {
            var best = FindBest();
            if (!ReferenceEquals(best, Focused))
            {
                Focused = best;
                OnFocusChanged?.Invoke(best);
            }
            ShowPrompt(best != null ? $"[E] {best.Prompt}" : null);

            if (best != null && GameInput.Interact != null && GameInput.Interact.WasPressedThisFrame())
                best.Interact();
        }

        IInteractable FindBest()
        {
            var player = PlayerController.Instance;
            // Подсказки нет, пока Дэнни заблокирован: телефон, мини-игра, катсцена, пауза.
            if (player == null || player.State != PlayerState.Normal || Time.timeScale == 0f) return null;

            Vector2 from = player.transform.position;
            IInteractable best = null;
            float bestDist = float.MaxValue;
            foreach (var col in Physics2D.OverlapCircleAll(from, radius))
            {
                foreach (var candidate in col.GetComponents<IInteractable>())
                {
                    if (candidate is Behaviour b && !b.isActiveAndEnabled) continue;
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

        void ShowPrompt(string text)
        {
            if (promptText == null || text == shownPrompt) return;
            shownPrompt = text;
            promptText.text = text ?? "";
            promptText.gameObject.SetActive(text != null);
        }
    }
}
