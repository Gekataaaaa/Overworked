using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Силуэты галлюцинаций H2 (Floor_02, середина коридора) и H4 (префаб; на Floor_02 — копия H4_Silhouette201
    /// у входа в Room_201): вместо тёмного прямоугольника — спрайт Silhouette_Wall в рост Дэнни, ногами на его уровне пола.
    /// Поведение (появление, рассудок, исчезновение от фонарика или при подходе) — SilhouetteBehaviour без изменений.
    /// Меню: Overworked → P1 → Art → Dress H2 Silhouette (повторить, если префабы пересобраны через
    /// Overworked → P4 → Hallucinations → Build Hallucinations или копия в 201 — через P2 → Move H4 Silhouette To Room 201).
    /// </summary>
    public static class SilhouetteArt
    {
        static readonly string[] Prefabs =
        {
            "Assets/_Project/Prefabs/Hallucinations/H2_Silhouette.prefab",
            "Assets/_Project/Prefabs/Hallucinations/H4_Room101.prefab",
        };
        const string Floor02 = "Assets/_Project/Scenes/Floor_02.unity";
        const string SpritePath = "Assets/_Project/Art/Characters/Silhouette/Silhouette_Wall.png";
        // Силуэт 152 × 460, фигура 457 px. PPU 155 — рост ~2.95 юнита, как у Дэнни (404 px при PPU 136).
        const float Ppu = 155f;
        // Корень силуэта стоит на высоте игрока + offsetFromPlayer.y (0.05), а ноги Дэнни — на 1.25 ниже позиции
        // игрока (спрайт +0.256, ноги в 205 px под пивотом кадра). Пивот спрайта — внизу, поэтому опускаем на 1.30.
        const float FeetY = -1.30f;

        [MenuItem("Overworked/P1/Art/Dress H2 Silhouette")]
        public static void Run()
        {
            var sprite = ImportSprite();
            if (sprite == null) return;
            int done = 0;

            foreach (var path in Prefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = DressAll(root, sprite);
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    done += n;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            // Копия H4 у Room_201 на Floor_02 — обычный объект сцены (не экземпляр префаба), правим в сцене.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(Floor02, OpenSceneMode.Single);
            int inScene = 0;
            foreach (var go in scene.GetRootGameObjects())
                inScene += DressAll(go, sprite);
            if (inScene > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log($"[Setup] Силуэты: спрайт {sprite.name} (рост {sprite.bounds.size.y:0.00}) — в префабах {done}, на Floor_02 {inScene}");
        }

        // Все силуэты под root, кроме частей экземпляров префабов (их меняет сам префаб).
        static int DressAll(GameObject root, Sprite sprite)
        {
            int n = 0;
            foreach (var s in root.GetComponentsInChildren<SilhouetteBehaviour>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(s.gameObject)) continue;
                var body = s.GetComponentInChildren<SpriteRenderer>(true);
                if (body == null) continue;
                body.sprite = sprite;
                body.transform.localScale = Vector3.one;
                body.transform.localPosition = new Vector3(0f, FeetY, 0f);
                EditorUtility.SetDirty(body);
                EditorUtility.SetDirty(body.transform);
                n++;
            }
            return n;
        }

        static Sprite ImportSprite()
        {
            if (!(AssetImporter.GetAtPath(SpritePath) is TextureImporter ti))
            {
                Debug.LogError($"[Setup] нет {SpritePath}");
                return null;
            }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = Ppu;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
    }
}
