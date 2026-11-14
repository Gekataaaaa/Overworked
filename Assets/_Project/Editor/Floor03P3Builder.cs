using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Overworked.EditorTools
{
    /// <summary>
    /// P3: записка Чарльза на Floor_03 (нарратив 1.1), которой нет в сборке P2/P1: у стола Чарльза
    /// «[E] Take the note» → текст на весь экран → тост «Received: Charles's note.» → реплики F3_CharlesNote_After.
    /// Всё кладётся в свой корневой объект P3_Floor03 — расстановку P2 и группу Chain (P1) не трогает.
    /// Меню Overworked → P3 → Add Charles Note To Floor 03; повторный запуск пересоздаёт только P3_Floor03.
    /// </summary>
    public static class Floor03P3Builder
    {
        const string ScenePath = "Assets/_Project/Scenes/Floor_03.unity";
        const string RootName = "P3_Floor03";
        const string NoteText = "THE BOSS DOESN'T LIKE IT WHEN I LEAVE BEFORE HIM!!!";
        // Линия пола этажей (FloorSkeletonBuilder / ChainBuilder): Дэнни стоит на FloorY + 0.9.
        const float FloorY = -4.2f;

        [MenuItem("Overworked/P3/Add Charles Note To Floor 03")]
        public static void Run()
        {
            // Сцену открываем ДО загрузки ассетов: OpenScene(Single) выгружает загруженные раньше ассеты.
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var square = PlayerSandboxBuilder.EnsureSquareSprite();

            foreach (var go in scene.GetRootGameObjects())
                if (go.name == RootName) Object.DestroyImmediate(go);
            var root = new GameObject(RootName);

            // Записка — у зоны стола Чарльза (сам стол теперь часть фоновой картинки P2).
            var desk = Find(scene, "CharlesDesk") ?? Find(scene, "CharlesDesk_Inspect");
            if (desk == null)
            {
                Debug.LogError("[Setup] Floor_03: не найден стол Чарльза (CharlesDesk / CharlesDesk_Inspect) — записка не поставлена");
                return;
            }
            Note(root, square, desk.position.x);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] Floor_03: записка Чарльза поставлена (P3_Floor03/CharlesNote)");
        }

        /// <summary>
        /// Галлюцинация 1 у турникета (нарратив 1.3): в сценарий цепочки «1.3 DeniedBossThenMax» после сообщения Босса
        /// вставляет шаги Hallucination (H1_Turnstile, P4) и Dialogue (F3_H1_After). Если шаги уже есть — ничего не делает.
        /// Сборщик цепочки P1 (ChainBuilder) теперь тоже их ставит — это меню для сцены без пересборки цепочки.
        /// </summary>
        [MenuItem("Overworked/P3/Add H1 To Floor 03 Turnstile")]
        public static void AddH1()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var after = AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/_Project/Data/Dialogues/F3_H1_After.asset");
            ScriptedSequence seq = null;
            HallucinationEvent h1 = null;
            foreach (var s in Object.FindObjectsByType<ScriptedSequence>(FindObjectsInactive.Include))
                if (s.name == "1.3 DeniedBossThenMax") seq = s;
            foreach (var h in Object.FindObjectsByType<HallucinationEvent>(FindObjectsInactive.Include))
                if (h.name == "H1_Turnstile") h1 = h;
            if (seq == null || h1 == null)
            {
                Debug.LogError("[Setup] Floor_03: нет сценария «1.3 DeniedBossThenMax» или галлюцинации H1_Turnstile");
                return;
            }
            foreach (var step in seq.Steps)
                if (step is HallucinationStep)
                {
                    Debug.Log("[Setup] Floor_03: H1 уже стоит в сценарии турникета");
                    return;
                }

            var so = new SerializedObject(seq);
            var steps = so.FindProperty("steps");
            // Шаг 0 — сообщение Босса (ждём прочтения); сразу за ним — галлюцинация и реплика.
            int at = 1;
            steps.InsertArrayElementAtIndex(at);
            steps.GetArrayElementAtIndex(at).managedReferenceValue = new HallucinationStep { Hallucination = h1, Wait = true };
            steps.InsertArrayElementAtIndex(at + 1);
            steps.GetArrayElementAtIndex(at + 1).managedReferenceValue = new DialogueStep { Dialogue = after, Wait = true };
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] Floor_03: после сообщения Босса — H1 и «My ears are ringing…»");
        }

        /// <summary>Записка и H1 за один запуск (batchmode).</summary>
        public static void RunAll()
        {
            Run();
            AddH1();
        }

        static void Note(GameObject root, Sprite square, float x)
        {
            var after = AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/_Project/Data/Dialogues/F3_CharlesNote_After.asset");
            var note = new GameObject("CharlesNote");
            note.transform.SetParent(root.transform, false);
            note.transform.position = new Vector3(x, FloorY + 1.5f, 0f);

            // Жёлтый листок на правом верхнем углу монитора Чарльза (монитор нарисован на фоне P2).
            var paper = PlayerSandboxBuilder.Rect(note, "Paper", square, new Vector2(-0.7f, 0f), new Vector2(0.25f, 0.25f),
                new Color(0.8f, 0.73f, 0.36f), 4);
            paper.transform.localRotation = Quaternion.Euler(0, 0, 8f);

            var col = note.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(2f, 3.5f);

            var interact = note.AddComponent<NoteInteractable>();
            var so = new SerializedObject(interact);
            so.FindProperty("prompt").stringValue = "Take the note";
            so.FindProperty("noteText").stringValue = NoteText;
            so.FindProperty("receivedToast").stringValue = "Received: Charles's note.";
            so.FindProperty("afterDialogue").objectReferenceValue = after;
            so.ApplyModifiedPropertiesWithoutUndo();
        }


        static Transform Find(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                var t = FindDeep(go.transform, name);
                if (t != null) return t;
            }
            return null;
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
