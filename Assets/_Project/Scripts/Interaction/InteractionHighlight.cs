using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Overworked
{
    /// <summary>
    /// Показывает, с чем сейчас можно взаимодействовать: подсказка «[E] …» в мире над объектом
    /// и жёлтый контур вокруг его спрайтов. Работает для любого IInteractable без настройки объектов.
    /// Живёт рядом с InteractionSystem (добавляется им самим) и рисует то, что у него в Focused.
    /// </summary>
    public class InteractionHighlight : MonoBehaviour
    {
        [Tooltip("Отступ подсказки над верхом объекта, юнитов")]
        [SerializeField] float promptOffset = 0.45f;

        [Tooltip("Размер шрифта подсказки (юниты мира, как у подписей дверей)")]
        [SerializeField] float fontSize = 3f;

        [Tooltip("Цвет контура")]
        [SerializeField] Color outlineColor = new Color(1f, 0.85f, 0.2f);

        [Tooltip("Толщина контура, юнитов: одинаковая у узкой двери и широкого стола")]
        [SerializeField] float outlineThickness = 0.07f;

        [Tooltip("Порядок отрисовки подсказки: поверх Дэнни и предметов")]
        [SerializeField] int promptSortingOrder = 100;

        [Tooltip("Цвет подсказки [E] — как у текста терминала в прологе")]
        [SerializeField] Color promptColor = new Color(0.36f, 0.85f, 0.45f);

        const string OutlineName = "~FocusOutline";

        TextMeshPro prompt;
        Component target;
        readonly List<GameObject> outlines = new List<GameObject>();
        static Material unlitMaterial;

        void Awake()
        {
            var go = new GameObject("WorldPrompt");
            go.transform.SetParent(transform, false);
            prompt = go.AddComponent<TextMeshPro>();
            prompt.fontSize = fontSize;
            prompt.alignment = TextAlignmentOptions.Bottom;
            prompt.color = promptColor;
            prompt.textWrappingMode = TextWrappingModes.NoWrap;
            prompt.rectTransform.sizeDelta = new Vector2(8f, 1f);
            prompt.rectTransform.pivot = new Vector2(0.5f, 0f);
            prompt.sortingOrder = promptSortingOrder;
            // Тёмная обводка букв — читается и на светлой стене, и в темноте Floor_02.
            // fontMaterial — копия материала только для этой надписи, общий материал шрифта не меняется.
            var mat = prompt.fontMaterial;
            mat.EnableKeyword("OUTLINE_ON");
            prompt.outlineWidth = 0.22f;
            prompt.outlineColor = new Color32(15, 15, 15, 255);
            go.SetActive(false);
        }

        /// <summary>Сменить объект в фокусе (null — ничего не показывать).</summary>
        public void SetTarget(IInteractable interactable)
        {
            var next = interactable as Component;
            if (next == target) return;
            ClearOutline();
            target = next;
            if (target != null) BuildOutline(Visual(target));
            prompt.gameObject.SetActive(target != null);
        }

        void LateUpdate()
        {
            // Объект выключили или уничтожили (подобранный пропуск) — гасим всё.
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                if (prompt.gameObject.activeSelf || outlines.Count > 0) SetTarget(null);
                return;
            }
            var text = $"[E] {((IInteractable)target).Prompt}";
            if (prompt.text != text) prompt.text = text;
            // Каждый кадр: объект может двигаться, сжиматься (бормочущий человек).
            var b = Bounds(Visual(target));
            prompt.transform.position = new Vector3(b.center.x, b.max.y + promptOffset, 0f);
        }

        void OnDisable() => SetTarget(null);

        // Что показывать: сам объект или настоящий объект за невидимой заглушкой (HighlightTarget).
        static GameObject Visual(Component c) =>
            c.TryGetComponent(out HighlightTarget ht) && ht.target != null ? ht.target : c.gameObject;

        // Верх объекта: коллайдер (он есть у всех IInteractable), иначе спрайты.
        static Bounds Bounds(GameObject go)
        {
            if (go.TryGetComponent(out Collider2D col) && col.enabled) return col.bounds;
            var renderers = go.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        // Контур: под каждым спрайтом объекта — его жёлтая копия чуть крупнее (на outlineThickness с каждой стороны).
        void BuildOutline(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<SpriteRenderer>();
            // Все копии — под самым нижним спрайтом объекта: у составных (лестница со ступеньками)
            // жёлтое видно только по внешнему краю, а не поверх деталей.
            int order = int.MaxValue;
            foreach (var sr in renderers) order = Mathf.Min(order, sr.sortingOrder);
            foreach (var sr in renderers)
            {
                if (sr.sprite == null || sr.name == OutlineName || !sr.enabled) continue;
                var size = sr.bounds.size;
                if (size.x <= 0f || size.y <= 0f) continue;

                var copy = new GameObject(OutlineName);
                copy.transform.SetParent(sr.transform, false);
                // Масштаб в долях родителя: (ширина + 2 толщины) / ширина — контур одинаковой толщины.
                copy.transform.localScale = new Vector3((size.x + 2f * outlineThickness) / size.x,
                                                        (size.y + 2f * outlineThickness) / size.y, 1f);
                var o = copy.AddComponent<SpriteRenderer>();
                o.sprite = sr.sprite;
                o.flipX = sr.flipX;
                o.flipY = sr.flipY;
                o.color = outlineColor;
                o.sortingLayerID = sr.sortingLayerID;
                o.sortingOrder = order - 1;
                // Без освещения: в тёмных коридорах жёлтый не должен тускнеть вместе со стеной.
                var unlit = UnlitMaterial();
                if (unlit != null) o.sharedMaterial = unlit;
                outlines.Add(copy);
            }
        }

        void ClearOutline()
        {
            foreach (var o in outlines) if (o != null) Destroy(o);
            outlines.Clear();
        }

        static Material UnlitMaterial()
        {
            if (unlitMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null) unlitMaterial = new Material(shader) { name = "FocusOutline (Unlit)" };
            }
            return unlitMaterial;
        }
    }
}
