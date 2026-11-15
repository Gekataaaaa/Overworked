using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// Floor_01: обязательное задание «Talk to the man in room 101.» (F1_TalkToMan) — выдаётся вместе с первым
    /// заданием этажа «Find the key in room 101.» (ObjectiveLink в Story), выполняется разговором с сидящим
    /// (PersonInteractable.completesObjective). Дверь 102 открывается, только когда есть ключ и задание выполнено;
    /// с ключом, но без разговора — реплика F1_Door102TalkFirst. Карточка показывает оба задания списком.
    /// Оба задания выдаются до входа в 101: зона перед дверью 101 (ObjectiveTrigger) — даже если игрок
    /// не пробовал дверь 102 (её табличка по-прежнему тоже выдаёт задание).
    /// Меню: Overworked → P1 → Floor_01 Talk To Man Objective (повторить после Overworked → P4 → Floor_01 Art
    /// не нужно — Floor01Art сам ставит задание сидящему, если ассет есть).
    /// </summary>
    public static class Floor01TalkToMan
    {
        public const string TalkPath = "Assets/_Project/Data/Objectives/F1_TalkToMan.asset";
        const string ScenePath = "Assets/_Project/Scenes/Floor_01.unity";
        const string FindKeyPath = "Assets/_Project/Data/Objectives/F1_FindKey101.asset";
        const string TalkFirstPath = "Assets/_Project/Data/Dialogues/F1_Door102TalkFirst.asset";
        const string LinkName = "F1_TalkToManLink";
        const string ZoneName = "F1_Door101Objectives";

        [MenuItem("Overworked/P1/Floor_01 Talk To Man Objective")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Ассеты — после OpenScene: иначе сцена выгрузит их и сохранятся пустые ссылки.
            var talk = AssetDatabase.LoadAssetAtPath<ObjectiveData>(TalkPath);
            if (talk == null)
            {
                talk = ScriptableObject.CreateInstance<ObjectiveData>();
                talk.Id = "F1_TalkToMan";
                talk.Title = "Talk to the man in room 101.";
                AssetDatabase.CreateAsset(talk, TalkPath);
            }
            var talkFirst = AssetDatabase.LoadAssetAtPath<DialogueData>(TalkFirstPath);
            if (talkFirst == null)
            {
                talkFirst = ScriptableObject.CreateInstance<DialogueData>();
                talkFirst.Lines = new[] { new DialogueLine { Speaker = Speaker.Danny, Text = "I should talk to the man in 101 first..." } };
                AssetDatabase.CreateAsset(talkFirst, TalkFirstPath);
            }
            AssetDatabase.SaveAssets();
            var findKey = AssetDatabase.LoadAssetAtPath<ObjectiveData>(FindKeyPath);
            if (findKey == null) { Debug.LogError($"[Setup] нет {FindKeyPath}"); return; }

            int persons = 0, doors = 0;
            DoorInteractable door101 = null;
            Transform story = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Story") story = root.transform;
                foreach (var p in root.GetComponentsInChildren<PersonInteractable>(true))
                {
                    RoomWiring.Set(p, ("completesObjective", talk));
                    persons++;
                }
                foreach (var d in root.GetComponentsInChildren<DoorInteractable>(true))
                {
                    var dso = new SerializedObject(d);
                    var room = dso.FindProperty("room").objectReferenceValue as Component;
                    if (room != null && room.name == "Room_101"
                        && dso.FindProperty("direction").enumValueIndex == (int)DoorInteractable.Direction.IntoRoom)
                        door101 = d;
                    if (dso.FindProperty("unlockFlag").stringValue != "Key102") continue;
                    RoomWiring.Set(d, ("unlockAlsoRequires", talk), ("notReadyDialogue", talkFirst));
                    doors++;
                }
            }

            if (story == null) story = new GameObject("Story").transform;
            var old = story.Find(LinkName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var link = new GameObject(LinkName).AddComponent<ObjectiveLink>();
            link.transform.SetParent(story, false);
            RoomWiring.Set(link, ("whenStarted", findKey), ("alsoStart", talk));

            // Зона перед дверью 101: подошёл, чтобы войти, — задания уже выданы (связка добавит «поговорить»).
            var oldZone = story.Find(ZoneName);
            if (oldZone != null) Object.DestroyImmediate(oldZone.gameObject);
            if (door101 != null)
            {
                var zone = new GameObject(ZoneName);
                zone.transform.SetParent(story, false);
                zone.transform.position = door101.transform.position;
                var col = zone.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(4f, 10f);   // по высоте с запасом: позиция двери — её центр, Дэнни ниже
                RoomWiring.Set(zone.AddComponent<ObjectiveTrigger>(), ("objective", findKey));
            }
            else Debug.LogWarning("[Setup] Floor_01: нет двери в Room_101 — задания выдаст только дверь 102");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (persons == 0) Debug.LogWarning("[Setup] Floor_01: нет сидящего (PersonInteractable) — сначала Overworked → P4 → Floor_01 Art");
            if (doors == 0) Debug.LogWarning("[Setup] Floor_01: нет двери 102 (unlockFlag Key102)");
            Debug.Log($"[Setup] Floor_01: задание «поговорить с сидящим» — сидящих {persons}, дверей 102 {doors}");
        }
    }
}
