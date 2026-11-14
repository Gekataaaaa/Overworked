using System.Collections;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Чёрный силуэт (Галлюцинации 2, 4 и фигура финального скримера). Сейчас — тёмный прямоугольник.
    /// Появляется (у игрока по ходу взгляда или там, где лежит) плавно или резко; как только попал в кадр —
    /// рассудок падает (SanityOnSeen). Ждёт, пока игрок подойдёт или наведёт фонарик (или идёт к нему сам),
    /// затем резкий момент (TriggersHit: звук + спайк пульса), дёргается и исчезает. Спрайт — дочерний SpriteRenderer.
    /// </summary>
    public class SilhouetteBehaviour : HallucinationVisual
    {
        [Header("Появление")]
        [Tooltip("Ставить силуэт относительно игрока в момент запуска (иначе — там, где лежит объект)")]
        [SerializeField] bool spawnNearPlayer = true;
        [Tooltip("Смещение от игрока: X — вперёд по взгляду, Y — вверх")]
        [SerializeField] Vector2 offsetFromPlayer = new Vector2(8f, 0.2f);
        [Tooltip("Появляется резко (в нарративе «резко»): сразу целиком, и в этот же момент — удар (звук + спайк пульса)")]
        [SerializeField] bool appearAbruptly;
        [Tooltip("Плавное проявление, секунд (если не резко)")]
        [SerializeField] float fadeIn = 0.5f;

        [Header("Реакция")]
        [Tooltip("Исчезает, когда игрок ближе этого расстояния (0 — не реагирует)")]
        [SerializeField] float approachDistance = 2.5f;
        [Tooltip("Исчезает, если на него светит фонарик с расстояния не больше этого (0 — не реагирует)")]
        [SerializeField] float flashlightDistance = 6f;
        [Tooltip("Сколько секунд фонарик должен светить на силуэт")]
        [SerializeField] float flashlightTime = 0.25f;
        [Tooltip("Исчезает сам через столько секунд, если ничего не случилось (0 — ждать бесконечно)")]
        [SerializeField] float maxWait = 15f;

        [Header("Движение")]
        [Tooltip("Идёт к игроку с этой скоростью, юнитов в секунду (0 — стоит; финальный двойник идёт)")]
        [SerializeField] float moveSpeed;
        [Tooltip("Рывки при ходьбе — «неестественные движения» (0 — плавно)")]
        [SerializeField] float moveJerk = 0.6f;

        [Header("Подход «шагами-вспышками»")]
        [Tooltip("Вместо ходьбы — за столько шагов: пропадает, появляется ближе, снова пропадает… " +
                 "и на последнем шаге оказывается в approachDistance от Дэнни (0 — выкл; финальный двойник — 3)")]
        [SerializeField] int blinkSteps;
        [Tooltip("Сколько секунд силуэт виден на каждом месте")]
        [SerializeField] float blinkVisibleTime = 0.6f;
        [Tooltip("Сколько секунд силуэта нет между шагами")]
        [SerializeField] float blinkHiddenTime = 0.2f;
        [Tooltip("Каждое резкое появление ближе — спайк пульса на столько BPM (потолок 140; 0 — без спайка)")]
        [SerializeField] float blinkSpike = 15f;

        [Header("Исчезновение")]
        [Tooltip("Дёрганье перед исчезновением, секунд")]
        [SerializeField] float vanishTime = 0.35f;
        [Tooltip("Сила дёрганья, юниты")]
        [SerializeField] float jitter = 0.25f;
        [Tooltip("Не исчезать в конце: силуэт стоит, пока не кончится вся галлюцинация (двойник перед броском)")]
        [SerializeField] bool keepVisible;

        SpriteRenderer body;
        Color baseColor;

        void Awake()
        {
            body = GetComponentInChildren<SpriteRenderer>(true);
            if (body != null)
            {
                baseColor = body.color;
                body.enabled = false;
            }
        }

        public override IEnumerator Play(HallucinationEvent hallucination)
        {
            if (body == null)
            {
                Debug.LogWarning($"[Hallucinations] у силуэта {name} нет SpriteRenderer");
                yield break;
            }
            var player = PlayerController.Instance;
            if (spawnNearPlayer && player != null)
            {
                var p = player.transform.position;
                transform.position = new Vector3(p.x + offsetFromPlayer.x * player.Facing, p.y + offsetFromPlayer.y, transform.position.z);
            }
            // Силуэт стоит лицом к стене / к игроку: зеркалим по стороне от игрока.
            if (player != null)
                body.flipX = player.transform.position.x < transform.position.x;

            body.enabled = true;
            if (appearAbruptly)
            {
                SetAlpha(1f);
                CheckSeen(hallucination);
                hallucination.Hit();
            }
            else
            {
                for (float t = 0f; t < fadeIn; t += Time.deltaTime)
                {
                    SetAlpha(t / fadeIn);
                    CheckSeen(hallucination);
                    yield return null;
                }
                SetAlpha(1f);
            }

            if (blinkSteps > 0 && player != null)
                yield return BlinkApproach(hallucination, player);

            float waited = 0f, lit = 0f;
            while (blinkSteps <= 0 && (maxWait <= 0f || waited < maxWait))
            {
                CheckSeen(hallucination);
                waited += Time.deltaTime;
                if (player == null) break;
                Vector3 me = transform.position, pp = player.transform.position;
                float distance = Mathf.Abs(me.x - pp.x);
                if (approachDistance > 0f && distance <= approachDistance) break;

                var light = FlashlightController.Instance;
                lit = flashlightDistance > 0f && light != null && light.Illuminates(me, flashlightDistance) ? lit + Time.deltaTime : 0f;
                if (flashlightDistance > 0f && lit >= flashlightTime) break;

                if (moveSpeed > 0f)
                {
                    // Неровный шаг: скорость «спотыкается» по шуму — движения неестественные.
                    float jerk = 1f + moveJerk * (Mathf.PerlinNoise(Time.time * 6f, 0f) * 2f - 1f);
                    me.x = Mathf.MoveTowards(me.x, pp.x, moveSpeed * Mathf.Max(0f, jerk) * Time.deltaTime);
                    transform.position = me;
                }
                yield return null;
            }

            // Дэнни мог так и не увидеть силуэт в кадре (ушёл назад) — рассудок всё равно падает к удару.
            SeenIfNeeded(hallucination);
            HitIfNeeded(hallucination);
            if (keepVisible) yield break;

            var origin = transform.position;
            for (float t = 0f; t < vanishTime; t += Time.deltaTime)
            {
                float k = t / vanishTime;
                transform.position = origin + (Vector3)(Random.insideUnitCircle * jitter);
                SetAlpha(Random.value < 0.3f ? 0f : 1f - k);
                yield return null;
            }
            transform.position = origin;
            Hide();
        }

        public override void Cleanup() => Hide();

        /// <summary>
        /// Подход вспышками: стоит → пропадает → резко появляется ближе (спайк пульса) … за blinkSteps шагов
        /// доходит до approachDistance от Дэнни.
        /// </summary>
        IEnumerator BlinkApproach(HallucinationEvent hallucination, PlayerController player)
        {
            for (int step = 1; step <= blinkSteps; step++)
            {
                for (float t = 0f; t < blinkVisibleTime; t += Time.deltaTime)
                {
                    CheckSeen(hallucination);
                    yield return null;
                }
                body.enabled = false;
                yield return new WaitForSeconds(blinkHiddenTime);

                // Шаг считаем от текущего места Дэнни: ровно столько, чтобы к последнему шагу встать в approachDistance.
                Vector3 me = transform.position, pp = player.transform.position;
                float side = Mathf.Sign(me.x - pp.x);
                if (side == 0f) side = player.Facing;
                float remaining = Mathf.Abs(me.x - pp.x) - approachDistance;
                me.x -= side * remaining / (blinkSteps - step + 1);
                transform.position = me;
                body.flipX = pp.x < me.x;
                body.enabled = true;
                if (blinkSpike > 0f && HeartbeatManager.Instance != null) HeartbeatManager.Instance.Spike(blinkSpike, 2f);
                CheckSeen(hallucination);
            }
            // Короткая пауза на последнем месте, перед следующим визуалом («No…» и бросок).
            for (float t = 0f; t < blinkVisibleTime; t += Time.deltaTime) yield return null;
        }

        /// <summary>Рассудок падает в первый кадр, когда силуэт на экране.</summary>
        void CheckSeen(HallucinationEvent hallucination)
        {
            if (!SanityOnSeen || hallucination.SanityApplied) return;
            var cam = Camera.main;
            if (cam == null) return;
            var v = cam.WorldToViewportPoint(transform.position);
            if (v.z > 0f && v.x >= 0f && v.x <= 1f && v.y >= 0f && v.y <= 1f) hallucination.ApplySanity();
        }

        void Hide()
        {
            if (body == null) return;
            body.enabled = false;
            SetAlpha(1f);
        }

        void SetAlpha(float a)
        {
            var c = baseColor;
            c.a = baseColor.a * Mathf.Clamp01(a);
            body.color = c;
        }

        void OnDrawGizmos()
        {
            // В редакторе силуэт выключен до запуска — показываем, где он стоит.
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(transform.position, new Vector3(0.9f, 2.1f, 0f));
        }
    }
}
