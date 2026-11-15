using UnityEditor;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// «Дэнни смотрит в экран»: фронтальный кадр Danny_LookAtScreen (PlayerAnimator.lookAtScreenSprite), точка кисти
    /// для фонарика на этом кадре и список диалогов, где Дэнни обращается к игроку (PlayerController.lookAtScreenDialogues):
    /// на его репликах из них он останавливается и поворачивается к игроку, пока они идут.
    /// Так работает и там, где диалог запускают чужие объекты без шага сценария (ящики, запрет возврата, испуг).
    /// Меню: Overworked → P1 → Setup Look At Screen.
    /// </summary>
    public static class LookAtScreenSetup
    {
        const string PlayerPrefab = "Assets/_Project/Prefabs/Player/Player.prefab";
        const string Dialogues = "Assets/_Project/Data/Dialogues/";
        const string FrontPath = "Assets/_Project/Art/Characters/Danny/Danny_LookAtScreen.png";
        // Кадр 142 × 417, рост 404 px — как у боковых кадров (PPU 136). Пивот: по центру и на 205 px выше низа ног,
        // как у боковых кадров (300 × 430, пивот в центре, ноги на y 420) — при повороте Дэнни не «прыгает».
        static readonly Vector2 FrontPivot = new Vector2(70f / 142f, 1f - (416f - 205f) / 417f);
        // Кисть правой руки на фронтальном кадре (у зрителя слева) — для фонарика в руке.
        static readonly Vector2 FrontHand = new Vector2(25f, 305f);

        // Реплики Дэнни к игроку по нарративу.
        // Floor_02: «Was that you?», «Who's {name}?… That's your name», «You hear me?…», «I just want to go home».
        // Floor_01: «Did you hear him, {name}?…», «Stop it… Please…» (H4 в 101), «This isn't funny anymore» (бейдж),
        //           испуг перед Белой комнатой: «{name}… I can't anymore…» (анимации падения у Дэнни пока нет — стоит).
        // WhiteRoom: «{name}… Are you still here?», «This isn't the office anymore, is it?».
        // Не включён WR_06 — Дэнни там лежит в кровати.
        static readonly string[] Names =
        {
            "F2_LookAtScreen", "F2_Drawer201_Name", "F2_Boss02_After", "F2_NoReturn",
            "F1_DannyToPlayer", "F1_H4_After", "F1_Drawer101", "F1_Screamer_Fall", "F1_Collapse",
            "WR_01", "WR_02",
        };

        [MenuItem("Overworked/P1/Setup Look At Screen")]
        public static void Run()
        {
            var front = ImportFront();
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                var pc = root.GetComponent<PlayerController>();
                if (pc == null) { Debug.LogError("[Setup] в префабе Player нет PlayerController"); return; }
                var so = new SerializedObject(pc);
                var list = so.FindProperty("lookAtScreenDialogues");
                list.arraySize = 0;
                foreach (var n in Names)
                {
                    var d = AssetDatabase.LoadAssetAtPath<DialogueData>(Dialogues + n + ".asset");
                    if (d == null) { Debug.LogWarning($"[Setup] нет диалога {n}"); continue; }
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = d;
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                var pa = root.GetComponent<PlayerAnimator>();
                if (pa != null && front != null)
                {
                    var aso = new SerializedObject(pa);
                    aso.FindProperty("lookAtScreenSprite").objectReferenceValue = front;
                    aso.ApplyModifiedPropertiesWithoutUndo();
                }

                // Фонарик в руке: точка кисти и на фронтальном кадре (картинки «только кисть» для него нет).
                var held = root.GetComponent<HeldFlashlight>();
                if (held != null && front != null)
                {
                    var hso = new SerializedObject(held);
                    var points = hso.FindProperty("handPoints");
                    int idx = -1;
                    for (int i = 0; i < points.arraySize; i++)
                        if (points.GetArrayElementAtIndex(i).FindPropertyRelative("Frame").objectReferenceValue == front) idx = i;
                    if (idx < 0) { idx = points.arraySize; points.arraySize++; }
                    var e = points.GetArrayElementAtIndex(idx);
                    e.FindPropertyRelative("Frame").objectReferenceValue = front;
                    e.FindPropertyRelative("Pixel").vector2Value = FrontHand;
                    e.FindPropertyRelative("Hand").objectReferenceValue = null;
                    hso.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
                Debug.Log($"[Setup] Player: взгляд в экран (кадр {(front != null ? "есть" : "нет")}), диалогов: {list.arraySize}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Sprite ImportFront()
        {
            if (!(AssetImporter.GetAtPath(FrontPath) is TextureImporter ti))
            {
                Debug.LogWarning($"[Setup] нет {FrontPath} — во время взгляда будет обычный Idle");
                return null;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 136f;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = FrontPivot;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(FrontPath);
        }
    }
}
