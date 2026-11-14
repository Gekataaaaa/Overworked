using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Room_101 на Floor_01, нарратив сцена 3.1: «в углу комнаты на стуле сидит человек и бессвязно бормочет.
    /// При приближении — подсказка E — Interact. После E фигура сжимается в точку и исчезает»,
    /// затем DANNY «He wasn't here…» (F1_PersonGone, ассет P3). Стул остаётся — исчезает только фигура.
    /// Пока без арта: стул и тёмная сидящая фигура из прямоугольников в левом углу комнаты.
    /// Меню: Overworked → P2 → Place Floor_01 Mumbling Man.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor01Person
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_01.unity";
        const string RequestFile = "Temp/p2_floor01_person.request";
        const string GonePath = "Assets/_Project/Data/Dialogues/F1_PersonGone.asset";

        // Левый угол комнаты (стена — на ±8.5 у каркаса), подальше от двери выхода по центру и тумбы справа.
        const float CornerX = -6.8f;

        static Floor01Person()
        {
            if (File.Exists(RequestFile)) EditorApplication.delayCall += RunRequest;
        }

        static void RunRequest()
        {
            // В Play Mode сцену не сохранить — ждём выхода в редактор.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= OnPlayMode;
                EditorApplication.playModeStateChanged += OnPlayMode;
                return;
            }
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            Place();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Place Floor_01 Mumbling Man")]
        public static void Place()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            // Ассет — после OpenScene (иначе сцена выгрузит его и сохранится пустая ссылка).
            var gone = AssetDatabase.LoadAssetAtPath<DialogueData>(GonePath);
            if (gone == null) Debug.LogWarning("[Setup] нет F1_PersonGone (P3) — без реплики после исчезновения");

            Transform room = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Environment") room = root.transform.Find("Rooms/Room_101");
            if (room == null)
            {
                Debug.LogError("[Setup] Floor_01: нет Environment/Rooms/Room_101");
                return;
            }
            var props = room.Find("Props");
            if (props == null)
            {
                props = new GameObject("Props").transform;
                props.SetParent(room, false);
            }
            foreach (var name in new[] { "Chair", "MumblingMan" })
            {
                var old = props.Find(name);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var entry = room.Find("EntryPoint");
            float floorY = entry != null ? entry.localPosition.y - 0.9f : -4.2f;
            var square = SB.EnsureSquareSprite();

            // Стул: сиденье на ножках и спинка — остаётся на месте, когда человек исчезнет.
            var chair = new GameObject("Chair");
            chair.transform.SetParent(props, false);
            chair.transform.localPosition = new Vector3(CornerX, floorY, 0f);
            var chairColor = new Color(0.3f, 0.26f, 0.22f);
            SB.Rect(chair, "Seat", square, new Vector2(0f, 0.55f), new Vector2(0.9f, 0.12f), chairColor, -8);
            SB.Rect(chair, "LegL", square, new Vector2(-0.38f, 0.25f), new Vector2(0.08f, 0.5f), chairColor, -8);
            SB.Rect(chair, "LegR", square, new Vector2(0.38f, 0.25f), new Vector2(0.08f, 0.5f), chairColor, -8);
            SB.Rect(chair, "Back", square, new Vector2(-0.42f, 1.05f), new Vector2(0.1f, 1f), chairColor, -8);

            // Человек: тёмная сидящая фигура (корпус и голова), триггер для E.
            var man = new GameObject("MumblingMan");
            man.transform.SetParent(props, false);
            man.transform.localPosition = new Vector3(CornerX, floorY + 1.15f, 0f);
            var col = man.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.2f, 1.9f);
            // Сжимается фигура вокруг своего центра — отдельный дочерний объект.
            var figure = new GameObject("Figure");
            figure.transform.SetParent(man.transform, false);
            var dark = new Color(0.1f, 0.1f, 0.11f);
            SB.Rect(figure, "Body", square, new Vector2(0f, -0.05f), new Vector2(0.6f, 0.9f), dark, -7);
            SB.Rect(figure, "Head", square, new Vector2(0.05f, 0.6f), new Vector2(0.4f, 0.4f), dark, -7);
            SB.Rect(figure, "Legs", square, new Vector2(0.25f, -0.55f), new Vector2(0.6f, 0.2f), dark, -7);

            RoomWiring.Set(man.AddComponent<PersonInteractable>(),
                ("prompt", "Interact"), ("visual", figure.transform), ("afterDialogue", gone));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log("[Setup] Floor_01: бормочущий человек на стуле в углу Room_101");
        }
    }
}
