using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Связывает PlayerController с Animator: параметр Speed для ходьбы и сценарные анимации по имени.
    /// Пока у Дэнни нет арта, Animator может отсутствовать — тогда компонент ничего не делает.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Tooltip("Animator Дэнни. Пусто — ищется в детях")]
        [SerializeField] Animator animator;

        static readonly int SpeedParam = Animator.StringToHash("Speed");

        PlayerController controller;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetFloat(SpeedParam, controller.IsMoving ? 1f : 0f);
        }

        /// <summary>Проигрывает состояние Animator с таким же именем ("LookAtScreen", "Fall"…).</summary>
        public void Play(string stateName)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.Play(stateName);
        }
    }
}
