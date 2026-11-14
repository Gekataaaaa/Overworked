using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SB = Overworked.EditorTools.PlayerSandboxBuilder;

namespace Overworked.EditorTools
{
    /// <summary>
    /// «Замок» Floor_03 по нарративу (сцены 1.2–1.3, паспорт раздел 2): путь к лестнице закрыт турникетом.
    /// - Кухня: пропуск Макса у кофеварки — «[E] Pick up the pass» → исчезает, флаг MaxPass,
    ///   задание F3_TakeMaxPass выполнено, «Got it.» (F3_PassPicked).
    /// - Турникет между кухней и лестницей, через него не пройти (сплошной барьер):
    ///   «[E] Use your card» → ACCESS DENIED (F3_TurnstileDenied); с пропуском — «[E] Use Max's card» →
    ///   мини-игра «Считыватель» → ACCESS GRANTED (F3_TurnstileGranted) → барьер убирается, лестница открывается.
    /// - Лестница заперта на старте, подсказка «[E] Go down to the second floor».
    /// Сообщение Босса и Галлюцинация 1 после отказа — сценарий P3/P4 через onDenied турникета.
    /// Тексты — ассеты Data/ с именами из таблицы P3, строки дословно из NARRATIVE_EN; существующие не трогаются.
    /// Меню: Overworked → P2 → Build Floor_03 Turnstile And Stairs.
    /// </summary>
    [InitializeOnLoad]
    public static class Floor03Turnstile
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_03.unity";
        const string RequestFile = "Temp/p2_floor03_turnstile.request";

        const string DeniedPath = "Assets/_Project/Data/Dialogues/F3_TurnstileDenied.asset";
        const string GrantedPath = "Assets/_Project/Data/Dialogues/F3_TurnstileGranted.asset";
        const string PassPickedPath = "Assets/_Project/Data/Dialogues/F3_PassPicked.asset";
        const string TakePassPath = "Assets/_Project/Data/Objectives/F3_TakeMaxPass.asset";

        static Floor03Turnstile()
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
            Build();
        }

        static void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            RunRequest();
        }

        [MenuItem("Overworked/P2/Build Floor_03 Turnstile And Stairs")]
        public static void Build()
        {
            // Несохранённые правки в открытой сцене — спросить, иначе OpenScene их выбросит.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string previous = SceneManager.GetActiveScene().path;

            // Сначала сцена, потом ассеты: OpenScene выгружает «неиспользуемые» ассеты, и загруженные
            // до него тексты/задания превращались в null — в сцену сохранялись пустые ссылки.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // --- Тексты (NARRATIVE_EN, сцены 1.2–1.3)
            var denied = Dialogue(DeniedPath, (Speaker.Description, "ACCESS DENIED."), (Speaker.Danny, "What?.."));
            var granted = Dialogue(GrantedPath, (Speaker.Description, "ACCESS GRANTED."), (Speaker.Danny, "It worked. Sorry, Max."));
            var passPicked = Dialogue(PassPickedPath, (Speaker.Danny, "Got it."));
            var takePass = AssetDatabase.LoadAssetAtPath<ObjectiveData>(TakePassPath);
            if (takePass == null) Debug.LogWarning("[Setup] нет F3_TakeMaxPass (P3) — пропуск не закроет задание");

            Transform env = null, interactables = null, staircaseRoot = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Environment") env = root.transform;
                if (root.name == "Interactables") interactables = root.transform;
                if (root.name == "Staircase") staircaseRoot = root.transform;
            }
            var kitchen = env != null ? env.Find("Rooms/Kitchen") : null;
            var kitchenDoor = env != null ? env.Find("Corridor/Doors/Door_Kitchen") : null;
            var stairs = staircaseRoot != null ? staircaseRoot.GetComponentInChildren<StairInteractable>(true) : null;
            if (kitchen == null || kitchenDoor == null || stairs == null || interactables == null)
            {
                Debug.LogError("[Setup] Floor_03: нет кухни, двери кухни, лестницы или корня Interactables");
                return;
            }
            var square = SB.EnsureSquareSprite();
            const float floorY = -4.2f; // линия пола каркаса (коридор и комнаты)

            // --- Кухня: пропуск Макса «на полке у кофеварки» (пока без арта — маленькая карточка)
            var kitchenProps = Child(kitchen, "Props");
            Remove(kitchenProps, "MaxPass");
            var pass = new GameObject("MaxPass");
            pass.transform.SetParent(kitchenProps, false);
            pass.transform.localPosition = new Vector3(4f, floorY + 1.2f, 0f);
            Trigger(pass, new Vector2(0.6f, 0.4f));
            SB.Rect(pass, "Sprite", square, Vector2.zero, new Vector2(0.6f, 0.4f), new Color(0.9f, 0.88f, 0.8f), -6);
            RoomWiring.Set(pass.AddComponent<PickupInteractable>(),
                ("prompt", "Pick up the pass"), ("setsFlag", "MaxPass"),
                ("completesObjective", takePass), ("afterDialogue", passPicked));

            // --- Турникет между кухней и лестницей
            Remove(interactables, "Turnstile");
            float x = (kitchenDoor.position.x + stairs.transform.position.x) / 2f;
            var turnstile = new GameObject("Turnstile");
            turnstile.transform.SetParent(interactables, false);
            turnstile.transform.position = new Vector3(x, floorY + 0.9f, 0f);
            // Зона взаимодействия — шире самого турникета, чтобы E работала, упёршись в барьер.
            Trigger(turnstile, new Vector2(2.4f, 1.8f));
            SB.Rect(turnstile, "Sprite", square, Vector2.zero, new Vector2(0.8f, 1.8f), new Color(0.5f, 0.55f, 0.6f), -7);
            // Барьер: сплошной коллайдер, Дэнни упирается в него, пока турникет не пропустит.
            var barrier = new GameObject("Barrier");
            barrier.transform.SetParent(turnstile.transform, false);
            barrier.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 6f);

            var reader = turnstile.AddComponent<CardReaderMiniGame>();
            var ts = turnstile.AddComponent<TurnstileInteractable>();
            RoomWiring.Set(ts,
                ("ownCardPrompt", "Use your card"), ("passPrompt", "Use Max's card"), ("passFlag", "MaxPass"),
                ("cardReader", reader), ("deniedDialogue", denied), ("grantedDialogue", granted));
            // ACCESS GRANTED → барьер убрать, лестницу открыть.
            UnityEventTools.AddBoolPersistentListener(ts.onDone, barrier.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(ts.onDone, stairs.SetLocked, false);
            EditorUtility.SetDirty(ts);

            // --- Лестница: заперта до турникета, подсказка из нарратива
            RoomWiring.Set(stairs, ("startLocked", true), ("prompt", "Go down to the second floor"));
            PrefabUtility.RecordPrefabInstancePropertyModifications(stairs);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(previous) && previous != ScenePath)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            Debug.Log($"[Setup] Floor_03: пропуск на кухне, турникет на X={x:0.#}, лестница заперта до турникета");
        }

        static Transform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return t;
            t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static void Remove(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        static void Trigger(GameObject go, Vector2 size)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;
        }

        static DialogueData Dialogue(string path, params (Speaker speaker, string text)[] lines)
        {
            var d = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (d != null) return d; // уже есть (мог сделать P3) — не трогаем
            d = ScriptableObject.CreateInstance<DialogueData>();
            d.Lines = new DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++) d.Lines[i] = new DialogueLine { Speaker = lines[i].speaker, Text = lines[i].text };
            AssetDatabase.CreateAsset(d, path);
            return d;
        }
    }
}
