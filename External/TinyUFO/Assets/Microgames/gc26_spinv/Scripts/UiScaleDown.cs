using UnityEngine;

namespace GC26.Spinv
{
    // Экран во весь рост (карта, победа) свёрстан под высоту телефона 2778.
    // Канвас Full HD ниже в factor раз, поэтому панель растягиваем в factor раз и уменьшаем обратно:
    // она по-прежнему закрывает весь экран, а содержимое помещается по высоте
    [RequireComponent(typeof(RectTransform))]
    public class UiScaleDown : MonoBehaviour
    {
        public float factor = 1.5f;

        void OnEnable() => Apply();
        void LateUpdate() => Apply();

        void Apply()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            if (parent == null || factor <= 0f) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = parent.rect.size * factor;
            rect.localScale = Vector3.one / factor;
        }
    }
}
