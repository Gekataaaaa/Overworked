using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Overworked.EditorTools.UiBuild;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Собирает префаб меню паузы (Prefabs/Systems/Resources/PauseMenu): затемнение, PAUSED,
    /// кнопки Resume / Main Menu / Quit. Bootstrap кладёт его в _Systems при старте игры.
    /// Запуск: меню Overworked → P1 → Build Pause Menu (после Build Main Menu — нужен шрифт PT Sans).
    /// Префаб пересоздаётся целиком.
    /// </summary>
    public static class PauseMenuBuilder
    {
        const string PrefabPath = "Assets/_Project/Prefabs/Systems/Resources/PauseMenu.prefab";
        const string FontPath = "Assets/_Project/Fonts/PTSans SDF.asset";

        [MenuItem("Overworked/P1/Build Pause Menu")]
        public static void Run()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Debug.LogError("[Setup] нет шрифта PT Sans — сначала Overworked → P1 → Build Main Menu");
                return;
            }

            var root = new GameObject("PauseMenu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Выше HUD (100): пауза перекрывает всё, кроме перехода — во время него она не открывается.
            canvas.sortingOrder = 150;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = Stretch(UI("Panel", root.transform));
            panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            var title = Tmp("Title", panel, font, "PAUSED", 96, MenuText, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0, 200), new Vector2(1000, 140));
            title.characterSpacing = 10;

            var buttons = UI("Buttons", panel);
            Place(buttons, new Vector2(0, -90), new Vector2(360, 280));
            var vlg = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            var resume = MenuButton("ResumeButton", buttons, font, "Resume");
            var menu = MenuButton("MainMenuButton", buttons, font, "Main Menu");
            var quit = MenuButton("QuitButton", buttons, font, "Quit");

            var ui = root.AddComponent<PauseMenuUI>();
            Wire(ui, ("panel", panel.gameObject), ("resumeButton", resume), ("menuButton", menu), ("quitButton", quit));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[Setup] PauseMenu: префаб готов");
        }
    }
}
