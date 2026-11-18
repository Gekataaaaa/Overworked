using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Белая комната: фон BG_WhiteRoom вместо пустоты, спрайт больничной кровати вместо прямоугольников-заглушек
    /// и кадры «Дэнни ложится в кровать» (PlayerAnimator.lieDownFrames в префабе Player).
    /// Кровать нарисована и в каждом кадре укладывания — на том же месте, что спрайт кровати в сцене (совпадает
    /// пиксель в пиксель, тот же тон и материал, что у Дэнни), поэтому подмена незаметна и сцену прятать не нужно.
    /// Каркас сцены (стены, триггеры, BedPoint) делает WhiteRoomBuilder от P4; здесь только арт и точка укладывания.
    /// Дэнни в этой сцене светлее, чем на этажах (тон RoomTint).
    /// Меню: Overworked → P1 → Art → Dress White Room (после пересборки сцены билдером P4).
    /// </summary>
    public static class WhiteRoomArt
    {
        const string ScenePath = "Assets/_Project/Scenes/WhiteRoom.unity";
        const string PlayerPrefab = "Assets/_Project/Prefabs/Player/Player.prefab";
        const string Dir = "Assets/_Project/Art/Characters/Danny/LieDown/";
        const string UnlitMaterial = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        const int FrameCount = 8;
        const float Ppu = 136f;   // как у кадров Дэнни: рост в кадрах укладывания тот же (404 px)

        // Кадр укладывания 461 × 408. Пивот — как у боковых кадров Дэнни: по центру головы (x 47) и на 205 px выше
        // ног (ноги на y 405 сверху) — на первом кадре Дэнни стоит ровно там же, где стоял, без скачка.
        static readonly Vector2 FramePivot = new Vector2(47f / 461f, (408f - 200f) / 408f);
        // Кровать в кадре: x 58..457, y 174..382 (сверху). Центр кровати относительно пивота кадра, в юнитах.
        static readonly Vector2 BedFromPivot = new Vector2((257.5f - 47f) / Ppu, (200f - 278f) / Ppu);

        // Фон 1920 × 1080: линия пола на y 1000. Линию ставим на FloorY сцены, верх — над кадром камеры.
        const float BgLine = 1000f / 1080f;
        const float BgTop = 5.7f;
        // Ниже картинки — фон камеры цветом пола картинки (пол однотонный).
        static readonly Color FloorColor = new Color32(239, 240, 238, 255);
        // На этажах Дэнни приглушён (тон 0.62 в префабе), в светлой палате он выглядел серым пятном —
        // здесь тон светлее (переопределение только в этой сцене). Кровать — в том же тоне, что и кадры укладывания.
        static readonly Color RoomTint = new Color(0.95f, 0.96f, 0.93f, 1f);

        [MenuItem("Overworked/P1/Art/Dress White Room")]
        public static void Run()
        {
            var frames = ImportFrames();
            var bed = ImportSprite(Dir + "Hospital_Bed_Trimmed.png", Ppu);
            var bg = ImportSprite(Dir + "BG_WhiteRoom.png", 100f);
            if (frames == null || bed == null || bg == null) return;

            // Префаб: кадры укладывания; заодно берём смещение, тон и материал спрайта Дэнни.
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            Vector3 spriteOffset;
            Color bodyColor;
            Material bodyMaterial;
            try
            {
                var pa = root.GetComponent<PlayerAnimator>();
                var anim = root.GetComponentInChildren<Animator>();
                var body = anim != null ? anim.GetComponent<SpriteRenderer>() : null;
                if (pa == null || body == null) { Debug.LogError("[Setup] в префабе Player нет PlayerAnimator/спрайта"); return; }
                spriteOffset = body.transform.localPosition;
                bodyColor = body.color;
                bodyMaterial = body.sharedMaterial;
                var so = new SerializedObject(pa);
                var list = so.FindProperty("lieDownFrames");
                list.arraySize = frames.Length;
                for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform env = null, player = null;
            Camera cam = null;
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == "Environment") env = go.transform;
                if (go.GetComponent<PlayerController>() != null) player = go.transform;
                if (cam == null) cam = go.GetComponentInChildren<Camera>(true);
            }
            var bedRoot = env != null ? env.Find("Bed") : null;
            if (bedRoot == null || player == null)
            {
                Debug.LogError("[Setup] WhiteRoom: нет Environment/Bed или Player — сначала соберите сцену билдером P4");
                return;
            }

            // Дэнни светлее: тело и всё, что красится в его тон (кисть, фонарик), — переопределением в сцене.
            foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!Approximately(sr.color, bodyColor)) continue;
                sr.color = RoomTint;
                PrefabUtility.RecordPrefabInstancePropertyModifications(sr);
            }
            bodyColor = RoomTint;

            // Кровать: спрайт на месте рамы-заглушки (её ссылается триггер проявления — объект оставляем),
            // остальные прямоугольники пустые. Позиция — там, где кровать окажется в кадрах укладывания.
            float playerY = player.position.y;
            var frame = bedRoot.Find("Frame").GetComponent<SpriteRenderer>();
            frame.sprite = bed;
            frame.color = bodyColor;
            frame.sharedMaterial = bodyMaterial;
            frame.transform.localScale = Vector3.one;
            frame.transform.position = new Vector3(bedRoot.position.x, playerY + spriteOffset.y + BedFromPivot.y, 0f);
            foreach (var n in new[] { "Sheet", "Pillow", "Headboard" })
            {
                var part = bedRoot.Find(n);
                if (part != null) part.GetComponent<SpriteRenderer>().sprite = null;
            }
            // Дэнни ложится с того места, где стоит у кровати: высота — как у игрока (а не на 0.4 выше).
            var bedPoint = bedRoot.Find("BedPoint");
            if (bedPoint != null)
                bedPoint.position = new Vector3(bedRoot.position.x - spriteOffset.x - BedFromPivot.x, playerY, 0f);

            // Фон: растянут на всю комнату по X, линия пола картинки — на линии пола сцены.
            var floorLine = env.Find("FloorLine");
            float floorY = floorLine != null ? floorLine.position.y : -2.5f;
            if (floorLine != null) floorLine.GetComponent<SpriteRenderer>().enabled = false;   // линия есть на фоне
            var old = env.Find("Background");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var bgr = new GameObject("Background").AddComponent<SpriteRenderer>();
            bgr.transform.SetParent(env, false);
            bgr.sprite = bg;
            bgr.sortingOrder = -100;
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(UnlitMaterial);
            if (unlit != null) bgr.sharedMaterial = unlit;
            float left = -12f, right = 38f;
            var poly = env.GetComponentInChildren<PolygonCollider2D>();
            if (poly != null) { left = poly.bounds.min.x - 1f; right = poly.bounds.max.x + 1f; }
            float h = (BgTop - floorY) / BgLine;
            var size = bg.bounds.size;
            bgr.transform.position = new Vector3((left + right) / 2f, BgTop - h / 2f, 0f);
            bgr.transform.localScale = new Vector3((right - left) / size.x, h / size.y, 1f);
            if (cam != null) cam.backgroundColor = FloorColor;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Setup] WhiteRoom: фон, кровать, кадры укладывания ({frames.Length}); BedPoint {bedPoint?.position}");
        }

        static bool Approximately(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

        static Sprite[] ImportFrames()
        {
            var result = new Sprite[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                string path = $"{Dir}Danny_LieDown_{i}.png";
                if (!(AssetImporter.GetAtPath(path) is TextureImporter ti))
                {
                    Debug.LogError($"[Setup] нет {path}");
                    return null;
                }
                Setup(ti, Ppu, FramePivot);
                result[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
            return result;
        }

        static Sprite ImportSprite(string path, float ppu)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter ti))
            {
                Debug.LogError($"[Setup] нет {path}");
                return null;
            }
            Setup(ti, ppu, new Vector2(0.5f, 0.5f));
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void Setup(TextureImporter ti, float ppu, Vector2 pivot)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = pivot;
            ti.SetTextureSettings(st);
            ti.SaveAndReimport();
        }
    }
}
