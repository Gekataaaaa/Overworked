using UnityEngine;
using UnityEngine.UI;

namespace Overworked
{
    /// <summary>
    /// Гасит HUD (рассудок, пульс, кнопка телефона) вместе с белым слоем смерти в WhiteRoom:
    /// чем белее экран, тем прозрачнее HUD; на полностью белом экране и титрах HUD не видно.
    /// Кладётся отдельным объектом в сцену WhiteRoom (меню Overworked → P3 → Add HUD Fade To White Room),
    /// overlay — белый слой WhiteRoomSequence (P4). Код P4 не меняет. Если ссылка пустая (WhiteRoomBuilder пересоздал
    /// WhiteRoomUI, и она слетела), слой ищется по имени WhiteRoomUI/WhiteOverlay.
    /// </summary>
    public class HudFadeWithOverlay : MonoBehaviour
    {
        [Tooltip("Белый слой, за которым следит HUD (WhiteRoomUI/White у P4)")]
        [SerializeField] Graphic overlay;

        [Tooltip("HUD полностью исчезает, когда слой непрозрачен на столько (0..1) — немного раньше полного белого")]
        [Range(0.1f, 1f)]
        [SerializeField] float goneAt = 0.8f;

        const string FallbackPath = "WhiteRoomUI/WhiteOverlay";

        void Awake()
        {
            if (overlay == null) FindOverlay();
        }

        void FindOverlay()
        {
            var go = GameObject.Find(FallbackPath);
            if (go != null) overlay = go.GetComponent<Graphic>();
            if (overlay == null) Debug.LogWarning($"[UI] HudFadeWithOverlay: нет белого слоя ({FallbackPath}) — HUD не будет гаснуть");
        }

        void Update()
        {
            if (overlay == null || UIManager.Instance == null) return;
            float white = overlay.color.a;
            UIManager.Instance.SetHudAlpha(1f - Mathf.Clamp01(white / goneAt));
        }

        void OnDisable()
        {
            // Уходим со сцены или объект выключен — HUD не должен остаться полупрозрачным.
            if (UIManager.Instance != null) UIManager.Instance.SetHudAlpha(1f);
        }
    }
}
