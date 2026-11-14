using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Управление Дэнни: только влево/вправо, без прыжка и бега.
    /// Лежит на объекте Player в каждой сцене этажа (префаб Prefabs/Player/Player).
    /// Двигает Rigidbody2D по X, стены коридора останавливают его коллайдерами.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : SingletonBehaviour<PlayerController>
    {
        [Tooltip("Скорость ходьбы, юнитов в секунду")]
        [SerializeField] float speed = 1.5f;

        [Tooltip("Спрайт Дэнни: отражается по X при смене направления. Пусто — ищется в детях")]
        [SerializeField] SpriteRenderer sprite;

        /// <summary>Текущий режим управления.</summary>
        public PlayerState State { get; private set; } = PlayerState.Normal;

        /// <summary>Куда смотрит Дэнни: 1 — вправо, -1 — влево.</summary>
        public int Facing { get; private set; } = 1;

        /// <summary>Идёт ли Дэнни сейчас (для анимации шагов и звука).</summary>
        public bool IsMoving { get; private set; }

        Rigidbody2D body;
        PlayerAnimator anim;
        bool onlyRight;
        float driftUntil;
        int lastDir = 1;

        // Этаж спрятан под главным меню (пауза → Main Menu) — игрока «нет», пока не вернёмся по Continue.
        void OnEnable() => ReleaseInstanceWhileDisabled(true);
        void OnDisable() => ReleaseInstanceWhileDisabled(false);

        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody2D>();
            // Дэнни ходит по полу, физика ему нужна только для упора в стены.
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
            anim = GetComponent<PlayerAnimator>();
        }

        void FixedUpdate()
        {
            float dir = State == PlayerState.Normal ? ReadInput() : 0f;
            body.linearVelocity = new Vector2(dir * speed, 0f);

            IsMoving = Mathf.Abs(dir) > 0.01f;
            if (IsMoving)
            {
                Facing = dir > 0 ? 1 : -1;
                lastDir = Facing;
                if (sprite != null) sprite.flipX = Facing < 0;
            }
        }

        float ReadInput()
        {
            float x = GameInput.Move != null ? GameInput.Move.ReadValue<float>() : 0f;
            if (onlyRight) x = Mathf.Max(0f, x);
            // Сбой ввода: пока окно открыто, отпущенная клавиша не останавливает Дэнни.
            if (Mathf.Abs(x) < 0.01f && Time.time < driftUntil) x = lastDir;
            return Mathf.Clamp(x, -1f, 1f);
        }

        /// <summary>
        /// Блокирует/возвращает управление (телефон, мини-игры, сцены).
        /// Не счётчик: последний вызов побеждает, поэтому разблокирует тот, кто блокировал.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            State = locked ? PlayerState.Locked : PlayerState.Normal;
            if (locked && body != null) body.linearVelocity = Vector2.zero;
            Debug.Log($"[Player] SetInputLocked({locked})");
        }

        /// <summary>WhiteRoom: можно идти только вправо.</summary>
        public void SetOnlyRight(bool value) => onlyRight = value;

        /// <summary>
        /// Сбой ввода на Floor_02: следующие seconds секунд Дэнни продолжает идти
        /// в последнюю сторону после отпускания клавиши.
        /// </summary>
        public void StartInputDrift(float seconds)
        {
            driftUntil = Time.time + seconds;
            Debug.Log($"[Player] StartInputDrift({seconds})");
        }

        /// <summary>Мгновенно ставит игрока в точку (вход/выход из комнаты, под затемнением).</summary>
        public void Teleport(Transform point)
        {
            Teleport(point.position);
            Debug.Log($"[Player] Teleport → {point.name}");
        }

        /// <summary>Мгновенно ставит игрока в позицию (Continue возвращает туда, где вышли в меню).</summary>
        public void Teleport(Vector3 position)
        {
            // Двигаем и Transform, и Rigidbody: иначе интерполяция на кадр вернёт старую позицию.
            transform.position = position;
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
        }

        /// <summary>Анимация по имени: "LookAtScreen", "Fall", "LieDown", "StandUp".</summary>
        public void PlayAnim(string animName)
        {
            Debug.Log($"[Player] PlayAnim({animName})");
            if (anim != null) anim.Play(animName);
        }
    }
}
