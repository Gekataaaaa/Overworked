using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Карточка задания в правом верхнем углу (под пульсом): заголовок и квадрат-чекбокс.
    /// При выполнении ставит галочку и через пару секунд прячется. Создаёт себя сам (DontDestroyOnLoad)
    /// и рисует свою карточку; P3 может отдать ей место в HUD через поля ниже.
    /// </summary>
    public class ObjectiveUI : SingletonBehaviour<ObjectiveUI>
    {
        [Tooltip("Корень карточки. Пусто — карточка строится из кода")]
        [SerializeField] GameObject card;

        [Tooltip("Текст задания")]
        [SerializeField] TMP_Text titleText;

        [Tooltip("Галочка внутри квадрата")]
        [SerializeField] Graphic checkMark;

        [Tooltip("Сколько секунд видна выполненная карточка")]
        [SerializeField] float hideAfter = 2.5f;

        [Tooltip("Если сразу пришло новое задание — сколько секунд ещё видна галочка прошлого")]
        [SerializeField] float checkVisibleTime = 1.2f;

        bool visible = true;
        bool hasCard;
        ObjectiveData shown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Spawn()
        {
            if (Instance != null) return;
            var go = new GameObject("ObjectiveUI");
            DontDestroyOnLoad(go);
            go.AddComponent<ObjectiveUI>();
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            if (card == null) BuildCard();
            card.SetActive(false);
        }

        void Start()
        {
            // ObjectiveManager создаётся в _Systems до первой сцены, мы — после неё: к Start он уже есть.
            if (ObjectiveManager.Instance == null) return;
            ObjectiveManager.Instance.OnStarted += Show;
            ObjectiveManager.Instance.OnCompleted += Check;
            if (ObjectiveManager.Instance.Current != null) Show(ObjectiveManager.Instance.Current);
        }

        protected override void OnDestroy()
        {
            if (ObjectiveManager.Instance != null)
            {
                ObjectiveManager.Instance.OnStarted -= Show;
                ObjectiveManager.Instance.OnCompleted -= Check;
            }
            base.OnDestroy();
        }

        /// <summary>Спрятать/показать карточку (онбординг HUD, мини-игры, WhiteRoom).</summary>
        public void SetVisible(bool value)
        {
            visible = value;
            card.SetActive(visible && hasCard);
        }

        void Show(ObjectiveData o)
        {
            StopAllCoroutines();
            // На карточке ещё галочка прошлого задания — даём её увидеть, новое покажем чуть позже.
            if (hasCard && checkMark.enabled)
            {
                StartCoroutine(ShowAfterCheck(o));
                return;
            }
            ShowNow(o);
        }

        IEnumerator ShowAfterCheck(ObjectiveData o)
        {
            yield return new WaitForSeconds(checkVisibleTime);
            ShowNow(o);
        }

        void ShowNow(ObjectiveData o)
        {
            shown = o;
            titleText.text = TextFormatter.Format(o.Title);
            checkMark.enabled = false;
            hasCard = true;
            card.SetActive(visible);
        }

        void Check(ObjectiveData o)
        {
            // Закрыли не то задание, что на карточке (шаг сделан заранее) — карточку не трогаем.
            if (!hasCard || !ObjectiveManager.Same(o, shown)) return;
            checkMark.enabled = true;
            StopAllCoroutines();
            StartCoroutine(HideLater());
        }

        IEnumerator HideLater()
        {
            yield return new WaitForSeconds(hideAfter);
            hasCard = false;
            card.SetActive(false);
        }

        void BuildCard()
        {
            var canvas = OverlayUi.CreateCanvas(transform, "ObjectiveCanvas", 40);
            var bg = OverlayUi.Box(canvas.transform, "ObjectivePanel", new Color(0f, 0f, 0f, 0.55f),
                new Vector2(1f, 1f), new Vector2(-30f, -130f), new Vector2(460f, 90f));
            card = bg.gameObject;
            OverlayUi.Label(card.transform, "Header", "OBJECTIVE", 18, new Vector2(0f, 1f),
                new Vector2(70f, -8f), new Vector2(370f, 24f), TextAlignmentOptions.Left).color = new Color(0.6f, 0.65f, 0.6f);
            titleText = OverlayUi.Label(card.transform, "Title", "", 26, new Vector2(0f, 1f),
                new Vector2(70f, -34f), new Vector2(370f, 50f), TextAlignmentOptions.TopLeft);
            // Чекбокс: светлая рамка, тёмная середина, зелёная галочка-квадрат внутри.
            OverlayUi.Box(card.transform, "Box", new Color(0.85f, 0.85f, 0.8f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(30f, 30f));
            OverlayUi.Box(card.transform, "BoxInner", new Color(0.08f, 0.08f, 0.08f), new Vector2(0f, 0.5f), new Vector2(25f, 0f), new Vector2(24f, 24f));
            checkMark = OverlayUi.Box(card.transform, "Check", new Color(0.35f, 0.85f, 0.45f), new Vector2(0f, 0.5f), new Vector2(29f, 0f), new Vector2(16f, 16f));
        }
    }
}
