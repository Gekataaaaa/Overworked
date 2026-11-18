using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Кухня Floor_03 — фон художника Kitchen_Background (1920×1080, PPU 100 = кадр комнатной камеры),
    /// как Room_201 на Floor_02: серые заглушки каркаса прячутся, дверь EXIT и пропуск Макса (спрайты P2)
    /// подгоняются под масштаб фона и встают на нарисованные места, под двумя нарисованными трубками — свет.
    /// Компоненты, подписки сценария и остальной Floor_03 не меняются. Повторный запуск безопасен.
    /// Меню: Overworked → P1 → Old Floor_03 → Build Floor_03 Kitchen Background (после Overworked → P2 → Old Floor_03 → Floor_03 Kitchen Art).
    /// </summary>
    public static class Floor03KitchenBackground
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_03.unity";
        const string Art = "Assets/_Project/Art/Environment/Floor03/";
        // Конус света комнатного масштаба (PPU 100) — тот же, что в комнатах Floor_01.
        const string RoomConePath = "Assets/_Project/Art/Environment/Floor01/Room_Light_Cone.png";
        const float RoomPpu = 100f;

        // Свет кухни: тон Floor_03, вчетверо слабее коридора, конус на четверть яркости — тусклая ночная кухня.
        static readonly FloorLamps.Style KitchenLight = new FloorLamps.Style
        {
            Color = FloorLamps.Office.Color, Intensity = FloorLamps.Office.Intensity * 0.25f,
            DarkLevel = FloorLamps.Office.DarkLevel, ConeDim = 0.75f,
        };

        [MenuItem("Overworked/P1/Old Floor_03/Build Floor_03 Kitchen Background")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Спрайты — после OpenScene, иначе сцена выгрузит их и сохранится пустая ссылка.
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "Kitchen_Background.png");
            var cone = AssetDatabase.LoadAssetAtPath<Sprite>(RoomConePath);
            Transform kitchen = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") kitchen = root.transform.Find("Rooms/Kitchen");
            if (kitchen == null || bgSprite == null)
            {
                Debug.LogError("[Setup] Floor_03: нет Environment/Rooms/Kitchen или Kitchen_Background.png");
                return;
            }

            // --- Фон во весь кадр; серые прямоугольники каркаса — выключить.
            var bg = Child(kitchen, "Background");
            foreach (Transform t in bg) if (t.name != "BackgroundArt") t.gameObject.SetActive(false);
            var art = Child(bg, "BackgroundArt");
            art.localPosition = Vector3.zero;
            art.localScale = Vector3.one;
            if (!art.TryGetComponent(out SpriteRenderer bgSr)) bgSr = art.gameObject.AddComponent<SpriteRenderer>();
            bgSr.sprite = bgSprite;
            bgSr.color = Color.white;
            bgSr.sortingOrder = -10;

            // Стены — только коллайдеры; рамка камеры — во весь фон (камера кухни стоит неподвижно).
            var walls = kitchen.Find("Walls");
            if (walls != null) foreach (Transform w in walls) if (w.TryGetComponent(out SpriteRenderer wsr)) wsr.enabled = false;
            SetBounds(kitchen.Find("CameraBounds"), 9.6f, 5.4f);

            // --- Дверь EXIT: спрайт P2 в масштабе коридора (PPU 151) — растягиваем до нарисованной двери на фоне.
            var doorPos = R(99, 515, 223, 485);
            var door = kitchen.Find("ExitDoor/Door");
            if (door != null && door.TryGetComponent(out SpriteRenderer doorSr) && doorSr.sprite != null)
            {
                door.localPosition = doorPos;
                door.localScale = Vector3.one * (doorSr.sprite.pixelsPerUnit / RoomPpu);
                doorSr.color = Color.white;
                doorSr.sortingOrder = -8;
                // Коллайдер — в локальных единицах спрайта, масштаб объекта растянет его вместе с дверью.
                if (door.TryGetComponent(out BoxCollider2D col)) { col.size = doorSr.sprite.bounds.size; col.offset = Vector2.zero; }
            }
            else Debug.LogWarning("[Setup] Floor_03: у двери кухни нет спрайта — сначала Overworked → P2 → Old Floor_03 → Floor_03 Kitchen Art");
            var label = kitchen.Find("ExitDoor/Label");
            if (label != null) label.gameObject.SetActive(false);
            var entry = kitchen.Find("EntryPoint");
            if (entry != null) entry.localPosition = new Vector3(doorPos.x, entry.localPosition.y, 0f);

            // --- Пропуск Макса — на столешнице справа от кофемашины (нарратив 1.2: «пропуск у кофеварки»).
            var pass = kitchen.Find("Props/MaxPass");
            var passArt = pass != null ? pass.Find("Sprite") : null;
            if (passArt != null && passArt.TryGetComponent(out SpriteRenderer passSr) && passSr.sprite != null)
            {
                float k = passSr.sprite.pixelsPerUnit / RoomPpu;
                var size = passSr.sprite.rect.size * k;   // размер в пикселях фона
                pass.localPosition = R(1185, 822 - size.y, size.x, size.y);
                passArt.localPosition = Vector3.zero;
                passArt.localScale = Vector3.one * k;
                passSr.sortingOrder = -7;
            }
            else Debug.LogWarning("[Setup] Floor_03: нет Props/MaxPass/Sprite со спрайтом — сначала Overworked → P2 → Old Floor_03 → Floor_03 Kitchen Art");

            // --- Свет: трубки нарисованы на фоне (центры x 822 и 1420, y ≈ 265), добавляем Light2D и мягкие конусы.
            var fixtures = new[]
            {
                new FloorLamps.Fixture(R(822 - 117, 258, 235, 14), R(822 - 510, 272, 1020, 768)),
                new FloorLamps.Fixture(R(1420 - 117, 258, 235, 14), R(1420 - 510, 272, 1020, 768)),
            };
            // Своей трубки не кладём (sprite null) — горит нарисованная; без BackgroundDimmer, как в комнатах.
            FloorLamps.Build(Child(kitchen, "Lights"), null, cone, fixtures, KitchenLight, null);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] Floor_03: кухня — фон, дверь, пропуск и свет");
        }

        // Пиксели фона 1920×1080 (x, y сверху, w, h) → локальный центр в комнате.
        static Vector3 R(float x, float y, float w, float h) =>
            new Vector3((x + w / 2f - 960f) / RoomPpu, (540f - (y + h / 2f)) / RoomPpu, 0f);

        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void SetBounds(Transform bounds, float halfW, float halfH)
        {
            if (bounds == null || !bounds.TryGetComponent(out PolygonCollider2D poly)) return;
            poly.SetPath(0, new[]
            {
                new Vector2(-halfW, -halfH), new Vector2(halfW, -halfH),
                new Vector2(halfW, halfH), new Vector2(-halfW, halfH),
            });
        }
    }
}
