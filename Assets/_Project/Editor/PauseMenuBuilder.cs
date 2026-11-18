using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static Overworked.EditorTools.UiBuild;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Собирает префаб меню паузы (Prefabs/Systems/Resources/PauseMenu) в стиле главного меню:
    /// тёмное затемнение, белая надпись PAUSE, нарисованная от руки в манере логотипа (Art/UI/Logo/Pause_Title),
    /// и пункты Resume / Main Menu / Quit как в главном меню — моноширинный шрифт, «> » у выбранного (ConsoleMenuItem),
    /// без фона, зелёным цветом терминала пролога (выбранный — ярче); звуки — UiSounds, как везде.
    /// Bootstrap кладёт префаб в _Systems при старте игры. На паузе HUD прячется (PauseMenuUI).
    /// Запуск: меню Overworked → P1 → UI → Build Pause Menu (после Build Main Menu — нужны шрифты). Префаб пересоздаётся целиком.
    /// </summary>
    public static class PauseMenuBuilder
    {
        const string PrefabPath = "Assets/_Project/Prefabs/Systems/Resources/PauseMenu.prefab";
        const string MonoFontPath = "Assets/_Project/Fonts/IBMPlexMono SDF.asset";
        const string TitlePath = "Assets/_Project/Art/UI/Logo/Pause_Title.png";
        // Зелёный терминала пролога (MainMenuBuilder): невыбранный — приглушённый, выбранный — яркий.
        static readonly Color ItemNormal = new Color(0.25f, 0.6f, 0.32f);
        static readonly Color ItemSelected = new Color(0.6f, 1f, 0.68f);

        [MenuItem("Overworked/P1/UI/Build Pause Menu")]
        public static void Run()
        {
            var mono = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoFontPath);
            if (mono == null)
            {
                Debug.LogError("[Setup] нет шрифта IBM Plex Mono — сначала Overworked → P1 → UI → Build Main Menu");
                return;
            }
            var titleSprite = ImportTitle();

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
            panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

            // Надпись PAUSE — белая, от руки, как название игры в главном меню.
            if (titleSprite != null)
            {
                var title = UI("Title", panel).gameObject.AddComponent<Image>();
                title.sprite = titleSprite;
                title.preserveAspect = true;
                title.raycastTarget = false;
                Place(title.rectTransform, new Vector2(0, 220), new Vector2(900, 312));
            }
            else
            {
                var title = Tmp("Title", panel, mono, "PAUSE", 110, Color.white, TextAlignmentOptions.Center);
                Place(title.rectTransform, new Vector2(0, 220), new Vector2(1000, 160));
            }

            // Пункты прямо на затемнении, без фона.
            var buttons = UI("Buttons", panel);
            Place(buttons, new Vector2(10, -150), new Vector2(380, 220));
            var vlg = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childControlHeight = vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            var resume = MainMenuBuilder.ConsoleItem("ResumeButton", buttons, mono, "Resume");
            var menu = MainMenuBuilder.ConsoleItem("MainMenuButton", buttons, mono, "Main Menu");
            var quit = MainMenuBuilder.ConsoleItem("QuitButton", buttons, mono, "Quit");
            foreach (var b in new[] { resume, menu, quit })
            {
                var so = new SerializedObject(b.GetComponent<ConsoleMenuItem>());
                so.FindProperty("normalColor").colorValue = ItemNormal;
                so.FindProperty("selectedColor").colorValue = ItemSelected;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var ui = root.AddComponent<PauseMenuUI>();
            Wire(ui, ("panel", panel.gameObject), ("resumeButton", resume), ("menuButton", menu), ("quitButton", quit));

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("[Setup] PauseMenu: префаб готов (стиль главного меню, PAUSE от руки)");
        }

        static Sprite ImportTitle()
        {
            if (!(AssetImporter.GetAtPath(TitlePath) is TextureImporter ti))
            {
                Debug.LogWarning($"[Setup] нет {TitlePath} — надпись PAUSE будет текстом");
                return null;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(TitlePath);
        }
    }
}
