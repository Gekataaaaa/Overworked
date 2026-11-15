using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// «Лагающее» исчезновение фигуры (сидящий человек в Room_101): рывки с замиранием кадра,
    /// пропадание и красно-голубые двойники, как у сбоящего видео; по желанию — кадры анимации
    /// (человек на кресле оборачивается), которые проигрываются один раз. Вешается рядом с PersonInteractable
    /// на объект со SpriteRenderer. Запускается сам, когда PersonInteractable начинает сжимать объект watch
    /// (масштаб пошёл вниз) — так эффект работает без правок в чужом скрипте. Если watch — пустая заглушка,
    /// фигура не уменьшается, а просто пропадает, когда PersonInteractable её выключит.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GlitchVanish : MonoBehaviour
    {
        [Tooltip("Кадры анимации: [0] — пока сидит, при запуске проигрываются один раз. Пусто — без анимации")]
        [SerializeField] Sprite[] frames;

        [Tooltip("Кадров анимации в секунду")]
        [SerializeField] float fps = 8f;

        [Tooltip("Чей масштаб отслеживать для автозапуска (visual у PersonInteractable). Пусто — свой")]
        [SerializeField] Transform watch;

        [Tooltip("Насколько далеко фигура «прыгает» при рывке, юнитов")]
        [SerializeField] float jumpDistance = 0.35f;

        [Tooltip("Сколько секунд держится один «застывший кадр» (от и до)")]
        [SerializeField] Vector2 holdTime = new Vector2(0.03f, 0.12f);

        [Tooltip("Доля рывков, на которых фигура пропадает целиком")]
        [Range(0f, 1f)] [SerializeField] float dropChance = 0.25f;

        [Tooltip("Смещение цветных двойников, юнитов")]
        [SerializeField] float ghostOffset = 0.12f;

        [Tooltip("Запустить эффект сразу при включении объекта (для проверки в Sandbox)")]
        [SerializeField] bool playOnEnable;

        SpriteRenderer body;
        SpriteRenderer[] ghosts;
        Vector3 basePos, watchScale;
        Color baseColor;
        bool playing;
        float nextJump, startTime;

        /// <summary>Идёт ли эффект.</summary>
        public bool Playing => playing;

        /// <summary>Длительность анимации кадров, секунд (0 — кадров нет).</summary>
        public float AnimationLength => frames != null && frames.Length > 0 ? frames.Length / Mathf.Max(0.01f, fps) : 0f;

        void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            if (watch == null) watch = transform;
            basePos = transform.localPosition;
            watchScale = watch.localScale;
            baseColor = body.color;
            if (frames != null && frames.Length > 0) body.sprite = frames[0];
            ghosts = new[] { MakeGhost("GhostRed", new Color(1f, 0.15f, 0.15f, 0.55f)),
                             MakeGhost("GhostCyan", new Color(0.15f, 0.9f, 1f, 0.55f)) };
        }

        void OnEnable()
        {
            if (playOnEnable) Play();
        }

        void OnDisable() => Restore();

        /// <summary>Запустить «лаг» вручную (обычно не нужно — стартует сам вместе со сжатием).</summary>
        public void Play()
        {
            if (playing) return;
            playing = true;
            startTime = Time.time;
            nextJump = 0f;
        }

        void LateUpdate()
        {
            // PersonInteractable сжимает watch: первый же шаг вниз по масштабу — сигнал начать.
            if (!playing && watch.localScale.x < watchScale.x * 0.999f) Play();
            if (!playing) return;

            // Кадры анимации идут ровно, один раз; последний держится до исчезновения.
            if (frames != null && frames.Length > 0)
            {
                int i = Mathf.Min(frames.Length - 1, (int)((Time.time - startTime) * fps));
                body.sprite = frames[i];
                foreach (var g in ghosts) g.sprite = frames[i];
            }

            if (Time.time < nextJump) return;
            nextJump = Time.time + Random.Range(holdTime.x, holdTime.y);

            // Новый «застывший кадр»: фигура держится в случайном месте до следующего рывка —
            // так выглядит пропуск кадров, а не плавная дрожь.
            bool drop = Random.value < dropChance;
            body.enabled = !drop;
            var jump = new Vector3(Random.Range(-jumpDistance, jumpDistance), Random.Range(-0.05f, 0.05f), 0f);
            transform.localPosition = basePos + (Random.value < 0.35f ? Vector3.zero : jump);
            body.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Random.Range(0.55f, 1f));

            for (int i = 0; i < ghosts.Length; i++)
            {
                bool show = !drop && Random.value < 0.7f;
                ghosts[i].enabled = show;
                if (!show) continue;
                ghosts[i].sprite = body.sprite;
                ghosts[i].flipX = body.flipX;
                float side = i == 0 ? -1f : 1f;
                ghosts[i].transform.localPosition = new Vector3(side * ghostOffset * Random.Range(0.5f, 2f), 0f, 0f);
            }
        }

        SpriteRenderer MakeGhost(string ghostName, Color color)
        {
            var go = new GameObject(ghostName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = body.sprite;
            sr.color = color;
            sr.sharedMaterial = body.sharedMaterial;
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder - 1;
            sr.enabled = false;
            return sr;
        }

        void Restore()
        {
            playing = false;
            if (body == null) return;
            transform.localPosition = basePos;
            body.enabled = true;
            body.color = baseColor;
            if (frames != null && frames.Length > 0) body.sprite = frames[0];
            if (ghosts != null) foreach (var g in ghosts) if (g != null) g.enabled = false;
        }
    }
}
