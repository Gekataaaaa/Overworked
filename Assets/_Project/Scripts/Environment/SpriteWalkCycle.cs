using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Цикл ходьбы для спрайта без Animator (двойник Дэнни в финальном скримере H5): пока объект (или его родитель)
    /// движется по X — крутит кадры frames, стоит — держит кадр idleFrame. Кадры ставятся в LateUpdate,
    /// после того как поведение сдвинуло объект в этом кадре; отражение (flipX) задаёт поведение, не мы.
    /// Мерцание: время от времени спрайт на долю секунды пропадает (иногда дважды подряд) — как сбоящая картинка.
    /// Пропадание — пустой sprite, а не enabled/цвет: ими управляет поведение (появление, исчезновение).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteWalkCycle : MonoBehaviour
    {
        [Tooltip("Кадры шага по порядку")]
        [SerializeField] Sprite[] frames;

        [Tooltip("Кадров в секунду")]
        [SerializeField] float fps = 8f;

        [Tooltip("Какой кадр показывать, пока стоит")]
        [SerializeField] int idleFrame;

        [Tooltip("Чей сдвиг считать ходьбой (пусто — родитель, он двигает силуэт)")]
        [SerializeField] Transform mover;

        [Header("Мерцание")]
        [Tooltip("Мерцать (пропадать и появляться)")]
        [SerializeField] bool flicker = true;

        [Tooltip("Пауза между пропаданиями, секунд (от–до)")]
        [SerializeField] Vector2 flickerGap = new Vector2(0.05f, 0.35f);

        [Tooltip("Сколько секунд спрайта нет при пропадании (от–до)")]
        [SerializeField] Vector2 flickerOff = new Vector2(0.06f, 0.22f);

        [Tooltip("Вероятность, что сразу после появления он пропадёт ещё раз (двойное мерцание)")]
        [Range(0f, 1f)] [SerializeField] float doubleFlicker = 0.6f;

        SpriteRenderer sr;
        float lastX, phase;
        float nextFlicker, offUntil;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (mover == null) mover = transform.parent != null ? transform.parent : transform;
            lastX = mover.position.x;
        }

        void LateUpdate()
        {
            if (frames == null || frames.Length == 0) return;
            float x = mover.position.x;
            // Сдвиг больше 0.5 юнита за кадр — это телепорт (появился в новом месте), а не шаг.
            float dx = Mathf.Abs(x - lastX);
            bool walking = dx > 0.0005f && dx < 0.5f;
            lastX = x;
            if (walking) phase += fps * Time.deltaTime;
            int i = walking ? (int)phase % frames.Length : Mathf.Clamp(idleFrame, 0, frames.Length - 1);
            sr.sprite = Flickered() ? null : frames[i];
        }

        // true — сейчас «пропал». Пропадания — по случайному расписанию, иногда сдвоенные.
        bool Flickered()
        {
            if (!flicker) return false;
            float now = Time.time;
            if (now < offUntil) return true;
            if (nextFlicker <= 0f) nextFlicker = now + Random.Range(flickerGap.x, flickerGap.y);
            if (now < nextFlicker) return false;
            offUntil = now + Random.Range(flickerOff.x, flickerOff.y);
            // Двойное: следующее пропадание почти сразу после появления.
            nextFlicker = Random.value < doubleFlicker
                ? offUntil + Random.Range(0.03f, 0.08f)
                : offUntil + Random.Range(flickerGap.x, flickerGap.y);
            return true;
        }
    }
}
