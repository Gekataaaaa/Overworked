using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Конец Floor_01 после финального скримера (NARRATIVE, сцена 3.3): Дэнни падает, экран постепенно
    /// темнеет под реплики «{name}… / I can't anymore…», Дэнни теряет сознание, звук резко обрывается,
    /// долгая пауза на чёрном → загрузка WhiteRoom. Лежит на префабе H5_FinalScreamer рядом с HallucinationEvent.
    /// </summary>
    [RequireComponent(typeof(HallucinationEvent))]
    public class FinalCollapse : MonoBehaviour
    {
        [Tooltip("Последние реплики Дэнни (Data/Dialogues/F1_Collapse)")]
        [SerializeField] DialogueData collapseLines;

        [Tooltip("За сколько секунд экран темнеет, пока Дэнни лежит (субтитры остаются видны)")]
        [SerializeField] float darkenTime = 4f;

        [Tooltip("Насколько темнеет экран под репликами (1 — полностью)")]
        [Range(0f, 1f)] [SerializeField] float darkenTo = 0.85f;

        [Tooltip("Дэнни теряет сознание: уход в полный чёрный, секунд")]
        [SerializeField] float blackoutTime = 1.2f;

        [Tooltip("Пауза на чёрном после обрыва звука перед WhiteRoom (в нарративе — «продолжительная»), секунд")]
        [SerializeField] float silenceBeforeWhiteRoom = 3f;

        HallucinationEvent hallucination;

        void Awake() => hallucination = GetComponent<HallucinationEvent>();
        void OnEnable() => hallucination.Finished += OnFinished;
        void OnDisable() => hallucination.Finished -= OnFinished;

        void OnFinished() => StartCoroutine(Run());

        IEnumerator Run()
        {
            var player = PlayerController.Instance;
            if (player != null)
            {
                player.SetInputLocked(true);
                player.PlayAnim("Fall");
            }

            // Затемнение ниже HUD (sortingOrder 100), чтобы субтитры последних реплик было видно.
            var canvasGo = new GameObject("CollapseDarkness", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var dark = new GameObject("Dark", typeof(RectTransform)).AddComponent<Image>();
            dark.rectTransform.SetParent(canvasGo.transform, false);
            dark.rectTransform.anchorMin = Vector2.zero;
            dark.rectTransform.anchorMax = Vector2.one;
            dark.rectTransform.offsetMin = dark.rectTransform.offsetMax = Vector2.zero;
            dark.raycastTarget = false;
            dark.color = new Color(0f, 0f, 0f, 0f);

            bool linesDone = collapseLines == null || DialogueManager.Instance == null;
            if (!linesDone) DialogueManager.Instance.Play(collapseLines, () => linesDone = true);

            for (float t = 0f; t < darkenTime || (!linesDone && t < darkenTime + 15f); t += Time.deltaTime)
            {
                dark.color = new Color(0f, 0f, 0f, darkenTo * Mathf.Clamp01(t / darkenTime));
                yield return null;
            }

            // Дэнни теряет сознание.
            var fade = FadeController.Instance;
            if (fade != null)
            {
                bool black = false;
                fade.FadeOut(blackoutTime, () => black = true);
                while (!black) yield return null;
            }
            else dark.color = Color.black;

            // Звук резко обрывается — и пульс тоже.
            if (AudioManager.Instance != null) AudioManager.Instance.StopAll();
            var heart = HeartbeatManager.Instance;
            if (heart != null)
            {
                heart.SetScripted(true);
                heart.FadeBpm(0f, 0.01f); // сразу 0: удары обрываются, а не затихают
            }
            yield return new WaitForSecondsRealtime(silenceBeforeWhiteRoom);

            Debug.Log("[Ending] финальный скример окончен → WhiteRoom");
            if (GameManager.Instance != null && SceneTransitionManager.Instance != null) GameManager.Instance.LoadFloor(0);
            else SceneManager.LoadScene("WhiteRoom");
        }
    }
}
